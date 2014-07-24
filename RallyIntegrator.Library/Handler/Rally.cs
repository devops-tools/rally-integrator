using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Rally.RestApi;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library.Handler
{
    public class Rally
    {
        private static RallyRestApi _api;
        private static RallyRestApi Api { get { return _api ?? (_api = new RallyRestApi(Username, Password)); } }
        private static string Username { get { return ConfigurationManager.AppSettings.Get("RallyUsername"); } }
        private static string Password { get { return ConfigurationManager.AppSettings.Get("RallyPassword"); } }
        private static string Project { get { return ConfigurationManager.AppSettings.Get("RallyProject"); } }

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
            var x = new Dictionary<string, object>();
            foreach (var reference in references)
            {
                string type;
                var objectId = GetReferenceObjectId(reference, out type);
                if (objectId != null && type != null)
                    x.Add(string.Format("_ref({0})", reference), objectId);
            }
            var toUpdate = new DynamicJsonObject();
            toUpdate["Artifacts"] = new DynamicJsonObject(x);
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
            if (changesetObjectId == null)
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
                    { "Project", Project },
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
            if (buildObjectId == null)
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
            var json = WebHelper.DownloadString(string.Concat(changesetObjectId, "/Changes"), Username, Password);
            var x = JObject.Parse(json)["QueryResult"];
            return (int) x["TotalResultCount"] > 0
                ? x["Results"].Select(change => (string)change["PathAndFilename"])
                : new string[]{};
        }
    }
}
