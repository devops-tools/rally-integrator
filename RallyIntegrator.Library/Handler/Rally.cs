using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Rally.RestApi;
using RallyIntegrator.Library.Config;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library.Handler
{
    public class Rally
    {
        private static readonly RallyApiSection Config = (RallyApiSection)ConfigurationManager.GetSection("integrationProviders/rally");
        private static RallyRestApi _api;
        private static RallyRestApi Api { get { return _api ?? (_api = new RallyRestApi(Config.Username, Config.Password)); } }

        public string GetObjectId(string objectType, string property, string value)
        {
            var queryResult = Api.Query(new Request(objectType)
            {
                Fetch = new List<string> { "FormattedID" },
                Query = new Query(property, Query.Operator.Equals, value)
            });
            if (queryResult.Results.Any())
            {
                var result = queryResult.Results.First();
                return (string) result["_ref"];
            }
            return null;
        }

        public string GetBuildObjectId(Build build)
        {
            var queryResult = Api.Query(new Request("build")
            {
                Fetch = new List<string> { "Uri" },
                Query = new Query("Number", Query.Operator.Equals, build.Number)
            });
            if (queryResult.Results.Any(x => build.Uri.Equals((string)x["Uri"], StringComparison.InvariantCultureIgnoreCase)))
            {
                var result = queryResult.Results.First(x => build.Uri.Equals((string)x["Uri"], StringComparison.InvariantCultureIgnoreCase));
                return (string) result["_ref"];
            }
            return null;
        }

        public string GetReferenceObjectId(string reference, out string type)
        {
            switch (reference.Substring(0, 2))
            {
                case "US":
                    type = "HierarchicalRequirement";
                    break;
                case "TA":
                    type = "task";
                    break;
                case "DE":
                    type = "defect";
                    break;
                default:
                    type = null;
                    break;
            }
            return GetObjectId(type, "FormattedID", reference);
        }

        public void Link(string changesetObjectId, IEnumerable<string> references)
        {
            references = references.ToArray();
            string type;
            var newArtifacts = references.ToDictionary(r => string.Format("_ref({0})", r), r => (object)GetReferenceObjectId(r, out type));

            var existingArtifacts = GetArtifacts(changesetObjectId);
            if (newArtifacts.Values.SequenceEqual(existingArtifacts))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Links to {0} exist for changeset {1}.", string.Join(",", references), changesetObjectId);
                Console.ResetColor();
            }
            else
            {
                var toUpdate = new DynamicJsonObject();
                toUpdate["Artifacts"] = new DynamicJsonObject(newArtifacts);
                var updateResult = Api.Update(changesetObjectId, toUpdate);
                if (updateResult.Success)
                    Console.WriteLine("{0} linked to changeset {1}", string.Join(",", references), changesetObjectId);
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Failed to link {0} to changeset {1}", string.Join(",", references), changesetObjectId);
                    Console.ResetColor();
                }
            }
        }

        public string Add(Change change, string changesetObjectId)
        {
            var changeJson = new DynamicJsonObject(new Dictionary<string, object>
                        {
                            { "Action", change.Action },
                            { "PathAndFilename", change.Path },
                            { "Extension", Path.GetExtension(change.Path) },
                            { "Uri", change.Uri },
                            { "Changeset", changesetObjectId }
                        });
            var changeCreateResult = Api.Create("change", changeJson);
            if (changeCreateResult.Success)
            {
                Console.WriteLine("{0} ({1}) linked to changeset {2}", change.Action, change.Path, changesetObjectId);
                return changeCreateResult.Reference;
            }
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Failed to link {0} ({1}) to changeset {2}", change.Action, change.Path, changesetObjectId);
            Console.ResetColor();
            return null;
        }

        public string Add(Changeset changeset)
        {
            var changesetObjectId = GetObjectId("changeset", "Revision", changeset.Revision);
            if (changesetObjectId != null)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Changeset revision {0} exists as {1}.", changeset.Revision, changesetObjectId);
                Console.ResetColor();
            }
            else
            {
                var changesetJson = new DynamicJsonObject(new Dictionary<string, object>
                {
                    { "Author", changeset.Author },
                    { "CommitTimestamp", changeset.CommitTimestamp },
                    { "Message", changeset.Message },
                    { "Revision", changeset.Revision },
                    { "SCMRepository", changeset.Repository },
                    { "Uri", changeset.Uri }
                });
                var changesetCreateResult = Api.Create("changeset", changesetJson);
                if (changesetCreateResult.Success)
                {
                    changesetObjectId = changesetCreateResult.Reference;
                    Console.WriteLine("Changeset revision {0} created as {1}.", changeset.Revision, changesetObjectId);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Failed to create changeset revision {0}.", changeset.Revision);
                    Console.ResetColor();
                }
            }
            if (changesetObjectId != null)
            {
                var known = GetChanges(changesetObjectId).ToArray();
                foreach (var change in changeset.Changes.Where(x => !known.Contains(x.Path)))
                    Add(change, changesetObjectId);
            }
            return changesetObjectId;
        }

        public string Add(BuildDefinition buildDefinition)
        {
            var buildDefinitionObjectId = GetObjectId("buildDefinition", "Name", buildDefinition.Name);
            if (buildDefinitionObjectId == null)
            {
                var buildDefinitionJson = new DynamicJsonObject(new Dictionary<string, object>
                {
                    { "Name", buildDefinition.Name },
                    { "Description", buildDefinition.Description },
                    { "Project", string.Format(Config.ProjectUriFormat, Config.Url, Config.Project) },
                    { "Uri", buildDefinition.Uri }
                });
                var createResult = Api.Create("buildDefinition", buildDefinitionJson);
                if (createResult.Success)
                {
                    buildDefinitionObjectId = createResult.Reference;
                    Console.WriteLine("Build definition {0} created as {1}.", buildDefinition.Name, buildDefinitionObjectId);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Failed to create build definition {0}.", buildDefinition.Name);
                    Console.ResetColor();
                }
            }
            return buildDefinitionObjectId;
        }

        public string Add(Build build, string changesetObjectId)
        {
            var buildObjectId = GetBuildObjectId(build);
            if (buildObjectId != null)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Build {0}/{1} exists as {2}.", build.BuildDefinition.Name, build.Number, buildObjectId);
                Console.ResetColor();
            }
            else
            {
                var buildDefinitionObjectId = Add(build.BuildDefinition);
                if (buildDefinitionObjectId != null)
                {
                    var buildJson = new DynamicJsonObject(new Dictionary<string, object>
                    {
                        { "BuildDefinition", buildDefinitionObjectId },
                        { "Duration", build.Duration },
                        { "Message", build.Message },
                        { "Number", build.Number },
                        { "Status", build.Status },
                        { "Uri", build.Uri },
                        { "Changesets", new Dictionary<string, object> { { "_ref", changesetObjectId } } }
                    });
                    var createResult = Api.Create("build", buildJson);
                    if (createResult.Success)
                    {
                        buildObjectId = createResult.Reference;
                        Console.WriteLine("Build {0}:{1} created as {2}.", build.BuildDefinition.Name, build.Number, buildObjectId);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Failed to create build {0}:{1}.", build.BuildDefinition.Name, build.Number);
                        Console.ResetColor();
                    }
                }
            }
            return buildObjectId;
        }

        private IEnumerable<string> GetChanges(string changesetObjectId)
        {
            var json = WebHelper.DownloadString(string.Concat(changesetObjectId, "/Changes"), Config.Username, Config.Password);
            var queryResult = JObject.Parse(json)["QueryResult"];
            return (int)queryResult["TotalResultCount"] > 0
                ? queryResult["Results"].Select(x => x["PathAndFilename"].Value<string>())
                : Enumerable.Empty<string>();
        }

        private IEnumerable<string> GetArtifacts(string changesetObjectId)
        {
            var json = WebHelper.DownloadString(string.Concat(changesetObjectId, "/Artifacts"), Config.Username, Config.Password);
            var queryResult = JObject.Parse(json)["QueryResult"];
            return (int)queryResult["TotalResultCount"] > 0
                ? queryResult["Results"].Select(x => x["_ref"].Value<string>())
                : Enumerable.Empty<string>();
        }
    }
}
