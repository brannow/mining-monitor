using System;
using System.Collections.Generic;
using System.Management;

namespace FuyukaiLib.HardwareData
{
    class HardDrive
    {
        public static HardDrive[] DetectDrives()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
            List<HardDrive> drives = new List<HardDrive>();
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                // get the hardware with a serial number
                if (wmi_HD["VolumeSerialNumber"] != null)
                {
                    HardDrive hdd = null;
                    hdd = new HardDrive(wmi_HD);
                    if (hdd != null)
                    {
                        drives.Add(hdd);
                    }                
                }
            }

            return drives.ToArray();
        }

        private readonly string deviceID;
        private readonly string serial;

        public HardDrive(ManagementObject data)
        {
            deviceID = Convert.ToString(data.Properties["DeviceId"].Value);
            serial = Convert.ToString(data.Properties["VolumeSerialNumber"].Value);
        }

        public void Update()
        {
            // update Hardware here
        }

        public string GetSerial()
        {
            return serial;
        }
    }
}
