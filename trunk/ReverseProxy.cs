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
                if (MatchUri(context.Request.Url, map.SourceURI, map.SourceRegexMatching))
                {
                    mapping = map;
                    break;
                }
            }

            // Return 404 if can't find mapping
            if (mapping == null)
            {
                Utility.Return404(context);
                return;
            }

            // Create destination mapping, this includes GET query as well
            outgoingUri = CreateRemoteUri(mapping, GenerateTokensFromRequest(context.Request));
            
            HttpWebRequest outgoing = (HttpWebRequest)WebRequest.Create(outgoingUri);

            // Credentials
            // TODO: Impliment Credential pass-through
            //request.Credentials = CredentialCache.DefaultCredentials;
            
            // Headers
            Utility.CopyHeaders(context.Request, outgoing);
         
            // Copy POST Data
            if (mapping.SourceIncludePost && (context.Request.RequestType == "POST"))
            {
                outgoing.ContentLength = context.Request.ContentLength;
                Utility.CopyStream(context.Request.InputStream, outgoing.GetRequestStream());
            }

            try
            {
                response = (HttpWebResponse)outgoing.GetResponse();
            }
            catch (WebException ex)
            {
                Utility.Return500(context, ex);
                return;
            }
            
            Stream receiveStream = response.GetResponseStream();

            //Copy some headers, not too many since I'm not against hiding internal details ;)
            //TODO: copy cookies?
            context.Response.ContentType = response.ContentType;

            // Copy Content Encoding
            //context.Response.ContentEncoding = Encoding.
            //context.Response.

            // Do any parsing of HTML (or anything with URLs) here
            if (!string.IsNullOrEmpty(mapping.RewriteContent)
                && ((response.ContentType.ToLower().IndexOf("html") >= 0) || (response.ContentType.ToLower().IndexOf("javascript") >= 0))
                )
            {
                string sResp = Utility.ConvertStream(receiveStream);
                sResp = RewriteContent(sResp, mapping.RewriteContent);
                context.Response.Write(sResp);
            }
            else
            {
                // Output without rewriting, this will offer the best performance and lowest memory useage.
                Utility.CopyStream(receiveStream, context.Response.OutputStream);
            }
            response.Close();
            context.Response.End();
        }

        private string RewriteContent(string content, string rewriteGroup)
        {
            //First get rewrite group
            RewriteGroup rewriteGrp = Configuration.RewriteGroups.Get(rewriteGroup);

            if (rewriteGrp == null)
                throw new ArgumentOutOfRangeException("rewriteGroup", rewriteGroup, "Unknown Rewrite Group");

            foreach (Rewrite rw in rewriteGrp)
            {
                if (rw.EnableRegEx)
                {
                    //TODO: Regex rewriting has not been implimented
                    throw new NotImplementedException("Regex rewriting has not been implimented.");
                }
                else
                {
                    content = content.Replace(rw.Match, rw.Replace);
                }
            }
            return content;
        }

        private Uri CreateRemoteUri(MappingElement mapping, IDictionary<string, string> tokens)
        {
            Uri remoteUri;
            string uri = ReplaceTokens(mapping.TargetURI, tokens);
            remoteUri = new Uri(uri);
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
                return regex.IsMatch(sourceUri.AbsolutePath);
            }
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

        private static IDictionary<string, string> GenerateTokensFromRequest(HttpRequest request)
        {
            IDictionary<string, string> tokens = new Dictionary<string, string>();
            string path;
            string page;

            GetDetailsFromPath(request.FilePath, out path, out page);

            tokens.Add("#host#", request.Url.Host);
            tokens.Add("#port#", request.Url.Port.ToString());
            tokens.Add("#path#", path);
            tokens.Add("#page#", page);
            tokens.Add("#query#", request.PathInfo + request.Url.Query);

            return tokens;
        }

        private static void GetDetailsFromPath(string fullPath, out string path, out string page)
        {
            int lastPos = fullPath.LastIndexOf('/') +1;
            if (lastPos > 0)
            {
                path = fullPath.Substring(0, lastPos);
                page = fullPath.Substring(lastPos);
            }
            else
            {
                path = string.Empty;
                page = fullPath;
            }
        }
    }
}
