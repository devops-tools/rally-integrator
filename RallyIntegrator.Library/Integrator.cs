using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RallyIntegrator.Library.Handler;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library
{
    public static class Integrator
    {
        static readonly Ldap Ldap = new Ldap();
        static readonly TeamCity TeamCity = new TeamCity();
        static readonly Handler.Rally Rally = new Handler.Rally();

        public static void Process(IEnumerable<int> revisions)
        {
            revisions = revisions.ToArray();
            Console.WriteLine("Processing revisions {0}.", string.Join(",", revisions));
            var tfsChangesets = revisions.Select(revision =>
            {
                var tfsChangeset = new Tfs().GetChangeset(revision.ToString(CultureInfo.InvariantCulture));
                if (tfsChangeset != null)
                    tfsChangeset.Builds = TeamCity.GetBuilds(tfsChangeset.Revision);
                return tfsChangeset;
            }).ToArray();
            var changesets = tfsChangesets.Where(x => x!= null).Select(tfsChangeset => tfsChangeset.ToRallyChangeset(Ldap, Rally))
                .Where(x => x != null && x.IsRelevant(Rally)).ToArray();
            foreach (var changeset in changesets)
                Process(changeset);
        }

        private static void Process(Changeset changeset)
        {
            var changesetObjectId = Rally.Add(changeset);
            foreach (var build in changeset.Builds)
                Rally.Add(build, changesetObjectId);
            Rally.Link(changesetObjectId, changeset.GetRallyReferences());
            Console.WriteLine("Revision {0} ({1}) processed: {2} change{3}, {4} build{5}.",
                changeset.Revision,
                string.Join(", ", changeset.GetRallyReferences()),
                changeset.Changes.Count(),
                changeset.Changes.Count() > 1 ? "s" : string.Empty,
                changeset.Builds.Count(),
                changeset.Builds.Count() > 1 ? "s" : string.Empty);
        }
    }
}
