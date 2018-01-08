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

        private uint resetCount = 0;

        public Telemetry(Config cfg)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            this.config = cfg;
            Telemetry.request = new Request(cfg.ServerAddress(), this);
            Telemetry.rig = new Rig(cfg, this);
        }

        public void Send()
        {
            if (resetCount >= 3)
            {
                Program.WriteLine("Try to Reset all Dangling connections", false, true);
                Telemetry.rig.Clear();
                Telemetry.request = new Request(this.config.ServerAddress(), this);
                Telemetry.rig = new Rig(this.config, this);
            }

            Program.WriteLine("Will Collect Data", false, true);
            if (Telemetry.rig.IsCollectorIdle())
            {
                resetCount = 0;
                Program.WriteLine("Collect Data");
                Telemetry.rig.Collect();
            }
            else 
            {
                ++resetCount;
            }
        }

        public void CollectingDone(Rig r, string jsonData)
        {
            Program.WriteLine("Sending...");
            Program.WriteLine(jsonData, false, true);
            Telemetry.request.SendData(jsonData);
        }

        public void CollectingError(Rig r)
        {
            Program.WriteLine("Collecting Error");
            this.Clear();
        }

        public void SendingDone(Request r, string response)
        {
            if (response.Equals("{\"status\":0}"))
            {
                Program.WriteLine("Success");
            }
            else
            {
                Program.WriteLine("Remote Server Error", false, true);
                Program.WriteLine("\t" + response, false, true);
            }
            
            this.Clear();
        }

        public void SendingError(Request r, Exception e)
        {
            Program.WriteLine("Sending Error: " + e.Message);
            this.Clear();
        }

        public void Clear()
        {
            Program.WriteLine("CleanUp Data", false, true);
            Telemetry.rig.Clear();
        }
    }
}
