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
        }

        private string GetReference()
        {
            return MD5Hash.Create(string.Concat(GetName(), GetBusIndex().ToString()));
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
