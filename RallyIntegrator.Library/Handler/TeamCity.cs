using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library.Handler
{
    public class TeamCity
    {
        private static string Username { get { return ConfigurationManager.AppSettings.Get("TeamCityUsername"); } }
        private static string Password { get { return ConfigurationManager.AppSettings.Get("TeamCityPassword"); } }
        private static string Url { get { return ConfigurationManager.AppSettings.Get("TeamCityUrl"); } }
        private static string ChangeUriFormat { get { return ConfigurationManager.AppSettings.Get("TeamCityChangeUriFormat"); } }
        private static string BuildUriFormat { get { return ConfigurationManager.AppSettings.Get("TeamCityBuildUriFormat"); } }
        private static string BuildTypeUriFormat { get { return ConfigurationManager.AppSettings.Get("TeamCityBuildTypeUriFormat"); } }
        private static string ChangeBuildsUriFormat { get { return ConfigurationManager.AppSettings.Get("TeamCityChangeBuildsUriFormat"); } }
        private static readonly Dictionary<string, BuildDefinition> BuildDefinitionCache = new Dictionary<string, BuildDefinition>();

        private static readonly WebClient WebClient = new WebClient { Credentials = new NetworkCredential(Username, Password) };

        private string GetChangeId(string vcsRevision)
        {
            var xml = WebClient.DownloadString(string.Format(ChangeUriFormat, Url, vcsRevision));
            var root = XDocument.Parse(xml).Root;
            if (root != null)
            {
                var change = root.Element("change");
                return change != null ? change.Attribute("id").Value : null;
            }
            return null;
        }

        private IEnumerable<string> GetBuildIds(string changeId)
        {
            var xml = WebClient.DownloadString(string.Format(ChangeBuildsUriFormat, Url, changeId));
            var root = XDocument.Parse(xml).Root;
            return root != null
                ? root.Elements("build").Select(build => build.Attribute("id").Value)
                : Enumerable.Empty<string>();
        }

        private BuildDefinition GetBuildDefinition(string buildTypeId)
        {
            if (!BuildDefinitionCache.ContainsKey(buildTypeId))
            {
                var xml = WebClient.DownloadString(string.Format(BuildTypeUriFormat, Url, buildTypeId));
                var root = XDocument.Parse(xml).Root;
                if (root != null)
                {
                    var project = root.Element("project");
                    if (project != null)
                    {
                        BuildDefinitionCache.Add(buildTypeId, new BuildDefinition
                        {
                            Name = root.Attribute("name").Value,
                            Description = string.IsNullOrWhiteSpace(root.Attribute("description").Value) ? root.Attribute("id").Value : root.Attribute("description").Value,
                            Project = project.Attribute("name").Value,
                            Uri = root.Attribute("webUrl").Value
                        });
                    }
                }
            }
            return BuildDefinitionCache.ContainsKey(buildTypeId)
                ? BuildDefinitionCache[buildTypeId]
                : null;
        }

        private Build GetBuild(string buildId)
        {
            var xml = WebClient.DownloadString(string.Format(BuildUriFormat, Url, buildId));
            var root = XDocument.Parse(xml).Root;
            if (root != null)
            {
                var statusText = root.Element("statusText");
                var startDate = root.Element("startDate");
                var finishDate = root.Element("finishDate");
                var duration = (startDate != null && finishDate != null)
                    ? DateTimeOffset.ParseExact(finishDate.Value, "yyyyMMddTHHmmsszzz", CultureInfo.InvariantCulture) - DateTimeOffset.ParseExact(startDate.Value, "yyyyMMddTHHmmsszzz", CultureInfo.InvariantCulture)
                    : TimeSpan.Zero;
                var buildType = root.Element("buildType");
                return (startDate != null && statusText != null && buildType != null)
                    ? new Build {
                        Duration = duration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                        Message = statusText.Value,
                        Number = root.Attribute("number").Value,
                        Start = startDate.Value,
                        Status = root.Attribute("status").Value,
                        Uri = root.Attribute("webUrl").Value,
                        BuildDefinition = GetBuildDefinition(buildType.Attribute("id").Value)
                    }
                    : null;
            }
            return null;
        }

        public IEnumerable<Build> GetBuilds(string vcsRevision)
        {
            var changeId = GetChangeId(vcsRevision);
            var buildIds = GetBuildIds(changeId);
            var builds = buildIds.Select(GetBuild).ToArray();
            return builds;
        }
    }
}
