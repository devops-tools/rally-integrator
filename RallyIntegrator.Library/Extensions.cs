using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using RallyIntegrator.Library.Handler;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library
{
    public static class Extensions
    {
        public static Changeset ToRallyChangeset(this Changeset tfsChangeset, Ldap ldap, Api.Rally rally)
        {
            if (tfsChangeset == null)
                return null;
            var username = tfsChangeset.Author.Split('\\').Last();
            var email = ldap.GetEmail(username);
            var rallyUserId = rally.GetObjectId("user", "EmailAddress", email) ?? rally.GetObjectId("user", "EmailAddress", email.ToLower());
            var rallyRepositoryId = rally.GetObjectId("scmrepository", "Name", Tfs.Repository);
            return new Changeset
            {
                Author = rallyUserId,
                Changes = tfsChangeset.Changes,
                CommitTimestamp = tfsChangeset.CommitTimestamp,
                Message = tfsChangeset.Message,
                Repository = rallyRepositoryId,
                Revision = tfsChangeset.Revision,
                Uri = tfsChangeset.Uri
            };
        }


        public static IEnumerable<string> GetRallyReferences(this Changeset changeset)
        {
            var regex = new Regex(@"((?i:(de|ta|us))\d{5})");
            return (regex.Matches(changeset.Message).Cast<object>().Where(x => x != null).Select(x => x.ToString().ToUpper()));
        }

        public static bool ContainsRallyReference(this Changeset changeset)
        {
            var isMatch = new Regex(@"((?i:(de|ta|us))\d{5})").IsMatch(changeset.Message);
            return isMatch;
        }

        public static bool ContainsAccessibleRallyReference(this Changeset changeset, Api.Rally rally)
        {
            var rallyRefs = changeset.GetRallyReferences().ToArray();
            string type;
            var accessibleReferenceFound = rallyRefs.Any(reference => rally.GetReferenceObjectId(reference, out type) != null);
            return accessibleReferenceFound;
        }

        public static bool IsRelevant(this Changeset changeset, Api.Rally rally)
        {
            return changeset.Changes.Any(x => x.Path.StartsWith(Tfs.Repository))
                && changeset.ContainsRallyReference()
                && changeset.ContainsAccessibleRallyReference(rally);
        }

        public static IEnumerable<IEnumerable<T>> Partition<T> (this IEnumerable<T> source, int size)
        {
            T[] array = null;
            var count = 0;
            foreach (var item in source)
            {
                if (array == null)
                {
                    array = new T[size];
                }
                array[count] = item;
                count++;
                if (count == size)
                {
                    yield return new ReadOnlyCollection<T>(array);
                    array = null;
                    count = 0;
                }
            }
            if (array != null)
            {
                Array.Resize(ref array, count);
                yield return new ReadOnlyCollection<T>(array);
            }
        }
    }
}
