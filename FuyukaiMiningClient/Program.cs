// system
using System;
using System.Threading;

// Fuyukai
using FuyukaiMiningClient.Classes;

namespace FuyukaiMiningClient
{
    class Program
    {

        private static bool EnvDev = true;
        private static Config config = new Config();
        private const int INTERVAL = 10 * 60 * 1000; // 10min * 60 * 1000 = milliseconds 
        private static bool __running = true;

        static void Main(string[] args)
        {
            Program.PrintBootHeader();
            Telemetry telemetry = new Telemetry(Program.config);
            Program.WriteLine("WarmUp...", false);
            Thread.Sleep(2000);
            telemetry.Send();

            while (__running) {
                Thread.Sleep(Program.INTERVAL);
                telemetry.Send();
            }
            
            Console.WriteLine("...Exiting - press any key to close");
            Console.ReadKey();
        }

        public static void Abort()
        {
            __running = false;
        }

        public static void WriteLine(string msg, bool OmitDate = false, bool development = false)
        {
            if (development && !EnvDev)
            {
                return;
            } 

            if (!OmitDate)
                    msg = "[" + DateTime.Now + "]("+ Thread.CurrentThread.ManagedThreadId.ToString() + "|"+ System.Diagnostics.Process.GetCurrentProcess().Threads.Count + ") >>> " + msg;
                Console.WriteLine(msg);
           
        }

        private static void PrintBootHeader()
        {
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine("#########################################################################");
            Console.WriteLine("#");
            Console.WriteLine("#   Fuyukai Mining Client {0:S}", version);
            Console.WriteLine("#   author: Benjamin Rannow © 2018");
            Console.WriteLine("#   email: benjamin@fuyukai.moe");
            Console.WriteLine("#   https://fuyukai.moe");
            Console.WriteLine("#");
            Console.WriteLine("#   Send Telemetry Data to Mining Control Server");
            Console.WriteLine("#");
            Console.WriteLine("#   Supported Hardware:");
            Console.WriteLine("#        TP - Link Smart Wireless PowerSocket (Global PowerUsage)");
            Console.WriteLine("#        ccminer (HashRate)");
            Console.WriteLine("#        soon: HDI-Temp - Sensor (External USB Room Temp Sensor)");
            Console.WriteLine("#");
            Console.WriteLine("#########################################################################");
        }
    }
}
