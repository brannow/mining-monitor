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

            // ccminer config
            string ccminerHost = file.Read("host", "ccminer");
            if (ccminerHost.Equals(""))
            {
                ccminerHost = "127.0.0.1";
            }
            this.configData.Add("ccminer.host", ccminerHost);
            string ccminerPort = file.Read("port", "ccminer");
            if (ccminerPort.Equals(""))
            {
                ccminerPort = "4068";
            }
            this.configData.Add("ccminer.port", ccminerPort);

            this.configData.Add("smartSocket.host", file.Read("host", "smartSocket"));

            this.configData.Add("user.key", file.Read("key", "user"));
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

        public string CCMinerPort()
        {
            return this.GetConfigValue("ccminer.port");
        }

        public string CCMinerHost()
        {
            return this.GetConfigValue("ccminer.host");
        }

        public string SmartSocketHost()
        {
            return this.GetConfigValue("smartSocket.host");
        }

        public string UserKey()
        {
            return this.GetConfigValue("user.key");
        }
    }
}
