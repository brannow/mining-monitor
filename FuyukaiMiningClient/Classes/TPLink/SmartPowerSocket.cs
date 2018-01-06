using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using FuyukaiMiningClient.Classes.Crypto;

using Newtonsoft.Json;
using System.Net.NetworkInformation;

/**
 * Windows 10 command line 
 * netsh wlan set hostednetwork mode=allow ssid=tp_test key=082a7e835bc7
 * netsh wlan start hostednetwork
 * 
 * find smart plug:
 * arp -a
 * 
 * setup config with ip
 **/

namespace FuyukaiMiningClient.Classes.TPLink
{
    public struct Power
    {
        public float watt;
        public float kwh;
    }

    class SmartPowerSocket
    {
        private static int port = 9999;

        private string address;

        public SmartPowerSocket(string address)
        {
            this.address = address;
        }

        public bool Check()
        {
            string answer = this.SendCommand("system", "get_sysinfo");
            dynamic answerObj = JsonConvert.DeserializeObject(answer);

            if (answerObj != null && answerObj.system != null && answerObj.system.get_sysinfo != null && answerObj.system.get_sysinfo.err_code != null) {
                return answerObj.system.get_sysinfo.err_code == 0;
            }

            return false;
        }

        public Power GetPower()
        {
            Program.WriteLine("Load PowerSocket Power Data", false, true);
            string answer = this.SendCommand("emeter", "get_realtime");
            dynamic answerObj = JsonConvert.DeserializeObject(answer);

            Power power = new Power
            {
                watt = 0,
                kwh = 0
            };

            if (answerObj != null && answerObj.emeter != null && answerObj.emeter.get_realtime != null && answerObj.emeter.get_realtime.err_code != null && answerObj.emeter.get_realtime.err_code == 0)
            {
                if (answerObj.emeter.get_realtime.power != null && answerObj.emeter.get_realtime.total != null)
                {
                    power.watt = (float)answerObj.emeter.get_realtime.power;
                    power.kwh = (float)answerObj.emeter.get_realtime.total;
                }
            }

            return power;
        }

        public bool ResetPower()
        {
            string answer = this.SendCommand("emeter", "erase_emeter_stat");
            dynamic answerObj = JsonConvert.DeserializeObject(answer);

            if (answerObj != null && answerObj.emeter != null && answerObj.emeter.get_realtime != null && answerObj.emeter.get_realtime.err_code != null)
            {
                return answerObj.system.get_sysinfo.err_code == 0;
            }

            return false;
        }


        private string SendCommand(string domain, string command, string payload = "")
        {
            Program.WriteLine("Try to Connect to Powersocket on " + address, false, true);
            if (address.Length == 0)
            {
                return "";
            }

            
            Ping p = new Ping();
            PingReply reply = p.Send(address);
            Program.WriteLine("Ping send", false, true);
            if (reply.Status == IPStatus.Success)
            {
                Program.WriteLine("Ping Success", false, true);
                byte[] bytes = new byte[4096];
                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                bool connected = false;
                Program.WriteLine("Try Establish Socket Connection", false, true);
                try
                {
                    sender.Connect(address, port);
                    connected = true;
                }
                catch (Exception e)
                {
                    Program.WriteLine("...failed", false, true);
                    connected = false;
                }

                if (connected)
                {
                    Program.WriteLine("...connected", false, true);
                    if (payload.Length < 2)
                        payload = "{}";

                    string msg = "{\"" + domain + "\":{\"" + command + "\":" + payload + "}}";
                    byte[] byteMsg = Xor.TPEncrypt(msg);
                    int bytesSent = sender.Send(byteMsg);
                    int bytesRec = sender.Receive(bytes);
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    Program.WriteLine("data Received", false, true);
                    if (bytesRec > 2)
                    {
                        Program.WriteLine("parse data", false, true);
                        return Xor.TPDecrypt(bytes, bytesRec);
                    }
                }
            }

            return "{}";
        }
    }
}
