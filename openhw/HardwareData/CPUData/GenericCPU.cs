using System;
using System.Collections.Generic;
using System.Text;

namespace FuyukaiLib.HardwareData.CPUData
{
    internal class GenericCPU : CPU
    {
        protected readonly CPUID[][] cpuid;
        protected readonly Vendor vendor;

        protected readonly uint family;
        protected readonly uint model;
        protected readonly uint stepping;
        protected readonly int coreCount;

        private readonly CPULoad cpuLoad;

        protected float packageTemp;
        protected float[] coreTemps;

        protected bool supportCoreTemp = false;
        protected bool supportPackageTemp = false;

        public GenericCPU(CPUID[][] cpuid)
        {
            this.cpuid = cpuid;
            this.vendor = cpuid[0][0].Vendor;
            this.family = cpuid[0][0].Family;
            this.model = cpuid[0][0].Model;
            this.stepping = cpuid[0][0].Stepping;
            this.coreCount = cpuid.Length;
            this.coreTemps = new float[coreCount];
            this.packageTemp = 0;

            this.cpuLoad = new CPULoad(cpuid);
        }

        public override void Update()
        {
            if (this.cpuLoad != null)
            {
                this.cpuLoad.Update();
            }
        }

        public override uint GetLoad()
        {
            if (cpuLoad != null)
            {
                return (uint)cpuLoad.GetTotalLoad();
            }

            return 0;
        }

        public override uint GetPackageTemp()
        {
            float calcTemp = 0;

            if (packageTemp > 0)
            {
                calcTemp = packageTemp;
            }
            else
            {
                foreach (float t in coreTemps)
                {
                    calcTemp += t;
                }

                if (calcTemp > 0)
                {
                    calcTemp = calcTemp / coreTemps.Length;
                }
            }

            return (uint)calcTemp;
        }
    }
}
