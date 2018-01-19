using System;

using FuyukaiMiningClient.Classes.HTTP;
using FuyukaiMiningClient.Classes.TelemetryData;

namespace FuyukaiMiningClient.Classes
{
    class Telemetry
    {
        private Config config;
        private Request request;
        private Rig rig;

        private uint resetCount = 0;

        public Telemetry(Config cfg)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            this.config = cfg;
            this.request = new Request(cfg.ServerAddress(), this);
            this.rig = new Rig(cfg, this);
        }

        public void Send()
        {
            if (resetCount >= 3)
            {
                Program.WriteLine("Try to Reset all Dangling connections", false, true);
                this.rig.Clear();
                this.request = new Request(this.config.ServerAddress(), this);
                this.rig = new Rig(this.config, this);
            }

            Program.WriteLine("Will Collect Data", false, true);
            if (this.rig.IsCollectorIdle())
            {
                resetCount = 0;
                Program.WriteLine("Collect Data");
                this.rig.Collect();
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
            this.request.SendData(jsonData);
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
            this.rig.Clear();
        }
    }
}
