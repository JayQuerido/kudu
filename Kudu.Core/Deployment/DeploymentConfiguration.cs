using System;
using System.Collections.Generic;
using System.IO;
using Kudu.Contracts.Settings;
using Kudu.Core.Infrastructure;
using Kudu.Core.Settings;

namespace Kudu.Core.Deployment
{
    public class DeploymentConfiguration : BasicSettingsProvider
    {
        internal const string DeployConfigFile = ".deployment";

        public DeploymentConfiguration(string path)
            : base(GetValues(path))
        {
        }

        private static IDictionary<string, string> GetValues(string path)
        {
            var iniFile = new IniFile(Path.Combine(path, DeployConfigFile));
            var values = iniFile.GetSectionValues("config");
            if (values == null)
            {
                values = new Dictionary<string, string>();
            }
            return values;
        }
    }
}
