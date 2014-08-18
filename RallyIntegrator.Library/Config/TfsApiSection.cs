using System.Configuration;

namespace RallyIntegrator.Library.Config
{
    public class TfsApiSection : AuthenticatedApiSection
    {
        [ConfigurationProperty("repository", IsRequired = true)]
        public string Repository
        {
            get { return (string)this["repository"]; }
            set { this["repository"] = value; }
        }

        [ConfigurationProperty("changeUriFormat", IsRequired = false, DefaultValue = "{0}/web/UI/Pages/Scc/ViewSource.aspx?path={1}&amp;changeset={2}")]
        public string ChangeUriFormat
        {
            get { return (string)this["changeUriFormat"]; }
            set { this["changeUriFormat"] = value; }
        }

        [ConfigurationProperty("changesetUriFormat", IsRequired = false, DefaultValue = "{0}/VersionControl/Changeset.aspx?artifactMoniker={1}&amp;webView={2}")]
        public string ChangesetUriFormat
        {
            get { return (string)this["changesetUriFormat"]; }
            set { this["changesetUriFormat"] = value; }
        }
    }
}