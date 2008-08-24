using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace ReverseProxy
{
    public class ReverseProxy : IHttpHandler
    {
        private static ReverseProxyConfiguration _configuration;

        public ReverseProxyConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                    _configuration = ReverseProxyConfiguration.LoadSettings();
                return _configuration;
            }
        }

        bool IHttpHandler.IsReusable
        {
            get { return true; }
        }
        

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            MappingElement mapping = null;
            Uri remoteUri;
            HttpWebResponse response;

            //Check Configuration
            if (Configuration == null)
                throw new Exception("Unable to load Configuration");


            // Search for a matching mapping
            foreach (MappingElement map in Configuration.Mappings)
            {
                if (MatchUri(context.Request.Url, map.SourceURI, map.UseRegex))
                {
                    mapping = map;
                    break;
                }
            }

            // Return 404 if can't find mapping
            if (mapping == null)
            {
                Return404(context);
                return;
            }

            // Create destination mapping
            remoteUri = CreateRemoteUri(context.Request.Url, mapping);
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(remoteUri);
            //request.Credentials = CredentialCache.DefaultCredentials;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                Return404(context);
                return;
            }
            Stream receiveStream = response.GetResponseStream();

            // Do any parsing of HTML (or anything with URLs) here
            if (mapping.RewriteContent
                && ((response.ContentType.ToLower().IndexOf("html") >= 0) 
                    || (response.ContentType.ToLower().IndexOf("javascript") >= 0))
                )
            {
                throw new NotImplementedException("Content Rewriting is not yet implimented");
            }
            else
            {
                // Output without formating
                // NOTE: this would normaly be in the "else"
                byte[] buff = new byte[1024];
                int bytes = 0;
                while ((bytes = receiveStream.Read(buff, 0, 1024)) > 0)
                {
                    context.Response.OutputStream.Write(buff, 0, bytes);
                }
            }
            response.Close();
            context.Response.End();
        }

        private Uri CreateRemoteUri(Uri uri, MappingElement mapping)
        {
            if (!mapping.UseRegex)
            {
                return new Uri(mapping.TargetURI);
            }
            else
            {
                //TODO: impliment
                throw new NotImplementedException("RegEx RemoteURI not yet supported");
                return null;
                //Regex regex = new Regex(targetUri);
                //return regex.IsMatch(sourceUri.AbsoluteUri);
            }
        }

        private bool MatchUri(Uri sourceUri, string targetUri, bool useRegex)
        {
            if (!useRegex) 
            {
                return sourceUri.AbsolutePath.Equals(targetUri, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Regex regex = new Regex(targetUri);
                return regex.IsMatch(sourceUri.AbsoluteUri);
            }
        }

        private static void Return404(HttpContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.StatusDescription = "Not Found";
            context.Response.Write("<h2>Not Found</h2>");
            context.Response.End();
        }
    }
}
