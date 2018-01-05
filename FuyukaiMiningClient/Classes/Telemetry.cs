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
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            this.config = cfg;
            Telemetry.request = new Request(cfg.ServerAddress(), this);
            Telemetry.rig = new Rig(cfg, this);
        }

        public void Send()
        {
            Console.WriteLine("TRY Send -> Collect");
            if (Telemetry.rig.IsCollectorIdle()) {
                Console.WriteLine("Send -> Collect");
                Telemetry.rig.Collect();
            }
        }

        public void CollectingDone(Rig r, string jsonData)
        {
            Console.WriteLine(jsonData);
            Telemetry.request.SendData("{}");
        }

        public void CollectingError(Rig r)
        {
            Console.WriteLine("CollectingError");
            this.Clear();
        }

        public void SendingDone(Request r, string response)
        {
            Console.WriteLine("SEND DONE:");
            Console.WriteLine(response);
            this.Clear();
        }

        public void SendingError(Request r, Exception e)
        {
            Console.WriteLine("SEND ERROR:");
            Console.WriteLine(e.Message);
            this.Clear();
        }

        public void Clear()
        {
            Telemetry.rig.Clear();
        }
    }
}
