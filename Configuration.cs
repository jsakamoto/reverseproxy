using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace ReverseProxy
{
    public class ReverseProxyConfiguration : ConfigurationSection
    {
        [ConfigurationProperty("requireAuthentication", DefaultValue = false, IsRequired = true)]
        public bool RequireAuthentication
        {
            get
            {
                return (bool)this["requireAuthentication"];
            }
        }

        [ConfigurationProperty("requireSSL", DefaultValue = false, IsRequired = true)]
        public bool RequireSSL
        {
            get
            {
                return (bool)this["requireSSL"];
            }
        }


        [ConfigurationProperty("Mappings", IsRequired = true)]
        public MappingCollection Mappings
        {
            get
            {
                return (MappingCollection)this["Mappings"];
            }
        }

        public static ReverseProxyConfiguration LoadSettings()
        {
            return (ReverseProxyConfiguration)System.Configuration.ConfigurationManager.GetSection("ReverseProxyConfiguration");
        }
    }

    [ConfigurationCollection(typeof(MappingElement))]
    public class MappingCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new MappingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MappingElement)element).SourceURI;
        }

        public MappingElement this[int idx]
        {
            get
            {
                return (MappingElement)BaseGet(idx);
            }
        }
    }

    public class MappingElement : ConfigurationElement 
    {
        [ConfigurationProperty("sourceURI", IsRequired = true)]
        public string SourceURI
        {
            get
            {
                return this["sourceURI"] as string;
            }
        }

        [ConfigurationProperty("targetURI", IsRequired = true)]
        public string TargetURI
        {
            get
            {
                return this["targetURI"] as string;
            }
        }

        [ConfigurationProperty("rewriteContent", DefaultValue = false, IsRequired = true)]
        public bool RewriteContent
        {
            get
            {
                return (bool)this["rewriteContent"];
            }
        }

        [ConfigurationProperty("useRegex", DefaultValue = false, IsRequired = true)]
        public bool UseRegex
        {
            get
            {
                return (bool)this["useRegex"];
            }
        }

        [ConfigurationProperty("includePost", DefaultValue = false, IsRequired = true)]
        public bool IncludePost
        {
            get
            {
                return (bool)this["includePost"];
            }
        }
    }
}
