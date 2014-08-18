using System.Configuration;

namespace RallyIntegrator.Library.Config
{
    public class RallyApiSection : AuthenticatedApiSection
    {
        [ConfigurationProperty("project", IsRequired = true)]
        public string Project
        {
            get { return (string)this["project"]; }
            set { this["project"] = value; }
        }

        [ConfigurationProperty("projectUriFormat", IsRequired = false, DefaultValue = "{0}/project/{1}")]
        public string ProjectUriFormat
        {
            get { return (string)this["projectUriFormat"]; }
            set { this["projectUriFormat"] = value; }
        }
    }
}