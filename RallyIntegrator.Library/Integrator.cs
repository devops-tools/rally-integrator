using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RallyIntegrator.Library.Handler;

namespace RallyIntegrator.Library
{
    public static class Integrator
    {
        static readonly Ldap Ldap = new Ldap();
        static readonly Handler.Rally Rally = new Handler.Rally();

        public static void Process(IEnumerable<int> revisions)
        {
            revisions = revisions.ToArray();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Processing revisions {0}.", string.Join(",", revisions));
            Console.ResetColor();
            var tfsChangesets = revisions.Select(revision => new Tfs().GetChangeset(revision.ToString(CultureInfo.InvariantCulture))).ToArray();
            var changesets = tfsChangesets.Select(tfsChangeset => tfsChangeset.ToRallyChangeset(Ldap, Rally))
                .Where(x => x != null && x.IsRelevant(Rally)).ToArray();
            foreach (var changeset in changesets)
                Rally.Link(Rally.Add(changeset), changeset.GetRallyReferences());
        }
    }
}
