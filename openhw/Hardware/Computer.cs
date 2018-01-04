
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using System.Reflection;

namespace FuyukaiHWMonitor.Hardware {

  public class Computer : IComputer {

    private readonly List<IGroup> groups = new List<IGroup>();
    private readonly ISettings settings;

    private SMBIOS smbios;

    private bool open;

    private bool mainboardEnabled;
    private bool cpuEnabled;
    private bool ramEnabled;
    private bool gpuEnabled;
    private bool fanControllerEnabled;
    private bool hddEnabled;    

    public Computer() {
      this.settings = new Settings();
    }

    public Computer(ISettings settings) {
      this.settings = settings ?? new Settings();
    }

    private void Add(IGroup group) {
      if (groups.Contains(group))
        return;

      groups.Add(group);

      if (HardwareAdded != null)
        foreach (IHardware hardware in group.Hardware)
          HardwareAdded(hardware);
    }

    private void Remove(IGroup group) {
      if (!groups.Contains(group))
        return;

      groups.Remove(group);

      if (HardwareRemoved != null)
        foreach (IHardware hardware in group.Hardware)
          HardwareRemoved(hardware);

      group.Close();
    }

    private void RemoveType<T>() where T : IGroup {
      List<IGroup> list = new List<IGroup>();
      foreach (IGroup group in groups)
        if (group is T)
          list.Add(group);
      foreach (IGroup group in list)
        Remove(group);
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    public void Open() {
      if (open)
        return;
            
      this.smbios = new SMBIOS();

      Ring0.Open();
      Opcode.Open();

      if (mainboardEnabled)
        Add(new Mainboard.MainboardGroup(smbios, settings));
      
      if (cpuEnabled)
        Add(new CPU.CPUGroup(settings));

      if (ramEnabled)
        Add(new RAM.RAMGroup(smbios, settings));

      if (gpuEnabled) {
        Add(new ATI.ATIGroup(settings));
        Add(new Nvidia.NvidiaGroup(settings));
      }

      if (fanControllerEnabled) {
        Add(new TBalancer.TBalancerGroup(settings));
        Add(new Heatmaster.HeatmasterGroup(settings));
      }

      if (hddEnabled)
        Add(new HDD.HarddriveGroup(settings));

      open = true;
        }

    public bool MainboardEnabled {
      get { return mainboardEnabled; }

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      set {
        if (open && value != mainboardEnabled) {
          if (value)
            Add(new Mainboard.MainboardGroup(smbios, settings));
          else
            RemoveType<Mainboard.MainboardGroup>();
        }
        mainboardEnabled = value;
      }
    }

    public bool CPUEnabled {
      get { return cpuEnabled; }

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      set {
        if (open && value != cpuEnabled) {
          if (value)
            Add(new CPU.CPUGroup(settings));
          else
            RemoveType<CPU.CPUGroup>();
        }
        cpuEnabled = value;
      }
    }

    public bool RAMEnabled {
      get { return ramEnabled; }

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      set {
        if (open && value != ramEnabled) {
          if (value)
            Add(new RAM.RAMGroup(smbios, settings));
          else
            RemoveType<RAM.RAMGroup>();
        }
        ramEnabled = value;
      }
    }    

    public bool GPUEnabled {
      get { return gpuEnabled; }

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      set {
        if (open && value != gpuEnabled) {
          if (value) {
            Add(new ATI.ATIGroup(settings));
            Add(new Nvidia.NvidiaGroup(settings));
          } else {
            RemoveType<ATI.ATIGroup>();
            RemoveType<Nvidia.NvidiaGroup>();
          }
        }
        gpuEnabled = value;
      }
    }

    public bool FanControllerEnabled {
      get { return fanControllerEnabled; }

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      set {
        if (open && value != fanControllerEnabled) {
          if (value) {
            Add(new TBalancer.TBalancerGroup(settings));
            Add(new Heatmaster.HeatmasterGroup(settings));
          } else {
            RemoveType<TBalancer.TBalancerGroup>();
            RemoveType<Heatmaster.HeatmasterGroup>();
          }
        }
        fanControllerEnabled = value;
      }
    }

    public bool HDDEnabled {
      get { return hddEnabled; }

      [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
      set {
        if (open && value != hddEnabled) {
          if (value)
            Add(new HDD.HarddriveGroup(settings));
          else
            RemoveType<HDD.HarddriveGroup>();
        }
        hddEnabled = value;
      }
    }

    public IHardware[] Hardware {
      get {
        List<IHardware> list = new List<IHardware>();
        foreach (IGroup group in groups)
          foreach (IHardware hardware in group.Hardware)
            list.Add(hardware);
        return list.ToArray();
      }
    }

    private static void NewSection(TextWriter writer) {
      for (int i = 0; i < 8; i++)
        writer.Write("----------");
      writer.WriteLine();
      writer.WriteLine();
    }

    private static int CompareSensor(ISensor a, ISensor b) {
      int c = a.SensorType.CompareTo(b.SensorType);
      if (c == 0)
        return a.Index.CompareTo(b.Index);
      else
        return c;
    }

    private static void ReportHardwareSensorTree(
      IHardware hardware, TextWriter w, string space) 
    {
      w.WriteLine("{0}|", space);
      w.WriteLine("{0}+- {1} ({2})",
        space, hardware.Name, hardware.Identifier);
      ISensor[] sensors = hardware.Sensors;
      Array.Sort(sensors, CompareSensor);
      foreach (ISensor sensor in sensors) {
        w.WriteLine("{0}|  +- {1,-14} : {2,8:G6} {3,8:G6} {4,8:G6} ({5})", 
          space, sensor.Name, sensor.Value, sensor.Min, sensor.Max, 
          sensor.Identifier);
      }
      foreach (IHardware subHardware in hardware.SubHardware)
        ReportHardwareSensorTree(subHardware, w, "|  ");
    }

    private static void ReportHardwareParameterTree(
      IHardware hardware, TextWriter w, string space) {
      w.WriteLine("{0}|", space);
      w.WriteLine("{0}+- {1} ({2})",
        space, hardware.Name, hardware.Identifier);
      ISensor[] sensors = hardware.Sensors;
      Array.Sort(sensors, CompareSensor);
      foreach (ISensor sensor in sensors) {
        string innerSpace = space + "|  ";
        if (sensor.Parameters.Length > 0) {
          w.WriteLine("{0}|", innerSpace);
          w.WriteLine("{0}+- {1} ({2})",
            innerSpace, sensor.Name, sensor.Identifier);
          foreach (IParameter parameter in sensor.Parameters) {
            string innerInnerSpace = innerSpace + "|  ";
            w.WriteLine("{0}+- {1} : {2}",
              innerInnerSpace, parameter.Name,
              string.Format(CultureInfo.InvariantCulture, "{0} : {1}",
                parameter.DefaultValue, parameter.Value));
          }
        }
      }
      foreach (IHardware subHardware in hardware.SubHardware)
        ReportHardwareParameterTree(subHardware, w, "|  ");
    }

    private static void ReportHardware(IHardware hardware, TextWriter w) {
      string hardwareReport = hardware.GetReport();
      if (!string.IsNullOrEmpty(hardwareReport)) {
        NewSection(w);
        w.Write(hardwareReport);
      }
      foreach (IHardware subHardware in hardware.SubHardware)
        ReportHardware(subHardware, w);
    }

        public IHardware[] GetRam()
        {
            List<IHardware> list = new List<IHardware>();
            foreach (IGroup group in groups)
            {
                if (group is RAM.RAMGroup)
                {
                    foreach (IHardware hardware in group.Hardware)
                    {
                        if (hardware is RAM.GenericRAM)
                        {
                            list.Add(hardware);
                        }
                    }
                }
            }

            return list.ToArray();
        }

        public IHardware[] GetGPU()
        {
            List<IHardware> list = new List<IHardware>();
            foreach (IGroup group in groups) {
                if (group is Nvidia.NvidiaGroup || group is ATI.ATIGroup) {
                    foreach (IHardware hardware in group.Hardware) {
                        if (hardware is Nvidia.NvidiaGPU || hardware is ATI.ATIGPU)  {
                            list.Add(hardware);
                        }
                    }
                }
            }
                
            return list.ToArray();
        }

        public IHardware[] GetCPU()
        {
            List<IHardware> list = new List<IHardware>();
            foreach (IGroup group in groups) {
                if (group is CPU.CPUGroup) {
                    foreach (IHardware hardware in group.Hardware) {
                        if (hardware is CPU.GenericCPU) {
                            list.Add(hardware);
                        }
                    }
                }
            }
                
            return list.ToArray();
        }

        public float RamUsage()
        {
            IHardware[] rams = this.GetRam();
            if (rams.Length > 0 && rams[0] is RAM.GenericRAM) {
                RAM.GenericRAM ram = (RAM.GenericRAM)rams[0];
                foreach (ISensor sensor in ram.Sensors) {
                    if (sensor.Name == "Memory") {
                        return (float)sensor.Value;
                    }
                }
            }

            return 0;
        }

        public float CPUUsage()
        {
            IHardware[] cpus = this.GetCPU();
            if (cpus.Length > 0 && cpus[0] is CPU.GenericCPU)
            {
                CPU.GenericCPU cpu = (CPU.GenericCPU)cpus[0];
                float clocks = 0;
                int cores = 0;
                foreach (ISensor sensor in cpu.Sensors)
                {
                    if (sensor.SensorType == SensorType.Load)
                    {
                        ++cores;
                        clocks += (float)sensor.Value;
                    }
                }

                if (cores > 0) {
                    return clocks / cores;
                }
            }

            return 0;
        }

        public float CPUTemp()
        {
            float maxTemp = 0;
            IHardware[] cpus = this.GetCPU();
            if (cpus.Length > 0 && cpus[0] is CPU.GenericCPU)
            {
                CPU.GenericCPU cpu = (CPU.GenericCPU)cpus[0];
                
                foreach (ISensor sensor in cpu.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Name != "CPU Package")
                    {
                        if (maxTemp < (float)sensor.Value) 
                            maxTemp = (float)sensor.Value;
                    }
                }
            }

            return maxTemp;
        }

        public void UpdateHardware()
        {
            if (!this.open) {
                return;
            }

            foreach (IGroup group in groups)
            {
                foreach (IHardware hardware in group.Hardware)
                {
                    hardware.Update();
                }
            }
        }

        public string GetReport() {

      using (StringWriter w = new StringWriter(CultureInfo.InvariantCulture)) {

        w.WriteLine();
        w.WriteLine("Open Hardware Monitor Report");
        w.WriteLine();

        Version version = typeof(Computer).Assembly.GetName().Version;

        NewSection(w);
        w.Write("Version: "); w.WriteLine(version.ToString());
        w.WriteLine();

        NewSection(w);
        w.Write("Common Language Runtime: ");
        w.WriteLine(Environment.Version.ToString());
        w.Write("Operating System: ");
        w.WriteLine(Environment.OSVersion.ToString());
        w.Write("Process Type: ");
        w.WriteLine(IntPtr.Size == 4 ? "32-Bit" : "64-Bit");
        w.WriteLine();

        string r = Ring0.GetReport();
        if (r != null) {
          NewSection(w);
          w.Write(r);
          w.WriteLine();
        }

        NewSection(w);
        w.WriteLine("Sensors");
        w.WriteLine();
        foreach (IGroup group in groups) {
          foreach (IHardware hardware in group.Hardware)
            ReportHardwareSensorTree(hardware, w, "");
        }
        w.WriteLine();

        NewSection(w);
        w.WriteLine("Parameters");
        w.WriteLine();
        foreach (IGroup group in groups) {
          foreach (IHardware hardware in group.Hardware)
            ReportHardwareParameterTree(hardware, w, "");
        }
        w.WriteLine();

        foreach (IGroup group in groups) {
          string report = group.GetReport();
          if (!string.IsNullOrEmpty(report)) {
            NewSection(w);
            w.Write(report);
          }

          IHardware[] hardwareArray = group.Hardware;
          foreach (IHardware hardware in hardwareArray)
            ReportHardware(hardware, w);

        }
        return w.ToString();
      }
    }

    public void Close() {      
      if (!open)
        return;

      while (groups.Count > 0) {
        IGroup group = groups[groups.Count - 1];
        Remove(group);         
      } 

      Opcode.Close();
      Ring0.Close();

      this.smbios = null;

      open = false;
    }

    public event HardwareEventHandler HardwareAdded;
    public event HardwareEventHandler HardwareRemoved;

    public void Accept(IVisitor visitor) {
      if (visitor == null)
        throw new ArgumentNullException("visitor");
      visitor.VisitComputer(this);
    }

    public void Traverse(IVisitor visitor) {
      foreach (IGroup group in groups)
        foreach (IHardware hardware in group.Hardware) 
          hardware.Accept(visitor);
    }

    private class Settings : ISettings {

      public bool Contains(string name) {
        return false;
      }

      public void SetValue(string name, string value) { }

      public string GetValue(string name, string value) {
        return value;
      }

      public void Remove(string name) { }
    }
  }
}
