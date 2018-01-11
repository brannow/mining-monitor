using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

using FuyukaiHWMonitor.Hardware;
using FuyukaiMiningClient.Classes;

namespace FuyukaiMiningClient.Classes.TelemetryData
{
    class Rig
    {
        private static Computer computer = new Computer();
        private static Gpu[] gpus;
        private static Config config;
        private bool collectorStatus = true;
        private Telemetry del;
        private static TPLink.SmartPowerSocket smartPlug;
        private static string userKey = "";

        // rig Data
        private string name = "";
        // TEMP RIG DATA
        private float rigHashRate = 0;
        private long minerUptime = 0;
        private CCMinerCollector ccminerCollector;

        public Rig(Config cfg, Telemetry del)
        {
            this.del = del;
            Rig.config = cfg;
            Rig.userKey = cfg.UserKey();
            Rig.smartPlug = new TPLink.SmartPowerSocket(cfg.SmartSocketHost());
            Rig.computer.CPUEnabled = true;
            Rig.computer.GPUEnabled = true;
            Rig.computer.RAMEnabled = true;
            Rig.computer.Open();

            IHardware[] hwGpus = Rig.computer.GetGPU();
            List<Gpu> list = new List<Gpu>();
            foreach (IHardware hw in hwGpus) {
                Gpu g = new Gpu(hw);
                list.Add(g);
            }
            Rig.gpus = list.ToArray();
            this.name = cfg.RigName();
            this.ccminerCollector = new CCMinerCollector(cfg.CCMinerHost(), cfg.CCMinerPort(), this);
        }

        private Gpu GetGpuByBus(uint bus)
        {
            foreach (Gpu gpuToCheck in Rig.gpus) {
                if (gpuToCheck.CompareBusId(bus))
                {
                    return gpuToCheck;
                }
            }

            return null;
        }

        private float CpuUsage()
        {
            return Rig.computer.CPUUsage();
        }

        private float CpuTemp()
        {
            return Rig.computer.CPUTemp();
        }

        private long Uptime()
        {
            return Hardware.UptimeInMinutes();
        }

        private float RamUsage()
        {
            return Rig.computer.RamUsage();
        }

        private string Identifier()
        {
            return Hardware.HardDriveGUID();
        }

        private string UserKey()
        {
            return userKey;
        }

        private string Name()
        {
            return this.name;
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

            r.AppendFormat("\"user-key\":\"{0}\",", this.UserKey());
            r.AppendFormat("\"identifier\":\"{0}\",", this.Identifier());
            r.AppendFormat("\"name\":\"{0}\",", this.Name());
            r.AppendFormat("\"os-uptime\":{0},", this.Uptime());
            r.AppendFormat("\"cpu-usage\":{0},", this.CpuUsage());
            r.AppendFormat("\"cpu-temp\":{0},", this.CpuTemp());
            r.AppendFormat("\"ram-usage\":{0},", this.RamUsage());
            r.AppendFormat("\"ccminer-uptime\":{0},", (long)this.minerUptime / 60);
            r.AppendFormat("\"ccminer-khash-rate\":{0},", this.rigHashRate);

            Program.WriteLine("Parse GPU DATA", false, true);
            List<string> gpujsonStrings = new List<string>();
            foreach (Gpu g in Rig.gpus)
            {
                gpujsonStrings.Add(g.GpuDataToJson());
            }
            r.AppendFormat("\"gpus\":[{0}],", string.Join(",", gpujsonStrings));

            Program.WriteLine("Look for PowerSocket", false, true);
            TPLink.Power p = Rig.smartPlug.GetPower();
            r.AppendFormat("\"power\":{0},", p.watt);
            r.AppendFormat("\"kwh\":{0}", p.kwh);

            r.Append("}");
            return r.ToString();
        }

        public void Collect()
        {
            if (this.UserKey().Length > 0)
            {
                collectorStatus = false;
                Program.WriteLine("Hardware updated", false, true);
                Rig.computer.UpdateHardware();
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

            Program.WriteLine("Process Summary", false, true);
            this.rigHashRate = result.summaryResult.rigHashRate;
            this.minerUptime = result.summaryResult.minerUptime;

            Program.WriteLine("Process HWInfo", false, true);
            foreach(GPUHWInfoResult hwResult in result.GPUHWInfoResult)
            {
                Gpu g = this.GetGpuByBus(hwResult.bus);
                if (g != null)
                {
                    g.temp = hwResult.temperature;
                }
            }

            Program.WriteLine("Process Threads", false, true);
            foreach (GPUThreadResult threadResult in result.GPUThreadResult)
            {
                Gpu g = this.GetGpuByBus(threadResult.bus);
                if (g != null)
                {
                    g.watt = threadResult.watt;
                    g.hashRate = threadResult.hashRateK;
                    g.hashRateWatt = threadResult.hashRateK / threadResult.watt;
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
            // clear temp data
            rigHashRate = 0;
            minerUptime = 0;

            // clear ccminer data
            ccminerCollector.Clear();
            foreach (Gpu g in Rig.gpus)
            {
                g.Clear();
            }

            // unlock Rig
            collectorStatus = true;
        }
    }
}
