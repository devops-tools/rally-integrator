using System;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library.Handler
{
    public class Tfs
    {
        public static string Repository { get { return ConfigurationManager.AppSettings.Get("TFSRepository"); } }
        private static string Username { get { return ConfigurationManager.AppSettings.Get("TFSUsername"); } }
        private static string Password { get { return ConfigurationManager.AppSettings.Get("TFSPassword"); } }

        private static string Url { get { return ConfigurationManager.AppSettings.Get("TFSUrl"); } }
        private static string ChangesetUriFormat { get { return ConfigurationManager.AppSettings.Get("TFSChangesetUriFormat"); } }
        private static string ChangeUriFormat { get { return ConfigurationManager.AppSettings.Get("TFSChangeUriFormat"); } }

        public Changeset GetChangeset(string revision)
        {
            var url = string.Format(ChangesetUriFormat, Url, revision);
            var root = XDocument.Parse(WebHelper.DownloadString(url, Username, Password), LoadOptions.None).Root;
            if (root != null)
            {
                var ns = root.GetDefaultNamespace();
                if (root.Name == "Changeset")
                {
                    var changes = root.Element(ns + "Changes");
                    return (changes != null)
                        ? new Changeset
                        {
                            Revision = revision,
                            Uri = url,
                            Author = root.GetAttributeValue(ns + "cmtr"),
                            CommitTimestamp = root.GetAttributeValue(ns + "date"),
                            Message = root.GetElementValue(ns + "Comment"),
                            Repository = Repository,
                            Changes = changes.Elements(ns + "Change").Select(x => new Change
                            {
                                Action = x.Attribute(ns + "type").Value,
                                Path = x.Element(ns + "Item").GetAttributeValue(ns + "item"),
                                Uri = string.Format(ChangeUriFormat, Url, Uri.EscapeDataString(x.Element(ns + "Item").GetAttributeValue(ns + "item")), revision)
                            })
                        }
                        : null;
                }
            }
            return null;
        }
    }
}
