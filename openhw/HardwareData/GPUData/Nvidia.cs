using System;
using System.Collections.Generic;
using System.Text;
using FuyukaiLib.Lib.NVML;

namespace FuyukaiLib.HardwareData.GPUData
{
    class Nvidia : GPU
    {
        private nvmlDevice device;
        private NvPhysicalGpuHandle NVAPIhandle;

        public Nvidia(uint i, nvmlDevice device)
        {
            this.deviceIndex = i;
            this.device = device;
            this.type = GPUType.NVIDIA;

            nvmlPciInfo pciInfo = new nvmlPciInfo();
            if (NvmlNativeMethods.nvmlDeviceGetPciInfo(device, ref pciInfo) == nvmlReturn.Success)
            {
                this.bus = pciInfo.bus;
            }

            if (NvmlNativeMethods.nvmlDeviceGetSerial(device, out string serialOut) == nvmlReturn.Success)
            {
                this.serial = serialOut;
            }

            if (NvmlNativeMethods.nvmlDeviceGetUUID(device, out string uuidOut) == nvmlReturn.Success)
            {
                this.uuid = uuidOut;
            }

            if (NvmlNativeMethods.nvmlDeviceGetName(device, out string name) == nvmlReturn.Success)
            {
                this.name = name;
            }

            NVAPIhandle = this.FindPhysicalGpuHandleForBus(this.bus);

            Update();
        }

        private NvPhysicalGpuHandle FindPhysicalGpuHandleForBus(uint bus)
        {
            NvPhysicalGpuHandle[] handlesFromDisplay =
                      new NvPhysicalGpuHandle[NVAPI.MAX_PHYSICAL_GPUS];
            if (NVAPI.NvAPI_EnumPhysicalGPUs(handlesFromDisplay, out int count) == NvStatus.OK) {
                foreach (NvPhysicalGpuHandle physHandle in handlesFromDisplay)
                {
                    if (NVAPI.NvAPI_GPU_GetBusID(physHandle, out int physBus) == NvStatus.OK && physBus == bus) {
                        return physHandle;
                    }
                }
            }

            return new NvPhysicalGpuHandle();
        }

        public override void Update()
        {
            this.coreTemp = 0;
            this.fanSpeed = 0;
            this.power = 0;
            this.ramUsage = 0;
            this.ramTotal = 0;
            this.coreUsage = 0;

            if (NvmlNativeMethods.nvmlDeviceGetUUID(device, out string uuidOut) == nvmlReturn.Success)
            {
                if (this.uuid == uuidOut)
                {
                    this.failure = false;
                } else
                {
                    this.failure = true;
                }
            }
            else {
                this.failure = true;
            }

            if (!this.failure)
            {
                uint coreTemp = 0;
                if (NvmlNativeMethods.nvmlDeviceGetTemperature(device, nvmlTemperatureSensors.Gpu, ref coreTemp) == nvmlReturn.Success)
                {
                    this.coreTemp = coreTemp;
                }

                if (NVAPI.NvAPI_GPU_GetTachReading(this.NVAPIhandle, out int value) == NvStatus.OK)
                {
                    this.fanSpeed = (uint)value;
                }

                uint powerUsage = 0;
                if (NvmlNativeMethods.nvmlDeviceGetPowerUsage(device, ref powerUsage) == nvmlReturn.Success)
                {
                    this.power = powerUsage / 1000;
                }

                nvmlMemory memInfo = new nvmlMemory();
                if (NvmlNativeMethods.nvmlDeviceGetMemoryInfo(device, ref memInfo) == nvmlReturn.Success)
                {
                    this.ramTotal = (memInfo.total / 1024) / 1024;
                }

                nvmlUtilization coreInfo = new nvmlUtilization();
                if (NvmlNativeMethods.nvmlDeviceGetUtilizationRates(device, ref coreInfo) == nvmlReturn.Success)
                {
                    this.ramUsage = coreInfo.memory;
                    this.coreUsage = coreInfo.gpu;
                }
            }
        }
    }
}
