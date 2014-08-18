using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using RallyIntegrator.Library.Handler;
using RallyIntegrator.Library.Model;

namespace RallyIntegrator.Library
{
    public static class Extensions
    {
        public static Changeset ToRallyChangeset(this Changeset tfsChangeset, Ldap ldap, Handler.Rally rally)
        {
            if (tfsChangeset == null)
                return null;
            var username = tfsChangeset.Author.Split('\\').Last();
            var email = ldap.GetEmail(username);
            var rallyUserId = rally.GetObjectId("user", "EmailAddress", email) ?? rally.GetObjectId("user", "EmailAddress", email.ToLower());
            var rallyRepositoryId = rally.GetObjectId("scmrepository", "Name", Tfs.Config.Repository);
            return new Changeset
            {
                Author = rallyUserId,
                Changes = tfsChangeset.Changes,
                Builds = tfsChangeset.Builds,
                CommitTimestamp = tfsChangeset.CommitTimestamp,
                Message = tfsChangeset.Message,
                Repository = rallyRepositoryId,
                Revision = tfsChangeset.Revision,
                Uri = tfsChangeset.Uri
            };
        }


        public static IEnumerable<string> GetRallyReferences(this Changeset changeset)
        {
            if (string.IsNullOrWhiteSpace(changeset.Message))
                return Enumerable.Empty<string>();
            var regex = new Regex(@"((?i:(de|ta|us))\d{5})");
            return (regex.Matches(changeset.Message).Cast<object>().Where(x => x != null).Select(x => x.ToString().ToUpper()));
        }

        public static bool ContainsRallyReference(this Changeset changeset)
        {
            if (string.IsNullOrWhiteSpace(changeset.Message))
                return false;
            var isMatch = new Regex(@"((?i:(de|ta|us))\d{5})").IsMatch(changeset.Message);
            return isMatch;
        }

        public static bool ContainsAccessibleRallyReference(this Changeset changeset, Handler.Rally rally)
        {
            var rallyRefs = changeset.GetRallyReferences().ToArray();
            string type;
            var accessibleReferenceFound = rallyRefs.Any(x => rally.GetReferenceObjectId(x, out type) != null);
            return accessibleReferenceFound;
        }

        public static bool IsRelevant(this Changeset changeset, Handler.Rally rally)
        {
            return changeset != null
                && changeset.Changes.Any(x => x.Path.StartsWith(Tfs.Config.Repository))
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

        #region XML Extensions

        public static string GetAttributeValue(this XElement xElement, XName xName)
        {
            return xElement.Attribute(xName) != null && !string.IsNullOrWhiteSpace(xElement.Attribute(xName).Value)
                ? xElement.Attribute(xName).Value
                : null;
        }

        public static string GetElementValue(this XElement xElement, XName xName)
        {
            var element = xElement.Element(xName);
            return element != null && !string.IsNullOrWhiteSpace(element.Value)
                ? element.Value
                : null;
        }

        #endregion
    }
}
