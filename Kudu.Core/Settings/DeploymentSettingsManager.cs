using System;
using System.Collections.Generic;
using System.Linq;
using Kudu.Contracts.Settings;
using XmlSettings;

namespace Kudu.Core.Settings
{
    public class DeploymentSettingsManager : IDeploymentSettingsManager
    {
        private readonly IEnumerable<ISettingsProvider> _settingsProviders;
        private readonly PerSiteSettingsProvider _perSiteSettings;

        public DeploymentSettingsManager(ISettings settings)
            : this(settings, new ISettingsProvider[] { new EnvironmentSettingsProvider(), new DefaultSettingsProvider() })
        {
        }

        internal DeploymentSettingsManager(ISettings settings, IEnumerable<ISettingsProvider> settingsProviders)
        {
            _perSiteSettings = new PerSiteSettingsProvider(settings);

            var providers = new List<ISettingsProvider>();
            providers.Add(_perSiteSettings);
            providers.AddRange(settingsProviders);
            _settingsProviders = providers;
        }

        public void SetValue(string key, string value)
        {
            // Note that this only applies to persisted per-site settings
            _perSiteSettings.SetValue(key, value);
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

        public void DeleteValue(string key)
        {
            // Note that this only applies to persisted per-site settings
            _perSiteSettings.DeleteValue(key);
        }
    }
}
