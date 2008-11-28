using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;
using System.Net;

namespace ReverseProxy
{
    public class Utility
    {
        public const int BUFFER_LENGTH = 1024;

        public static string ConvertStream(Stream stream)
        {
            byte[] buff = new byte[BUFFER_LENGTH];
            int bytes = 0;
            StringBuilder sb = new StringBuilder();
            while ((bytes = stream.Read(buff, 0, BUFFER_LENGTH)) > 0)
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                sb.Append(enc.GetString(buff, 0, bytes));
            }
            return sb.ToString();
        }

        public static void CopyHeaders(HttpRequest from, HttpWebRequest to)
        {
            string value;
            foreach (string key in from.Headers.AllKeys)
            {
                value = from.Headers[key];
                switch (key)
                {
                    case "Host":
                    case "Connection":
                    case "Content-Length":
                        //Ignore, will be populated as needed by the outgoing request
                        break;

                    case "Expect":
                        /* Not sure how to impliment this one. Just filtering it out for now.
                         * if (value == "100-Continue")
                            System.Net.ServicePointManager.Expect100Continue=true;
                         * */
                        break;

                    case "Content-Type":
                        to.ContentType = value;
                        break;

                    case "Accept":
                        to.Accept = value;
                        break;

                    case "Referer":
                        to.Referer = value;
                        break;

                    case "User-Agent":
                        to.UserAgent = value;
                        break;

                    default:
                        to.Headers.Add(key, value);
                        break;
                }
            }
            // Not a header per say... but close enough
            to.Method = from.RequestType;
        }

        public static void Return404(HttpContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.StatusDescription = "Not Found";
            context.Response.Write("<h2>Not Found</h2>");
            context.Response.End();
        }

        public static void Return500(HttpContext context, WebException ex)
        {
            context.Response.StatusCode = 500;
            context.Response.StatusDescription = "Proxy Error";
            context.Response.Write("<h2>Error connecting to upstream server</h2>");
            context.Response.Write("<pre>");

            CopyStream(ex.Response.GetResponseStream(), context.Response.OutputStream);

            context.Response.Write("</pre>");
            context.Response.End();
        }

        /// <summary>
        /// Copy data from one stream, to the other.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void CopyStream(Stream source, Stream dest)
        {
            byte[] buffer = new byte[BUFFER_LENGTH];
            int readBytes;
            while ((readBytes = source.Read(buffer, 0, BUFFER_LENGTH)) > 0)
            {
                dest.Write(buffer, 0, readBytes);
            }
        }
    }
}
