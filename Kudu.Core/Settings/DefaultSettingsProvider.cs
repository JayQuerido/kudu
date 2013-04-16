using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using Kudu.Contracts.Settings;

namespace Kudu.Core.Settings
{
    public class DefaultSettingsProvider : ISettingsProvider
    {
        private readonly Dictionary<string, string> _settings;

        public DefaultSettingsProvider()
        {
            // Ideally, these default settings would live in Kudu's web.config. However, we also need them in 
            // kudu.exe, so they actually need to be in a shared config file. For now, it's easier to hard code
            // the defaults, since things like 'branch' will rarely want a different global default

            _settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                { SettingsKeys.DeploymentBranch, "master" },
                { SettingsKeys.TraceLevel, ((int)DeploymentSettingsExtension.DefaultTraceLevel).ToString() },
                { SettingsKeys.CommandIdleTimeout, ((int)DeploymentSettingsExtension.DefaultCommandIdleTimeout.TotalSeconds).ToString() },
                { SettingsKeys.LogStreamTimeout, ((int)DeploymentSettingsExtension.DefaultLogStreamTimeout.TotalSeconds).ToString() },
                { SettingsKeys.BuildArgs, "" }
            };
        }

        public IEnumerable<KeyValuePair<string, string>> GetValues()
        {
            return _settings;
        }

        public string GetValue(string key)
        {
            string value;
            _settings.TryGetValue(key, out value);
            return value;
        }
    }
}
