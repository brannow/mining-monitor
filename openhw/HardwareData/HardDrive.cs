using System;
using System.Collections.Generic;
using System.Management;

namespace FuyukaiLib.HardwareData
{
    class HardDrive
    {
        public static HardDrive[] DetectDrives()
        {
            ManagementObjectSearcher Finder = new ManagementObjectSearcher("Select * from Win32_OperatingSystem");
            string Name = "";
            string deviceId = "";
            foreach (ManagementObject OS in Finder.Get()) Name = OS["Name"].ToString();

            //Name = "Microsoft Windows XP Professional|C:\WINDOWS|\Device\Harddisk0\Partition1"

            int ind = Name.IndexOf("|") +1;
            if (ind > 1) {
                deviceId = Name.Substring(ind, 2);
            }

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
            List<HardDrive> drives = new List<HardDrive>();
            foreach (ManagementObject wmi_HD in searcher.Get())
            {
                // get the hardware with a serial number
                if (wmi_HD["VolumeSerialNumber"] != null)
                {
                    HardDrive hdd = null;
                    hdd = new HardDrive(wmi_HD, deviceId);
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
        private readonly bool isRootDevice = false;

        public HardDrive(ManagementObject data, string rootId = "")
        {
            deviceID = Convert.ToString(data.Properties["DeviceId"].Value);
            isRootDevice = (bool)(deviceID.Equals(rootId) && deviceID != "");
            serial = Convert.ToString(data.Properties["VolumeSerialNumber"].Value);
        }

        public bool IsRootDevice()
        {
            return isRootDevice;
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
