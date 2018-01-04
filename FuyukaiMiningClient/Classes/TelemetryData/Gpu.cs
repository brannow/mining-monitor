using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FuyukaiHWMonitor.Hardware;
using FuyukaiMiningClient.Classes.Crypto;

namespace FuyukaiMiningClient.Classes.TelemetryData
{
    class Gpu
    {
        private IHardware gpu;

        public Gpu(IHardware gpu)
        {
            this.gpu = gpu;
        }

        public void Collect()
        {
            Console.WriteLine("GPU: {0}", GetBusIndex());
            Console.WriteLine("GPU Name: {0}", GetName());
            Console.WriteLine("GPU REF: {0}", GetReference());
            Console.WriteLine("GPU Temp: {0}", GetTemp());
            Console.WriteLine("GPU memUsed: {0}", GetMemUsed());
            Console.WriteLine("GPU memTotal: {0}", GetMemTotal());
            Console.WriteLine("GPU Core used: {0}", GetCoreUsed());
            Console.WriteLine("GPU Fan Speed: {0}", GetFanSpeed());
        }

        private string GetReference()
        {

            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetReference();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetReference();
            }

            return "";
        }

        private float GetTemp()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetCoreTemp();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetCoreTemp();
            }

            return 0;
        }

        private float GetMemUsed()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetMemUsed();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetMemUsed();
            }

            return 0;
        }

        private float GetMemTotal()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetMemTotal();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetMemTotal();
            }

            return 0;
        }

        private int GetBusIndex()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn) {
                return gn.CurrentAdapterIndex();
            }else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.CurrentAdapterIndex();
            }

            return 0;
        }

        private float GetCoreUsed()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetCoreUsed();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetCoreUsed();
            }

            return 0;
        }

        private float GetFanSpeed()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetFanSpeed();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetFanSpeed();
            }

            return 0;
        }

        private string GetName()
        {
            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetName();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetName();
            }

            return "";
        }
    }
}
