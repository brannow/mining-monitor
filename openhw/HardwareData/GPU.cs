using FuyukaiLib.Lib.NVML;
using FuyukaiLib.HardwareData.GPUData;
using System;

namespace FuyukaiLib.HardwareData
{
    enum GPUType : uint
    {
        Generic = 0,
        NVIDIA = 1,
        ATI = 2
    };

    abstract class GPU
    {
        protected uint deviceIndex;
        protected uint bus;
        protected string uuid;
        protected string serial;
        protected string name;

        protected ulong coreUsage;
        protected uint coreTemp;
        protected uint ramUsage;
        protected ulong ramTotal;
        protected uint fanSpeed;
        protected uint power;
        
        private float hashRate;

        protected bool failure = false;

        protected GPUType type;

        private static Nvidia[] DetectNvidiaGPUS()
        {
            if (NvmlNativeMethods.nvmlInit() == nvmlReturn.Success && NVAPI.IsAvailable) {
                uint deviceCount = 0;
                if (NvmlNativeMethods.nvmlDeviceGetCount(ref deviceCount) == nvmlReturn.Success) {

                    Nvidia[] nvgpu = new Nvidia[deviceCount];
                    for (uint i = 0; i < deviceCount; ++i) {
                        nvmlDevice device = new nvmlDevice();
                        if (NvmlNativeMethods.nvmlDeviceGetHandleByIndex((uint)i, ref device) == nvmlReturn.Success) {
                            nvgpu[i] = new Nvidia(i, device);
                        }
                    }

                    return nvgpu;
                }
            }

            return new Nvidia[0];
        }

        private static Ati[] DetectAtiGPUS()
        {
            // currently not supported
            return new Ati[0];
        }

        public static GPU[] DetectGPUS()
        {
            GPU[] nvidiaGPUs = DetectNvidiaGPUS();
            GPU[] atiGPUs = DetectAtiGPUS();

            GPU[] allGPUS = new GPU[nvidiaGPUs.Length + atiGPUs.Length];
            Array.Copy(nvidiaGPUs, allGPUS, nvidiaGPUs.Length);
            Array.Copy(atiGPUs, 0, allGPUS, nvidiaGPUs.Length, atiGPUs.Length);

            return allGPUS;
        }

        public abstract void Update();


        public void SetHashRate(float hashRate)
        {
            this.hashRate = hashRate;
        }

        public float GetHashRate()
        {
            if (coreUsage > 15) {
                return hashRate;
            }

            return 0;
        }

        public uint GetBus()
        {
            return bus;
        }

        public uint GetPower()
        {
            return power;
        }

        public string JsonString()
        {
            uint active = 0;
            if (!failure)
                active = 1;

            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            System.Text.StringBuilder r = new System.Text.StringBuilder("{");
            r.AppendFormat("\"serial\":\"{0}\",", serial);
            r.AppendFormat("\"bus\":{0},", bus);
            r.AppendFormat("\"name\":\"{0}\",", name);
            r.AppendFormat("\"reference\":\"{0}\",", uuid);
            r.AppendFormat("\"core-temp\":{0},", coreTemp.ToString("0.#########"));
            r.AppendFormat("\"ram-usage\":{0},", ramUsage.ToString("0.#########"));
            r.AppendFormat("\"ram-total\":{0},", ramTotal.ToString("0.#########"));
            r.AppendFormat("\"core-usage\":{0},",coreUsage.ToString("0.#########"));
            r.AppendFormat("\"fan\":{0},", fanSpeed.ToString("0.#########"));
            r.AppendFormat("\"type\":{0},", (uint)type);
            r.AppendFormat("\"khash-rate\":{0},", GetHashRate().ToString("0.#########"));
            r.AppendFormat("\"hash-rate-watt\":{0},", (GetHashRate() / power).ToString("0.#########"));
            r.AppendFormat("\"power\":{0},", power.ToString("0.#########"));
            r.AppendFormat("\"active\":{0}", active);
            r.Append("}");
            return r.ToString();
        }
    }
}
