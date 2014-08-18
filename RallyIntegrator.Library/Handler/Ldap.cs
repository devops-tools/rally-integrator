using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using RallyIntegrator.Library.Config;

namespace RallyIntegrator.Library.Handler
{
    public class Ldap
    {
        private static readonly LdapSection Config = (LdapSection)ConfigurationManager.GetSection("integrationProviders/ldap");

        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>(); 

        public string GetEmail(string username)
        {
            if (!Cache.ContainsKey(username))
            {
                var directorySearcher = new DirectorySearcher
                {
                    Filter = string.Format("({0}={1})", Config.AccountProperty, username)
                };
                directorySearcher.PropertiesToLoad.Add(Config.EmailProperty);
                var searchResult = directorySearcher.FindOne();
                if (searchResult != null && !Cache.ContainsKey(username))
                    Cache.Add(username, (string)searchResult.Properties[Config.EmailProperty][0]);
            }
            return Cache.ContainsKey(username) ? Cache[username] : null;
        }
    }
}
