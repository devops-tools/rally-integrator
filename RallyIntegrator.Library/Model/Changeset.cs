using System.Collections.Generic;

namespace RallyIntegrator.Library.Model
{
    public class Changeset
    {
        public string Author { get; set; }
        public string CommitTimestamp { get; set; }
        public string Message { get; set; }
        public string Revision { get; set; }
        public string Repository { get; set; }
        public string Uri { get; set; }
        public IEnumerable<Change> Changes { get; set; }
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
    }
}
