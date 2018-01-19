using System.Text;
using System.Diagnostics;

using FuyukaiLib.HardwareData;

namespace FuyukaiLib
{
    public class Hardware
    {
        private GPU[] gpus;
        private HardDrive[] drives;
        private CPU cpu;
        private Ram ram;

        public Hardware()
        {
            Lib.Generic.Opcode.Open();
            Lib.Generic.Ring0.Open();
            this.drives = HardDrive.DetectDrives();
            this.cpu = CPU.DetectCPU();
            this.ram = Ram.DetectRam();
            this.gpus = GPU.DetectGPUS();

            this.Update();
        }

        public bool SetHashRateForGPUAtBus(uint bus, float hashRate)
        {
            foreach (GPU g in this.gpus)
            {
                if (g.GetBus() == bus)
                {
                    g.SetHashRate(hashRate);
                    return true;
                }
            }

            return false;
        }

        public void Update()
        {
            // update GPU data
            if (this.gpus != null) {
                foreach (GPU gpu in this.gpus) {
                    gpu.Update();
                }
            }

            // update hardDrive data
            if (this.drives != null)
            {
                foreach (HardDrive drive in this.drives)
                {
                    drive.Update();
                }
            }

            // update cpu data
            if (this.cpu != null) {
                this.cpu.Update();
            }

            // update ram data
            if (this.ram != null)
            {
                this.ram.Update();
            }
        }

        public string GetHardwareIdentifier()
        {
            StringBuilder b = new StringBuilder();
            foreach (HardDrive hdd in this.drives) {
                b.Append(hdd.GetSerial());
            }

            return Crypto.MD5Hash.Create(b.ToString());
        }

        public uint GetCPUUsage()
        {
            return this.cpu.GetLoad();
        }

        public uint GetCPUTemp()
        {
            return this.cpu.GetPackageTemp();
        }

        public uint GetRamUsage()
        {
            return this.ram.GetUsage();
        }

        public float GetHashRate()
        {
            float hashRate = 0;
            foreach (GPU g in this.gpus)
            {
                hashRate += g.GetHashRate();
            }

            return hashRate;
        }

        public uint GetPower()
        {
            uint power = 0;
            foreach (GPU g in this.gpus)
            {
                power += g.GetPower();
            }

            return power + 60;
        }

        public string GetGPUJson()
        {
            string[] jsonChunks = new string[gpus.Length];
            uint i = 0;
            foreach (GPU g in gpus)
            {
                jsonChunks[i] = g.JsonString();
                ++i;
            }

            return "[" + string.Join(",", jsonChunks) + "]";
        }

        public uint GetUpTime()
        {
            PerformanceCounter pc = new PerformanceCounter("System", "System Up Time");
            pc.NextValue();
            uint uptime = (uint)pc.NextValue();

            return uptime;
        }
    }
}
