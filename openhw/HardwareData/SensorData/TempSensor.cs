using HidLibrary;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace FuyukaiLib.HardwareData.SensorData
{
    class TempSensor
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct SensorDataResult {
            [FieldOffset(1)]
            public byte SensorCount;
            [FieldOffset(2)]
            public byte currentSensor;
            [FieldOffset(3)]
            public byte powerSupply;

            [FieldOffset(5)]
            public short temp;

            [FieldOffset(8)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U8)]
            public byte[] sensorId;
        }

        private const int VendorId = 0x16C0;
        private const int ProductId = 0x0480;

        private readonly HidDevice sensor;
        private SensorDataResult sensorResult;
        private bool failure;

        public static TempSensor DetectSensor()
        {
            HidDevice _device = HidDevices.Enumerate(VendorId, ProductId).FirstOrDefault();
            if (_device != null)
            {
                return new TempSensor(_device);
            }

            return null;
        }

        public TempSensor(HidDevice sensor)
        {
            this.sensor = sensor;
            this.sensorResult = new SensorDataResult();
            Update();
        }

        // in °C
        public float GetTemp()
        {
            return (float)sensorResult.temp / 10;
        }

        public bool IsFailure()
        {
            return failure;
        }

        public void Update()
        {
            HidDeviceData data = sensor.Read(2);
            if (data.Status == HidDeviceData.ReadStatus.Success)
            {
                int size = Marshal.SizeOf(this.sensorResult);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(data.Data, 0, ptr, size);
                this.sensorResult = (SensorDataResult)Marshal.PtrToStructure(ptr, sensorResult.GetType());
                Marshal.FreeHGlobal(ptr);
                failure = false;
            }
            else
            {
                failure = true;
                sensorResult.temp = 0;
            }
        }
    }
}
