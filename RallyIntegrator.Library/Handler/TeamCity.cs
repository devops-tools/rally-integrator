using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
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

        private string GetChangeId(string vcsRevision)
        {
            var xml = WebHelper.DownloadString(string.Format(ChangeUriFormat, Url, vcsRevision), Username, Password);
            var root = XDocument.Parse(xml).Root;
            if (root != null)
            {
                var change = root.Element("change");
                return change != null ? change.GetAttributeValue("id") : null;
            }
            return null;
        }

        private IEnumerable<string> GetBuildIds(string changeId)
        {
            var xml = WebHelper.DownloadString(string.Format(ChangeBuildsUriFormat, Url, changeId), Username, Password);
            var root = XDocument.Parse(xml).Root;
            return root != null
                ? root.Elements("build").Select(build => build.GetAttributeValue("id"))
                : Enumerable.Empty<string>();
        }

        private BuildDefinition GetBuildDefinition(string buildTypeId)
        {
            if (!BuildDefinitionCache.ContainsKey(buildTypeId))
            {
                var xml = WebHelper.DownloadString(string.Format(BuildTypeUriFormat, Url, buildTypeId), Username, Password);
                var root = XDocument.Parse(xml).Root;
                if (root != null)
                {
                    var project = root.Element("project");
                    if (project != null && !BuildDefinitionCache.ContainsKey(buildTypeId))
                        BuildDefinitionCache.Add(buildTypeId, new BuildDefinition
                        {
                            Name = root.GetAttributeValue("name"),
                            Description = string.IsNullOrWhiteSpace(root.GetAttributeValue("description")) ? root.GetAttributeValue("id") : root.GetAttributeValue("description"),
                            Project = project.GetAttributeValue("name"),
                            Uri = root.GetAttributeValue("webUrl")
                        });

                }
            }
            return BuildDefinitionCache.ContainsKey(buildTypeId)
                ? BuildDefinitionCache[buildTypeId]
                : null;
        }

        private Build GetBuild(string buildId)
        {
            var xml = WebHelper.DownloadString(string.Format(BuildUriFormat, Url, buildId), Username, Password);
            var root = XDocument.Parse(xml).Root;
            if (root != null)
            {
                var startDate = root.GetElementValue("startDate");
                var finishDate = root.GetElementValue("finishDate");
                var duration = (startDate != null && finishDate != null)
                    ? DateTimeOffset.ParseExact(finishDate, "yyyyMMddTHHmmsszzz", CultureInfo.InvariantCulture) - DateTimeOffset.ParseExact(startDate, "yyyyMMddTHHmmsszzz", CultureInfo.InvariantCulture)
                    : TimeSpan.Zero;
                var buildType = root.Element("buildType");
                return (buildType != null)
                    ? new Build {
                        Duration = duration.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                        Message = root.GetElementValue("statusText"),
                        Number = root.GetAttributeValue("number"),
                        Start = startDate,
                        Status = root.GetAttributeValue("status"),
                        Uri = root.GetAttributeValue("webUrl"),
                        BuildDefinition = GetBuildDefinition(buildType.GetAttributeValue("id"))
                    }
                    : null;
            }
            return null;
        }

        public IEnumerable<Build> GetBuilds(string vcsRevision)
        {
            var changeId = GetChangeId(vcsRevision);
            if (changeId != null)
            {
                var buildIds = GetBuildIds(changeId);
                var builds = buildIds.Select(GetBuild).ToArray();
                return builds;
            }
            return Enumerable.Empty<Build>();
        }
    }
}
