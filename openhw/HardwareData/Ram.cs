using System.Runtime.InteropServices;

namespace FuyukaiLib.HardwareData
{
    class Ram
    {
        private float load;
        private float used;
        private float total;

        public static Ram DetectRam()
        {
            return new Ram();
        }

        public Ram()
        {
            this.Update();
        }

        public void Update()
        { 
            NativeMethods.MemoryStatusEx status = new NativeMethods.MemoryStatusEx
            {
                Length = checked((uint)Marshal.SizeOf(
                typeof(NativeMethods.MemoryStatusEx)))
            };

            if (!NativeMethods.GlobalMemoryStatusEx(ref status))
                return;

            load = 100.0f -
              (100.0f * status.AvailablePhysicalMemory) /
              status.TotalPhysicalMemory;

            used = (float)(status.TotalPhysicalMemory
              - status.AvailablePhysicalMemory) / (1024 * 1024 * 1024);

            total = (float)status.TotalPhysicalMemory /
              (1024 * 1024 * 1024);
        }

        private class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct MemoryStatusEx
            {
                public uint Length;
                public uint MemoryLoad;
                public ulong TotalPhysicalMemory;
                public ulong AvailablePhysicalMemory;
                public ulong TotalPageFile;
                public ulong AvailPageFile;
                public ulong TotalVirtual;
                public ulong AvailVirtual;
                public ulong AvailExtendedVirtual;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GlobalMemoryStatusEx(
              ref NativeMethods.MemoryStatusEx buffer);
        }

        public uint GetUsage()
        {
            return (uint)load;
        }
    }
}
