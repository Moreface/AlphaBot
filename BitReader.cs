using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CSharpClient
{
    class BitReader
    {
        private BitArray m_bits;
        private int m_offset;

        public BitReader(byte[] bytes)
        {
            m_bits = new BitArray(bytes);
            m_offset = 0;
        }

        public Boolean ReadBit()
        {
            return m_bits[m_offset++];
        }

        public Int32 Read(int length)
        {
            return ReadBitsLittleEndian(length);
        }



        public Int32 ReadBitsLittleEndian(int length)
        {
            int initialLen = length;
            Int32 bits = 0;
            while (length > 0)
            {
                bool bit = ReadBit();
                bits |= (Int32)((bit ? 1 : 0) << initialLen - length);

                length -= 1;
            }
            return bits;
        }

        public Int32 ReadBitsBigEndian(int length)
        {
            Int32 bits = 0;
            while (length > 0)
            {
                bits <<= 1;
                bool bit = ReadBit();
                bits += bit ? 1 : 0;
                length -= 1;
            }
            return bits;
        }

        public byte ReadByte()
        {
            return 0;
        }

        public byte ConvertToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }
        /*
        static void Main(string[] args)
        {
            byte[] ba = { 0x10,0x88 };
            BitReader br= new BitReader(ba);

            for (int i = 0; i < ba.Length * 8; i++)
            {
                Console.Write("{0} ", br.ReadBit() ? 1 : 0);
            }

            Console.WriteLine();

            br = new BitReader(ba);
            Console.WriteLine("{0}", br.ReadBitsLittleEndian(8));
            Console.WriteLine("{0}", br.ReadBitsLittleEndian(8));
            for (int i = 0; i < 8; i++)
            {
                Console.Write("{0} ", br.ReadBit() ? 1 : 0);
            }
            Console.ReadKey();

        }
        */
    }
}
