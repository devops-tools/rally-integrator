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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Processing revisions {0}.", string.Join(",", revisions));
            Console.ResetColor();
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
        }
    }
}
