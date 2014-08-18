using System.Configuration;

namespace RallyIntegrator.Library.Config
{
    public class TeamCityApiSection : AuthenticatedApiSection
    {
        [ConfigurationProperty("changeUriFormat", IsRequired = false, DefaultValue = "{0}/httpAuth/app/rest/changes?locator=version:{1}")]
        public string ChangeUriFormat
        {
            get { return (string)this["changeUriFormat"]; }
            set { this["changeUriFormat"] = value; }
        }

        [ConfigurationProperty("changeBuildsUriFormat", IsRequired = false, DefaultValue = "{0}/httpAuth/app/rest/changes/id:{1}/firstBuilds")]
        public string ChangeBuildsUriFormat
        {
            get { return (string)this["changeBuildsUriFormat"]; }
            set { this["changeBuildsUriFormat"] = value; }
        }

        [ConfigurationProperty("buildUriFormat", IsRequired = false, DefaultValue = "{0}/httpAuth/app/rest/builds/id:{1}")]
        public string BuildUriFormat
        {
            get { return (string)this["buildUriFormat"]; }
            set { this["buildUriFormat"] = value; }
        }

        [ConfigurationProperty("buildTypeUriFormat", IsRequired = false, DefaultValue = "{0}/httpAuth/app/rest/buildTypes/id:{1}")]
        public string BuildTypeUriFormat
        {
            get { return (string)this["buildTypeUriFormat"]; }
            set { this["buildTypeUriFormat"] = value; }
        }
    }
}