using System;
using System.Configuration;
using System.Linq;
using System.Net;
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
            var wc = new WebClient { Credentials = new NetworkCredential(Username, Password) };
            var url = string.Format(ChangesetUriFormat, Url, revision);
            var xdoc = XDocument.Parse(wc.DownloadString(new Uri(url)), LoadOptions.None);
            var ns = xdoc.Root.GetDefaultNamespace();
            try
            {
                return new Changeset
                {
                    Revision = revision,
                    Uri = url,
                    Author = xdoc.Root.Attribute(ns + "cmtr").Value,
                    CommitTimestamp = xdoc.Root.Attribute(ns + "date").Value,
                    Message = xdoc.Root.Element(ns + "Comment").Value,
                    Repository = Repository,
                    Changes = xdoc.Root.Element(ns + "Changes").Elements(ns + "Change").Select(x => new Change
                    {
                        Action = x.Attribute(ns + "type").Value,
                        Path = x.Element(ns + "Item").Attribute(ns + "item").Value,
                        Uri = string.Format(ChangeUriFormat, Url, Uri.EscapeDataString(x.Element(ns + "Item").Attribute(ns + "item").Value), revision)
                    })
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
