using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuyukaiMiningClient.Classes.Crypto
{
    class Xor
    {
        private static uint TPKEY = 171;

        public static byte[] TPEncrypt(string text)
        {
			uint key = TPKEY;

            byte[] bytes = new byte[text.Length + 4];
            bytes[0] = 0;
            bytes[1] = 0;
            bytes[2] = 0;
            bytes[3] = 0;

            uint c = 0;
            foreach (char i in text)
            {
                uint a = key ^ (uint)i;
                key = a;
                bytes[c+4] = (byte)a;
                ++c;
            }

            return bytes;
        }

        public static string TPDecrypt(byte[] bytes, int len)
        {
            uint key = TPKEY;
            StringBuilder sb = new StringBuilder(len - 4);

            for (uint c = 4; c < len; ++c)
            {
				uint i = (uint)bytes[c];
                uint a = key ^ i;
                key = (uint)i;
                sb.Append((char)a);
            }

            return sb.ToString();
        }
    }
}
