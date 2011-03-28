using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

namespace CSharpClient
{
    class Utils
    {
        private static ulong readNumber(ref String input, int offset, uint size)
        {
            ulong output = 0;
            for (int i = 0; i < size; i++, offset++)
            {
                uint temp = (byte)input[offset];
                int shift = i * 8;
                output |= temp << shift;
            }
            return output;
        }

        public static string readNullTerminatedString(string packet, ref int offset)
        {
            int zero = packet.IndexOf('\0',offset);
            string output;
            if (zero == -1)
            {
                zero = packet.Length;
                output = packet.Substring(offset, zero - offset);
                offset = 0;
            }
            else
            {
                output = packet.Substring(offset, zero - offset);
                offset = zero + 1;
            }
            return output;
        }


    }
}
