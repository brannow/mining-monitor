using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FuyukaiMiningClient.Classes.HTTP;
using FuyukaiMiningClient.Classes.TelemetryData;

namespace FuyukaiMiningClient.Classes
{
    class Telemetry
    {
        private Config config;
        private static Request request;
        private static Rig rig;

        public Telemetry(Config cfg)
        {
            this.config = cfg;
            Telemetry.request = new Request(cfg.ServerAddress());
            Telemetry.rig = new Rig(cfg);
        }

        public void Collect()
        {
            Telemetry.rig.Collect();
        }

        public void Send()
        {
            Telemetry.request.SendData("{}");
        }

        public void Clear()
        {
            Telemetry.rig.Clear();
        }
    }
}
