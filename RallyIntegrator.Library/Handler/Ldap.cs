using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;

namespace RallyIntegrator.Library.Handler
{
    public class Ldap
    {
        private static string LdapAccountProperty { get { return ConfigurationManager.AppSettings.Get("LDAPAccountProperty"); } }
        private static string LdapEmailProperty { get { return ConfigurationManager.AppSettings.Get("LDAPEmailProperty"); } }

        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>(); 

        public string GetEmail(string username)
        {
            if (!Cache.ContainsKey(username))
            {
                var directorySearcher = new DirectorySearcher
                {
                    Filter = string.Format("({0}={1})", LdapAccountProperty, username)
                };
                directorySearcher.PropertiesToLoad.Add(LdapEmailProperty);
                var searchResult = directorySearcher.FindOne();
                if (searchResult != null && !Cache.ContainsKey(username))
                    Cache.Add(username, (string)searchResult.Properties[LdapEmailProperty][0]);
            }
            return Cache.ContainsKey(username) ? Cache[username] : null;
        }
    }
}
