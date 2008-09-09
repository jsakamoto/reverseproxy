using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

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
            Uri outgoingUri;
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

            // Create destination mapping, this includes GET query as well
            outgoingUri = CreateRemoteUri(mapping, GenerateTokensFromUri(context.Request.Url));
            
            HttpWebRequest outgoing = (HttpWebRequest)WebRequest.Create(outgoingUri);

            // Credentials
            //request.Credentials = CredentialCache.DefaultCredentials;
            
            // Headers
            outgoing.Method = context.Request.RequestType;
            outgoing.ContentType = context.Request.ContentType;

         
            // Copy POST Data
            if (mapping.IncludePost && (context.Request.RequestType == "POST"))
            {
                outgoing.ContentLength = context.Request.ContentLength;
                CopyStream(context.Request.InputStream, outgoing.GetRequestStream());
            }

            try
            {
                response = (HttpWebResponse)outgoing.GetResponse();
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

        /// <summary>
        /// Copy data from one stream, to the other.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        private void CopyStream(Stream source, Stream dest)
        {
            const int MAX_LEN = 512;
            byte[] buffer = new byte[MAX_LEN];
            int readBytes;
            while ((readBytes = source.Read(buffer, 0, MAX_LEN)) > 0)
            {
                dest.Write(buffer, 0, buffer.Length);
            }
        }

        private Uri CreateRemoteUri(MappingElement mapping, IDictionary<string, string> tokens)
        {
            Uri remoteUri;
            if (!mapping.UseRegex)
            {
                string uri = ReplaceTokens(mapping.TargetURI, tokens);
                remoteUri = new Uri(uri);
            }
            else
            {
                //TODO: impliment
                remoteUri = null;
                throw new NotImplementedException("RegEx RemoteURI not yet supported");
                //Regex regex = new Regex(targetUri);
                //return regex.IsMatch(sourceUri.AbsoluteUri);
            }
            return remoteUri;
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

        private static string ReplaceTokens(string text, IDictionary<string, string> tokens)
        {
            Regex re = new Regex("#.*?#", RegexOptions.Compiled | RegexOptions.Singleline);
            StringBuilder sb = new StringBuilder(text);
            string replace;

            foreach (Match m in re.Matches(text))  // assuming text is the text to search
            {
                // Replace any matching tokens
                if (tokens.TryGetValue(m.Value, out replace))
                    sb.Replace(m.Value, replace);
            }
            return sb.ToString();
        }

        private static IDictionary<string, string> GenerateTokensFromUri(Uri uri)
        {
            IDictionary<string, string> tokens = new Dictionary<string, string>();

            tokens.Add("#host#", uri.Host);
            tokens.Add("#port#", uri.Port.ToString());
            //tokens.Add("#path#", uri.AbsolutePath);
            tokens.Add("#path#", GetPathFromSegments(uri.Segments));
            tokens.Add("#page#", GetPageFromSegments(uri.Segments));
            tokens.Add("#query#", uri.Query);

            return tokens;
        }

        private static string GetPathFromSegments(string[] segments)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < segments.Length - 1; i++)
                sb.Append(segments[i]);

            return sb.ToString();
        }

        private static string GetPageFromSegments(string[] segments)
        {
            return segments[segments.Length - 1];
        }
    }
}
