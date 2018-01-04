using System.Management;
using System.IO;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

using FuyukaiMiningClient.Classes.Crypto;

namespace FuyukaiMiningClient.Classes
{
    class Hardware
    {
        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        private static PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        public static string HardDriveGUID()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            Collection<string> sn = new Collection<string>();
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                // get the hardware serial no.
                if (wmi_HD["SerialNumber"] != null)
                    return MD5Hash.Create(wmi_HD["SerialNumber"].ToString().Trim());
            }

            return "";
        }

        public static long UptimeInMinutes()
        {
            TimeSpan uptime = TimeSpan.FromMilliseconds(GetTickCount64());
            return uptime.Minutes + ((long)uptime.TotalHours * 60);
        }
    }  
}
