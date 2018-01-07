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
        public uint watt = 0;
        public float temp = 0;
        public int ccminerId = 0;

        public float hashRate = 0;
        public float hashRateWatt = 0;

        public Gpu(IHardware gpu)
        {
            this.gpu = gpu;
        }

        public string GpuDataToJson()
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            StringBuilder r = new StringBuilder("{");

            r.AppendFormat("\"serial\":\"{0}\",", this.GetSerial());
            r.AppendFormat("\"bus\":{0},", this.GetBusIndex());
            r.AppendFormat("\"name\":\"{0}\",", this.GetName());
            r.AppendFormat("\"reference\":\"{0}\",", this.GetReference());
            r.AppendFormat("\"temp\":{0},", this.GetTemp().ToString("0.#########"));
            r.AppendFormat("\"mem-used\":{0},", this.GetMemUsed().ToString("0.#########"));
            r.AppendFormat("\"mem-total\":{0},", this.GetMemTotal().ToString("0.#########"));
            r.AppendFormat("\"core-used\":{0},", this.GetCoreUsed().ToString("0.#########"));
            r.AppendFormat("\"fan\":{0},", this.GetFanSpeed().ToString("0.#########"));

            r.AppendFormat("\"khash-rate\":{0},", hashRate.ToString("0.#########"));
            r.AppendFormat("\"hash-rate-watt\":{0},", hashRateWatt.ToString("0.#########"));

            r.AppendFormat("\"watt\":{0},", watt.ToString("0.#########"));
            r.AppendFormat("\"ccminer-temp\":{0}", temp.ToString("0.#########"));

            r.Append("}");
            return r.ToString();
        }

        public void Clear()
        {
            ccminerId = 0;
            watt = 0;
            temp = 0;
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

        private string GetSerial()
        {

            if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
            {
                return gn.GetSerial();
            }
            else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
            {
                return ga.GetSerial();
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
                return gn.GetPCIBusId();
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

        public bool CompareBusId(int remoteBusId)
        {
            if (remoteBusId > 0) {
                if (gpu is FuyukaiHWMonitor.Hardware.Nvidia.NvidiaGPU gn)
                {
                    return (remoteBusId == gn.GetPCIBusId());
                }
                else if (gpu is FuyukaiHWMonitor.Hardware.ATI.ATIGPU ga)
                {
                    return (remoteBusId == ga.GetPCIBusId());
                }
            }

            return false;
        }
    }
}
