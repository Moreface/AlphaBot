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
    along with AlphaBot.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CSharpClient
{
    class CheckRevision
    {
        public enum CheckRevisionResult
        {
            CHECK_REVISION_SUCCESS,
            CHECK_REVISION_MPQ_ERROR,
            CHECK_REVISION_FORMULA_ERROR,
            CHECK_REVISION_FILE_ERROR
        };

        static uint[] mpqHashCodes =
	    {
		    0xE7F4CB62,
		    0xF6A14FFC,
		    0xAA5504AF,
		    0x871FCDC2,
		    0x11BF6A18,
		    0xC57292E6,
		    0x7927D27E,
		    0x2FEC8733
	    };

        static String[] d2Files =
	    {
		    "Game.exe",
		    "Bnclient.dll",
		    "D2Client.dll"
	    };

        public delegate ulong OperatorType(ulong x, ulong y);

        static ulong operator_add(ulong left, ulong right)
        {
            return left + right;
        }

        static ulong operator_sub(ulong left, ulong right)
        {
            return left - right;
        }

        static ulong operator_xor(ulong left, ulong right)
        {
            return left ^ right;
        }

        protected static bool GetVariableIndex(char input,ref uint offset)
        {
            if (input >= 'A' && input <= 'C')
            {
                offset = (uint)(input - 'A');
                return true;
            }
            return false;
        }

        protected static bool RetrieveMpqIndex(String mpq,ref uint offset)
        {
            if (mpq.Length != 14)
                return false;

            char input = mpq[9];

            if (input >= '0' && input <= '9')
            {
                offset = (uint)(input - '0');
                return true;
            }
            return false;
        }

        public static CheckRevisionResult DoCheck(String formula, String mpq, String directory, ref uint output)
        {          
            const uint variableCount = 3;
            const uint operatorCount = 4;

            uint[] values = new uint[variableCount];

            OperatorType[] operators = new OperatorType[operatorCount];

            String[] tokens = formula.Split(' ');

            uint offset;
            for (offset = 0; offset < tokens.Length; offset++)
            {
                String token = tokens[offset];
                if (token == "4")
                {
                    offset++;
                    break;
                }
                else if (token.Length < 3)
                    return CheckRevisionResult.CHECK_REVISION_FORMULA_ERROR;
                char variableLetter = token[0];
                String numberString = token.Substring(2);
                uint number;
                try
                {
                    number = UInt32.Parse(numberString);
                }
                catch
                {
                    return CheckRevisionResult.CHECK_REVISION_FORMULA_ERROR;
                }

                uint variableIndex = 0;
                if (!GetVariableIndex(variableLetter,ref  variableIndex))
                    return CheckRevisionResult.CHECK_REVISION_FORMULA_ERROR;
                values[variableIndex] = number;
            }

            for (uint i = 0; offset < tokens.Length; i++, offset++)
            {
                String token = tokens[offset];
                if (token.Length != 5)
                    return CheckRevisionResult.CHECK_REVISION_FORMULA_ERROR;
                OperatorType current_operator;

                switch (token[3])
                {
                    case '+':
                        current_operator = new OperatorType(operator_add);
                        break;
                    case '-':
                        current_operator = new OperatorType(operator_sub);
                        break;
                    case '^':
                        current_operator = new OperatorType(operator_xor);                        
                        break;
                    default:
                        return CheckRevisionResult.CHECK_REVISION_FORMULA_ERROR;
                }
                operators[i] = current_operator;
            }
            uint mpq_index = 0;
            if (!RetrieveMpqIndex(mpq,ref mpq_index))
                return CheckRevisionResult.CHECK_REVISION_MPQ_ERROR;

            uint mpq_hash = mpqHashCodes[mpq_index];

            ulong a = values[0];
            ulong b = values[1];
            ulong c = values[2];

            a ^= mpq_hash;

            for (uint i = 0; i < d2Files.Length; i++)
            {
                String file = directory + d2Files[i];

                byte[] contentBytes =  File.ReadAllBytes(file);
                for (int j = 0; j < contentBytes.Length; j += 4)
                {
                    ulong s = (ulong)BitConverter.ToUInt32(contentBytes, j);

                    a = operators[0](a, s);
                    b = operators[1](b, c);
                    c = operators[2](c, a);
                    a = operators[3](a, b);

                }
            }
            output = (uint)c;

            return CheckRevisionResult.CHECK_REVISION_SUCCESS;
        }
    }
}
