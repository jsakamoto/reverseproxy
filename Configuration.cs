using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace ReverseProxy
{
    /// <summary>
    /// Abstract class that provides a generic implimentation of ConfigurationElementCollection
    /// </summary>
    /// <typeparam name="K">Type of the Key. For example this could be a string or integer.</typeparam>
    /// <typeparam name="V">Type of the Value. For example this could be string, integer or even a entire class.</typeparam>
    public abstract class ConfigurationElementCollection<K, V> : ConfigurationElementCollection where V : ConfigurationElement, new()
    {
        public ConfigurationElementCollection()
        {
        }

        public abstract override ConfigurationElementCollectionType CollectionType
        {
            get;
        }

        protected abstract override string ElementName
        {
            get;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new V();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return GetElementKey((V)element);
        }

        protected abstract K GetElementKey(V element);

        public void Add(V path)
        {
            BaseAdd(path);
        }

        public void Remove(K key)
        {
            BaseRemove(key);
        }

        public V Get(K key)
        {
            return (V)BaseGet(key);
        }

        public V Get(int index)
        {
            return (V)BaseGet(index);
        }

     /* The following code, while allowing direct enumeration, prevents the use of properties
      * 
       public V this[K key]
        {
            get { return (V)BaseGet(key); }
        }

        public V this[int index]
        {
            get { return (V)BaseGet(index); }
        }*/
    }

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

        [ConfigurationProperty("RewriteGroups", IsRequired = true)]
        public RewriteGroups RewriteGroups
        {
            get
            {
                return (RewriteGroups)this["RewriteGroups"];
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

        [ConfigurationProperty("sourceRegexMatching", DefaultValue = false, IsRequired = true)]
        public bool SourceRegexMatching
        {
            get
            {
                return (bool)this["sourceRegexMatching"];
            }
        }

        [ConfigurationProperty("sourceIncludePost", DefaultValue = false, IsRequired = true)]
        public bool SourceIncludePost
        {
            get
            {
                return (bool)this["sourceIncludePost"];
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

        [ConfigurationProperty("rewriteContent", DefaultValue = "", IsRequired = true)]
        public string RewriteContent
        {
            get
            {
                return (string)this["rewriteContent"];
            }
        }
    }

    public class RewriteGroups : ConfigurationElementCollection<string, RewriteGroup>
    {

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "RewriteGroup"; }
        }

        protected override string GetElementKey(RewriteGroup element)
        {
            return element.Id;
        }
    }

    public class RewriteGroup : ConfigurationElementCollection<string, Rewrite>
    {
        [ConfigurationProperty("id", DefaultValue = "", IsRequired = true)]
        public string Id
        {
            get
            {
                return (string)this["id"];
            }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "Rewrite"; }
        }

        protected override string GetElementKey(Rewrite element)
        {
            return element.Match;
        }
    }

    public class Rewrite : ConfigurationElement
    {
        [ConfigurationProperty("enableRegEx", DefaultValue = false, IsRequired = true)]
        public bool EnableRegEx
        {
            get
            {
                return (bool)this["enableRegEx"];
            }
        }

        [ConfigurationProperty("match", DefaultValue = "", IsRequired = true)]
        public string Match
        {
            get
            {
                return (string)this["match"];
            }
        }

        [ConfigurationProperty("replace", DefaultValue = "", IsRequired = true)]
        public string Replace
        {
            get
            {
                return (string)this["replace"];
            }
        }

    }
}
