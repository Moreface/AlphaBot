﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace BattleNet
{
    class GenericServer
    {
        static public readonly byte[] nulls = { 0x00, 0x00, 0x00, 0x00 };
        static public readonly byte[] ten = { 0x10, 0x00, 0x00, 0x00 };
        static public readonly byte[] six = { 0x06, 0x00, 0x00, 0x00 };
        static public readonly byte[] zero = { 0x00 };
        static public readonly byte[] one = { 0x01, 0x00, 0x00, 0x00 };

        static protected readonly String platform = "68XI", classic_id = "VD2D", lod_id = "PX2D";

        public TcpClient m_socket;
        protected NetworkStream m_stream;
        protected ClientlessBot m_owner;

        public virtual void Write(byte[] packet)
        {
            try
            {
                if(m_socket.Connected)
                    m_stream.Write(packet, 0, packet.Length);
            }
            catch
            {
            }
        }

        public void Kill()
        {
            m_stream.Close();
            m_socket.Close();
        }

        public virtual void PrintPacket(byte[] packet)
        {
            if (ClientlessBot.debugging)
            {
                Console.WriteLine("\tWriting to Stream: ");
                for (int i = 0; i < packet.Length; i++)
                {
                    if (i % 8 == 0 && i != 0)
                        Console.Write(" ");
                    if (i % 16 == 0 && i != 0)
                        Console.WriteLine("");
                    Console.Write("{0:X2} ", packet[i]);
                }
                Console.WriteLine("");
            }
        }
        public GenericServer(ClientlessBot cb)
        {
            m_owner = cb;
            m_socket = new TcpClient();
        }

        protected virtual Boolean GetPacket(ref List<byte> bncsBuffer, ref List<byte> data)
        {
            return false;
        }

        public virtual void ThreadFunction()
        {
        }

        protected delegate void PacketHandler(byte type, List<byte> data);

        public virtual byte[] BuildPacket(byte command, params IEnumerable<byte>[] args)
        {
            return null;
        }

        protected virtual PacketHandler DispatchPacket(byte type)
        {
            return null;
        }
    }
}
