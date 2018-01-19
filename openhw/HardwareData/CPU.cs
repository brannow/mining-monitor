using FuyukaiLib.HardwareData.CPUData;
using System;
using System.Collections.Generic;

namespace FuyukaiLib.HardwareData
{
    abstract class CPU
    {
        private static CPUID[][] GetProcessorThreads()
        {
            List<CPUID> threads = new List<CPUID>();
            for (int i = 0; i < 64; i++)
            {
                try
                {
                    threads.Add(new CPUID(i));
                }
                catch (ArgumentOutOfRangeException) { }
            }

            SortedDictionary<uint, List<CPUID>> processors =
              new SortedDictionary<uint, List<CPUID>>();
            foreach (CPUID thread in threads)
            {
                List<CPUID> list;
                processors.TryGetValue(thread.ProcessorId, out list);
                if (list == null)
                {
                    list = new List<CPUID>();
                    processors.Add(thread.ProcessorId, list);
                }
                list.Add(thread);
            }

            CPUID[][] processorThreads = new CPUID[processors.Count][];
            int index = 0;
            foreach (List<CPUID> list in processors.Values)
            {
                processorThreads[index] = list.ToArray();
                index++;
            }
            return processorThreads;
        }

        private static CPUID[][] GroupThreadsByCore(IEnumerable<CPUID> threads)
        {

            SortedDictionary<uint, List<CPUID>> cores =
              new SortedDictionary<uint, List<CPUID>>();
            foreach (CPUID thread in threads)
            {
                List<CPUID> coreList;
                cores.TryGetValue(thread.CoreId, out coreList);
                if (coreList == null)
                {
                    coreList = new List<CPUID>();
                    cores.Add(thread.CoreId, coreList);
                }
                coreList.Add(thread);
            }

            CPUID[][] coreThreads = new CPUID[cores.Count][];
            int index = 0;
            foreach (List<CPUID> list in cores.Values)
            {
                coreThreads[index] = list.ToArray();
                index++;
            }
            return coreThreads;
        }

        public static CPU DetectCPU()
        {
            CPUID[][] processorThreads = GetProcessorThreads();
            foreach (CPUID[] threads in processorThreads)
            {
                if (threads.Length == 0)
                    continue;

                CPUID[][] coreThreads = GroupThreadsByCore(threads);
                switch (threads[0].Vendor)
                {
                    case Vendor.Intel:
                        return new IntelCPU(coreThreads);
                    case Vendor.AMD:
                        switch (threads[0].Family)
                        {
                            case 0x0F:
                                // AMD0F CPU CURRENTLY NOT SUPPORTED
                                return new GenericCPU(coreThreads);
                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x14:
                            case 0x15:
                            case 0x16:
                                // AMD10 CPU CURRENTLY NOT SUPPORTED
                                return new GenericCPU(coreThreads);
                            default:
                                return new GenericCPU(coreThreads);
                        }
                    default:
                        return new GenericCPU(coreThreads);
                }
            }

            return null;
        }

        public abstract void Update();

        public abstract uint GetPackageTemp();

        public abstract uint GetLoad();
    }
}
