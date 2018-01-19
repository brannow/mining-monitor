using FuyukaiLib;
using System.Collections.Generic;
using System.Text;


namespace FuyukaiMiningClient.Classes.TelemetryData
{
    class Rig
    {
        private bool collectorStatus = true;

        private Config config;
        private Telemetry del;
        
        private readonly string userKey = "";
        private readonly string name = "";

        private Hardware hardware = new Hardware();
        private CCMinerCollector ccminerCollector;
        private TPLink.SmartPowerSocket smartPlug;

        public Rig(Config cfg, Telemetry del)
        {
            this.del = del;
            this.config = cfg;
            this.userKey = cfg.UserKey();
            this.smartPlug = new TPLink.SmartPowerSocket(cfg.SmartSocketHost());

            this.name = cfg.RigName();
            this.ccminerCollector = new CCMinerCollector(cfg.CCMinerHost(), cfg.CCMinerPort(), this);
        }

        public bool IsCollectorIdle()
        {
            return collectorStatus;
        }

        private string RigDataToJsonString()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            StringBuilder r = new StringBuilder("{");
            Program.WriteLine("Parse Collected Data to Json", false, true);

            r.AppendFormat("\"user-key\":\"{0}\",", this.userKey);
            r.AppendFormat("\"identifier\":\"{0}\",", this.hardware.GetHardwareIdentifier());
            r.AppendFormat("\"name\":\"{0}\",", this.name);
            r.AppendFormat("\"os-uptime\":{0},", this.hardware.GetUpTime());
            r.AppendFormat("\"cpu-usage\":{0},", this.hardware.GetCPUUsage());
            r.AppendFormat("\"cpu-temp\":{0},", this.hardware.GetCPUTemp());
            r.AppendFormat("\"ram-usage\":{0},", this.hardware.GetRamUsage());
            r.AppendFormat("\"khash-rate\":{0},", this.hardware.GetHashRate());

            Program.WriteLine("Look for PowerSocket", false, true);
            TPLink.Power p = this.smartPlug.GetPower();
            if (p.watt == 0)
            {
                // we asume a offset of 60 Watt for a complete rig
                p.watt = hardware.GetPower();
            }

            Program.WriteLine("Parse GPU DATA", false, true);
            r.AppendFormat("\"gpus\":{0},", this.hardware.GetGPUJson());
            r.AppendFormat("\"power\":{0},", p.watt);
            r.AppendFormat("\"kwh\":{0}", p.kwh);

            r.Append("}");
            return r.ToString();
        }

        public void Collect()
        {
            if (this.userKey.Length > 0)
            {
                collectorStatus = false;
                Program.WriteLine("Hardware updated", false, true);
                hardware.Update();
                // add external TEMP HDI SENSOR HERE

                ccminerCollector.CollectData();
            } else
            {
                Program.WriteLine("FATAL:UserKey not found please enter it in config.ini under [User] key=XXXXXXXX");
                Program.Abort();
            }
        }

        public void CCMinerCollectorDone(CCMinerResult result)
        {
            Program.WriteLine("All CCMiner Data aquired", false, true);

            if (result.GPUThreadResult != null)
            {
                Program.WriteLine("Process Threads", false, true);
                foreach (GPUThreadResult threadResult in result.GPUThreadResult)
                {
                    this.hardware.SetHashRateForGPUAtBus(threadResult.bus, threadResult.hashRateK);
                }
            }

            this.CollectingDone();
        }

        private void CollectingDone()
        {
            Program.WriteLine("All Data Collected", false, true);
            del.CollectingDone(this, this.RigDataToJsonString());
        }

        public void Clear()
        {
            Program.WriteLine("Clear Rig Data", false, true);
            // clear ccminer data
            ccminerCollector.Clear();
            // unlock Rig
            collectorStatus = true;
        }
    }
}
