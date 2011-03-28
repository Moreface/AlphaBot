/*  Copyright (c) 2010 Daniel Kuwahara
 *    This file is part of AlphaBot.

    AlphaBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AlphaBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
 */
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
