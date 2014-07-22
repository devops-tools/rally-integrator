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
        public IEnumerable<Build> Builds { get; set; }
    }
}
