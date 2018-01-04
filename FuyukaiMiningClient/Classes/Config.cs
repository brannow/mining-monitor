using System.Collections.Generic;

namespace FuyukaiMiningClient.Classes
{
    class Config
    {
        Dictionary<string, string> configData;

        public Config()
        {
            this.configData = new Dictionary<string, string>();
            this.LoadConfig();
        }

        private void LoadConfig(IniFile file = null)
        {
            // remove prevous config data
            this.configData.Clear();
            

            if (file == null) {
                file = new IniFile("config.ini");
            }

            // server settings
            this.configData.Add("server.server", file.Read("server", "server"));

            // rig settings
            string rigName = file.Read("name", "rig");
            if (rigName.Equals("")) {
                rigName = "Rig#01";
            } 
            this.configData.Add("rig.name", rigName);
        }

        private string GetConfigValue(string key)
        {
            if (!this.configData.TryGetValue(key, out string value))
            {
                return "";
            }

            return value;
        }

        public string ServerAddress()
        {
            return this.GetConfigValue("server.server");
        }

        public string RigName()
        {
            return this.GetConfigValue("rig.name");
        }
    }
}
