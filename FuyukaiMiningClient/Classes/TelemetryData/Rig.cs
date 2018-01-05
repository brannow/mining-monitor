﻿using System;
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

        // rig Data
        private string name = "";
        // TEMP RIG DATA
        private float rigHashRate = 0;
        private long minerUptime = 0;

        public Rig(Config cfg, Telemetry del)
        {
            this.del = del;
            Rig.config = cfg;
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
                    string sn = "";
                    uint watt = 0;
                    int bus = 0;
                    int ccminerId = 0;
                    float temp = 0;

                    // skip os data we only want GPU data
                    if (gpuData.Count() > 10)
                    {
                        foreach (KeyValuePair<string, string> keyValue in gpuData)
                        {
                            if (keyValue.Key == "SN" && keyValue.Value.Length > 2)
                            {
                                sn = (string)keyValue.Value.Substring(2);
                            }
                            if (keyValue.Key == "GPU")
                            {
                                ccminerId = int.Parse(keyValue.Value);
                            }
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

                    if (sn.Length > 0) {
                        foreach (Gpu g in Rig.gpus)
                        {
                            // if this not working use BUS ID compare
                            if (g.CompareSerial(sn))
                            {
                                g.ccminerId = ccminerId;
                                g.watt = watt;
                                g.bus = bus;
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
                    int bus = 0;
                    float khash = 0;
                    float khashWatt = 0;

                    // skip os data we only want GPU data
                    if (gpuData.Count() > 8)
                    {
                        foreach (KeyValuePair<string, string> keyValue in gpuData)
                        {
                            if (keyValue.Key == "KHW")
                            {
                                khashWatt = float.Parse(keyValue.Value);
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
                            if (g.bus == bus)
                            {
                                g.hashRate = khash;
                                g.hashRateWatt = khashWatt;
                            }
                        }
                    }
                }
            }

            this.RigPowerUsageCollect();
        }

        public void CCMinerError(CCMiner.CCMiner c)
        {
            del.CollectingError(this);
        }

        public void RigPowerUsageCollect()
        {
            // ADD SMART WATT USAGE DETECTION HEHE
            Console.WriteLine("Collect RIG WATT CONSUME");
            this.RigPowerUsageDone();
        }

        public void RigPowerUsageDone()
        {
            Console.WriteLine("DONE RIG WATT");
            this.CollectingDone();
        }

        private string RigDataToJsonString()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            StringBuilder r = new StringBuilder("{");

            r.AppendFormat("\"identifier\":\"{0}\",", this.Identifier());
            r.AppendFormat("\"name\":\"{0}\",", this.Name());
            r.AppendFormat("\"os-uptime\":{0},", this.Uptime());
            r.AppendFormat("\"cpu-usage\":{0},", this.CpuUsage());
            r.AppendFormat("\"cpu-temp\":{0},", this.CpuTemp());
            r.AppendFormat("\"ram-usage\":{0},", this.RamUsage());
            r.AppendFormat("\"ccminer-uptime\":{0},", this.minerUptime);
            r.AppendFormat("\"ccminer-khash-rate\":{0},", this.rigHashRate);

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
            collectorStatus = false;
            Rig.computer.UpdateHardware();
            // add external TEMP HDI SENSOR HERE

            ccminer.Collect();
        }

        private void CollectingDone()
        {
            del.CollectingDone(this, this.RigDataToJsonString());
        }

        public void Clear()
        {
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
