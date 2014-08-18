using System.Configuration;

namespace RallyIntegrator.Library.Config
{
    public class LdapSection : ConfigurationSection
    {
        [ConfigurationProperty("accountProperty", IsRequired = false, DefaultValue = "sAMAccountName")]
        public string AccountProperty
        {
            get { return (string)this["accountProperty"]; }
            set { this["accountProperty"] = value; }
        }

        [ConfigurationProperty("emailProperty", IsRequired = false, DefaultValue = "mail")]
        public string EmailProperty
        {
            get { return (string)this["emailProperty"]; }
            set { this["emailProperty"] = value; }
        }
    }
}