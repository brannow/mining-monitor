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
        private static CCMiner.CCMiner ccminer;
        private Telemetry del;
        private static TPLink.SmartPowerSocket smartPlug;
        private static string userKey = "";

        // rig Data
        private string name = "";
        // TEMP RIG DATA
        private float rigHashRate = 0;
        private long minerUptime = 0;

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

            ccminer = new CCMiner.CCMiner(cfg, this);
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

        public void CCMinerDone(CCMiner.CCMiner c, string summary, string hwInfo, string threads)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            Program.WriteLine("CCMiner: Data Collected ... start parsing", false, true);
            IList <IList<KeyValuePair<string, string>>> summaryList = CCMiner.CCMiner.ResultParser(summary);
            if (summaryList.Count() > 0)
            {
                IList<KeyValuePair<string, string>> summaryResult = summaryList[0];
                foreach (KeyValuePair<string, string> keyValue in summaryResult)
                {
                    if (keyValue.Key == "KHS")
                    {
                        rigHashRate = float.Parse(keyValue.Value);
                    }
                    if (keyValue.Key == "UPTIME")
                    {
                        minerUptime = long.Parse(keyValue.Value);
                    }
                }
            }

            IList<IList<KeyValuePair<string, string>>> hwInfoGpuList = CCMiner.CCMiner.ResultParser(hwInfo);
            if (hwInfoGpuList.Count() > 0)
            {
                foreach (IList<KeyValuePair<string, string>> gpuData in hwInfoGpuList)
                {
                    uint watt = 0;
                    int bus = 0;
                    float temp = 0;

                    // skip os data we only want GPU data
                    if (gpuData.Count() > 10)
                    {
                        foreach (KeyValuePair<string, string> keyValue in gpuData)
                        {
                            if (keyValue.Key == "POWER")
                            {
                                watt = uint.Parse(keyValue.Value);
                            }
                            if (keyValue.Key == "BUS")
                            {
                                bus = int.Parse(keyValue.Value);
                            }
                            if (keyValue.Key == "TEMP")
                            {
                                temp = float.Parse(keyValue.Value);
                            }
                        }
                    }

                    if (bus > 0) {
                        foreach (Gpu g in Rig.gpus)
                        {
                            // if this not working use BUS ID compare
                            if (g.CompareBusId(bus))
                            {
                                g.watt = watt;
                                g.temp = temp;
                            }
                        }
                    }
                }
            }


            IList<IList<KeyValuePair<string, string>>> threadList = CCMiner.CCMiner.ResultParser(threads);
            if (hwInfoGpuList.Count() > 0)
            {
                foreach (IList<KeyValuePair<string, string>> gpuData in threadList)
                {
                    uint watt = 0;
                    int bus = 0;
                    float khash = 0;

                    // skip os data we only want GPU data
                    if (gpuData.Count() > 8)
                    {
                        foreach (KeyValuePair<string, string> keyValue in gpuData)
                        {
                            if (keyValue.Key == "POWER")
                            {
                                watt = uint.Parse(keyValue.Value);
                            }
                            if (keyValue.Key == "BUS")
                            {
                                bus = int.Parse(keyValue.Value);
                            }
                            if (keyValue.Key == "KHS")
                            {
                                khash = float.Parse(keyValue.Value);
                            }
                        }
                    }

                    if (bus > 0)
                    {
                        foreach (Gpu g in Rig.gpus)
                        {
                            // if this not working use BUS ID compare
                            if (g.CompareBusId(bus))
                            {
                                g.hashRate = khash;
                                if (watt > 0)
                                {
                                    g.hashRateWatt = khash / watt;
                                }
                            }
                        }
                    }
                }
            }

            this.CollectingDone();
        }

        public void CCMinerError(CCMiner.CCMiner c)
        {
            del.CollectingError(this);
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

            TPLink.Power p = Rig.smartPlug.GetPower();
            r.AppendFormat("\"power\":{0},", p.watt);
            r.AppendFormat("\"kwh\":{0},", p.kwh);

            Program.WriteLine("Parse GPU DATA", false, true);
            List<string> gpujsonStrings = new List<string>();
            foreach (Gpu g in Rig.gpus)
            {
                gpujsonStrings.Add(g.GpuDataToJson());
            }
            r.AppendFormat("\"gpus\":[{0}]", string.Join(",", gpujsonStrings));

            r.Append("}");
            return r.ToString();
        }

        public void Collect()
        {
            if (this.UserKey().Length > 0)
            {
                collectorStatus = false;
                Rig.computer.UpdateHardware();
                // add external TEMP HDI SENSOR HERE

                Program.WriteLine("Hardware updated", false, true);
                ccminer.Collect();
            } else
            {
                Program.WriteLine("FATAL:UserKey not found please enter it in config.ini under [User] key=XXXXXXXX");
                Program.Abort();
            }
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
            ccminer.Clear();
            foreach (Gpu g in Rig.gpus)
            {
                g.Clear();
            }

            // unlock Rig
            collectorStatus = true;
        }
    }
}
