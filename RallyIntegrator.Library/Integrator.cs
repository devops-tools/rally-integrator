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
            var tfs = new Tfs();
            var changesets = revisions.Select(x => tfs.GetChangeset(x.ToString(CultureInfo.InvariantCulture)))
                .Where(x => x.IsRelevant(Rally))
                .ToList();
            changesets.ForEach(x =>
            {
                var changeset = x.ToRallyChangeset(Ldap, Rally);
                changeset.Builds = TeamCity.GetBuilds(changeset.Revision);
                Process(changeset);
            });
            if (!changesets.Any())
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("No relevant revisions in set: {0}.", string.Join(",", revisions));
                Console.ResetColor();
            }
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
