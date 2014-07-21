using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using Rally.RestApi;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library.Api
{
    public class Rally
    {
        private static RallyRestApi _api;
        private static RallyRestApi Api { get { return _api ?? (_api = new RallyRestApi(Username, Password)); } }
        private static string Username { get { return ConfigurationManager.AppSettings.Get("RallyUsername"); } }
        private static string Password { get { return ConfigurationManager.AppSettings.Get("RallyPassword"); } }


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

        public string Add(string changesetObjectId, Change change)
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
                    //{"Builds", new Dictionary<string, object>{ { "Build", "/project/17806613497" } }},
                    //{"Changes", new Dictionary<string, object>{ { "Change", "/project/17806613497" } }},
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
                    Add(changesetObjectId, change);
            }
            return changesetObjectId;
        }

        private IEnumerable<string> GetChanges(string changesetObjectId)
        {
            var json = new WebClient { Credentials = new NetworkCredential(Username, Password) }.DownloadString(string.Concat(changesetObjectId, "/Changes"));
            var x = JObject.Parse(json)["QueryResult"];
            return (int) x["TotalResultCount"] > 0
                ? x["Results"].Select(change => (string)change["PathAndFilename"])
                : new string[]{};
        }
    }

    
            //var projectRef = "https://rally1.rallydev.com/slm/webservice/v2.0/project/17806613497";
            //var repositoryRef = "https://rally1.rallydev.com/slm/webservice/v2.0/scmrepository/20691322881";
            //var changesetRef = "https://rally1.rallydev.com/slm/webservice/v2.0/changeset/20691809495";

            //Initialize the REST API
            //RallyRestApi restApi = new RallyRestApi("robin.thijssen@ihs.com", "R@llydev");

            //var task = restApi.GetByReference("/task/20461118446");
            //var changeset = restApi.GetByReference("/changeset/20691809495");
            //var artifacts = changeset["Artifacts"];
            //artifacts.Add(task);
            //changeset["Artifacts"] = new Dictionary<string, object> { { "_ref", "20461118446" } };
            //var updateResult = restApi.Update("/changeset/20691809495", changeset);
            //var toUpdate = new DynamicJsonObject();
            //toUpdate["Changesets"] = new Dictionary<string, object> { { "Changeset", "/changeset/20691809495" } };
            //var updateResult = restApi.Update("task", "20461118446", toUpdate);
            //var x = updateResult.ToString();

            //var changeset = new DynamicJsonObject(new Dictionary<string, object>
            //{
            //    {"Author", "/user/18948801540"},
            //    //{"Builds", new Dictionary<string, object>{ { "Build", "/project/17806613497" } }},
            //    //{"Changes", new Dictionary<string, object>{ { "Change", "/project/17806613497" } }},
            //    {"CommitTimestamp", "2014-07-17T11:02:15.207Z"},
            //    {"Message", "US21345: Implemented delete handler by adding delete messages to the queue (TA47420)."},
            //    {"Revision", "62748"},
            //    {"SCMRepository", "/scmrepository/20691322881"},
            //    {"Uri", "http://gda-tfs-01:8080/tfs/VersionControl/Changeset.aspx?artifactMoniker=62748"}
            //});
            //var createResult = restApi.Create("changeset", changeset);
            //var changesetRef = createResult.Reference;

            //var scmrepository = new DynamicJsonObject(new Dictionary<string, object>
            //{
            //    { "Name", "$/Castle/Connect" },
            //    { "Description", "IHS Connect" },
            //    { "SCMType", "TFS" },
            //    { "Uri", "http://gda-tfs-01:8080/tfs/emea-gdacollection" },
            //    { "Projects", new Dictionary<string, object>{ { "Project", "/project/17806613497" } } }
            //});
            //var createResult = restApi.Create("scmrepository", scmrepository);
            //var repoRef = createResult.Reference;

            ////Update
            //item = new DynamicJsonObject();
            //item["Description"] = "Created with API";
            //var updateResult = restApi.Update(createResult.Reference, item);

            ////Get
            //item = restApi.GetByReference(createResult.Reference);

            ////Query for items
            //var request = new Request("userstory")
            //{
            //    Fetch = new List<string>()
            //    {
            //        "Name",
            //        "Description",
            //        "FormattedID"
            //    },
            //    Query = new Query("Name", Query.Operator.Equals, "Test User Story")
            //};

            //var queryResult = restApi.Query(request);
            //foreach(var result in queryResult.Results)
            //{
            //    //Process item
            //}

            //Delete the item
            //OperationResult deleteResult = restApi.Delete(createResult.Reference);
}
