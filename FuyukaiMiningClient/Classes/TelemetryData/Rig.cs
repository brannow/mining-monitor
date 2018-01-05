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

        // rig Data
        private string name = "";

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

        public void Collect()
        {
            collectorStatus = false;
            Rig.computer.UpdateHardware();
            Console.WriteLine("CPU USAGE: {0}", this.CpuUsage());
            Console.WriteLine("CPU Temp: {0}", this.CpuTemp());
            Console.WriteLine("RAM USAGE: {0}", this.RamUsage());
            Console.WriteLine("Uptime: {0}", this.Uptime());
            Console.WriteLine("identifier: {0}", this.Identifier());
            Console.WriteLine("Name: {0}", this.Name());
            Console.WriteLine("");
            Console.WriteLine("GPUS: {0}", Rig.gpus.Length);
            foreach (Gpu g in Rig.gpus) {
                g.Collect();
            }

            // add external TEMP HDI SENSOR HERE

            ccminer.Collect();
        }

        public void CCMinerDone(CCMiner.CCMiner c, string summary, string hwInfo, string threads)
        {
            Console.WriteLine(summary);
            Console.WriteLine(hwInfo);
            Console.WriteLine(threads);
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
            del.CollectingDone(this);
        }

        public bool IsCollectorIdle()
        {
            return collectorStatus;
        }

        public void Clear()
        {
            collectorStatus = true;
            ccminer.Clear();
        }
    }
}
