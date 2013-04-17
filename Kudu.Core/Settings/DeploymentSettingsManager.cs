using System;
using System.Collections.Generic;
using System.Linq;
using Kudu.Contracts.Settings;
using Kudu.Core.Deployment;
using XmlSettings;

namespace Kudu.Core.Settings
{
    public class DeploymentSettingsManager : IDeploymentSettingsManager
    {
        private readonly PerSiteSettingsProvider _perSiteSettings;
        private readonly IEnumerable<ISettingsProvider> _settingsProviders;

        public DeploymentSettingsManager(ISettings settings)
            : this(settings, new ISettingsProvider[] { new EnvironmentSettingsProvider(), new DefaultSettingsProvider() })
        {
        }

        internal DeploymentSettingsManager(ISettings settings, IEnumerable<ISettingsProvider> settingsProviders)
            : this(new PerSiteSettingsProvider(settings), settingsProviders)
        {
        }

        private DeploymentSettingsManager(PerSiteSettingsProvider perSiteSettings, IEnumerable<ISettingsProvider> settingsProviders)
        {
            _perSiteSettings = perSiteSettings;

            var settingsProvidersList = new List<ISettingsProvider>();
            settingsProvidersList.Add(_perSiteSettings);
            settingsProvidersList.AddRange(settingsProviders);
            _settingsProviders = settingsProvidersList;
        }

        public IDeploymentSettingsManager BuildPerDeploymentSettingsManager(string path)
        {
            return new DeploymentSettingsManager(
                _perSiteSettings,
                new ISettingsProvider[]
                {
                    _perSiteSettings, new EnvironmentSettingsProvider(), new DeploymentConfiguration(path), new DefaultSettingsProvider()
                });
        }

        public IEnumerable<KeyValuePair<string, string>> GetValues()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var reverseProviders = _settingsProviders.Reverse<ISettingsProvider>();
            foreach (var provider in reverseProviders)
            {
                foreach (var keyValuePair in provider.GetValues())
                {
                    values[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            return values;
        }

        public string GetValue(string key, bool preventUnification)
        {
            if (preventUnification)
            {
                return _perSiteSettings.GetValue(key);
            }

            foreach (var provider in _settingsProviders)
            {
                var value = provider.GetValue(key);
                if (!String.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return null;
        }

        public void SetValue(string key, string value)
        {
            // Note that this only applies to persisted per-site settings
            _perSiteSettings.SetValue(key, value);
        }

        public void DeleteValue(string key)
        {
            // Note that this only applies to persisted per-site settings
            _perSiteSettings.DeleteValue(key);
        }
    }
}
