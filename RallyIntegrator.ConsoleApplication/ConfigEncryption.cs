#if !DEBUG
#define ENCRYPT_CONFIG
#endif

using System.Configuration;
using System.Diagnostics;

namespace RallyIntegrator.ConsoleApplication
{
    static class ConfigEncryption
    {
        [Conditional("ENCRYPT_CONFIG")]
        internal static void EncryptAppSettings()
        {
            EncryptConfigSection("appSettings");
        }

        private static void EncryptConfigSection(string sectionKey)
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
