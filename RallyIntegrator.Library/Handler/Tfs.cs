using System;
using System.Configuration;
using System.Linq;
using System.Xml.Linq;
using RallyIntegrator.Library.Config;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library.Handler
{
    public class Tfs
    {
        internal static readonly TfsApiSection Config = (TfsApiSection) ConfigurationManager.GetSection("integrationProviders/tfs");

        public Changeset GetChangeset(string revision)
        {
            var root = XDocument.Parse(WebHelper.DownloadString(string.Format(Config.ChangesetUriFormat, Config.Url, revision, "false"), Config.Username, Config.Password), LoadOptions.None).Root;
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
                            Uri = string.Format(Config.ChangesetUriFormat, Config.Url, revision, "true"),
                            Author = root.GetAttributeValue(ns + "cmtr"),
                            CommitTimestamp = root.GetAttributeValue(ns + "date"),
                            Message = root.GetElementValue(ns + "Comment"),
                            Repository = Config.Repository,
                            Changes = changes.Elements(ns + "Change").Select(x => new Change
                            {
                                Action = x.Attribute(ns + "type").Value,
                                Path = x.Element(ns + "Item").GetAttributeValue(ns + "item"),
                                Uri = string.Format(Config.ChangeUriFormat, Config.Url, Uri.EscapeDataString(x.Element(ns + "Item").GetAttributeValue(ns + "item")), revision)
                            })
                        }
                        : null;
                }
            }
            return null;
        }
    }
}
