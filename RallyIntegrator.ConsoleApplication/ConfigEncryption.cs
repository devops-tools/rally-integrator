using System.Configuration;
using System.Diagnostics;

namespace RallyIntegrator.ConsoleApplication
{
    static class ConfigEncryption
    {
        internal static void EncryptConfigSection(string sectionKey)
        {
            if (!Debugger.IsAttached)
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var section = config.GetSection(sectionKey);
                if (section != null
                    && !section.SectionInformation.IsProtected
                    && !section.ElementInformation.IsLocked)
                {
                    section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    section.SectionInformation.ForceSave = true;
                    config.Save(ConfigurationSaveMode.Full);
                }
            }
        }
    }
}
