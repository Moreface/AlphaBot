using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections;

namespace CSharpClient
{
    class GameServer
    {
        static byte[] zero = { 0x00 };
        private static Int32 s_gsPort = 4000;
        private ClientlessBot m_owner;

        TcpClient m_gsSocket;
        NetworkStream m_gsStream;

        public void Write(byte[] packet)
        {
            m_gsStream.Write(packet, 0, packet.Length);
        }

        private byte[] BuildPacket(byte command, params IEnumerable<byte>[] args)
        {
            List<byte> packet = new List<byte>();

            List<byte> packetArray = new List<byte>();
            foreach (IEnumerable<byte> a in args)
            {
                packetArray.AddRange(a);
            }

            packet.Add((byte)command);
            packet.AddRange(packetArray);

            byte[] bytes = new byte[packet.Count];
            packet.CopyTo(bytes);
            return bytes;
        }

        public GameServer(ClientlessBot cb)
        {
            m_owner = cb;
            m_gsSocket = new TcpClient();
        }
        public void GameServerThreadFunction()
        {

            //Initialize the game's data

            Console.Write("{0}: [D2GS] Connecting to Game Server {1}:{2} .......",m_owner.Account,m_owner.GsIp,s_gsPort);
            m_gsSocket.Connect(m_owner.GsIp, s_gsPort);
            m_gsStream = m_gsSocket.GetStream();
            if (m_gsStream.CanWrite)
            {
                Console.WriteLine(" Connected");
            }
            else
            {
                Console.WriteLine(" Failed To connect");
                return;
            }

            m_owner.ConnectedToGs = true;
            List<byte> buffer = new List<byte>();
            byte[] byteBuffer = new byte[4096];
            Int32 bytesRead;
            while (true)
            {
                if(!m_gsStream.DataAvailable)
                {
                    if (ClientlessBot.debugging)
                        Console.WriteLine("{0}: [D2GS] Disconnected from game server", m_owner.Account);
                    if (m_owner.ConnectedToGs)
                    {
                        m_owner.ConnectedToGs = false;
                        // Join threads
                    }
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME;
                    break;
                }
                bytesRead = m_gsStream.Read(byteBuffer, 0, byteBuffer.Length);

                buffer.AddRange(byteBuffer);
                while (true)
                {
                    UInt16 receivedPacket = BitConverter.ToUInt16(byteBuffer,0);
                    if (buffer.Count >= 2 && receivedPacket == (UInt16)0xaf01)
                    {
                        //if (ClientlessBot.debugging)
                            Console.WriteLine("{0}: [D2GS] Logging on to game server", m_owner.Account);

                        byte[] temp = {0x50, 0xcc, 0x5d, 0xed, 0xb6, 0x19, 0xa5, 0x91};

                        Int32 pad = m_owner.Character.Length;

                        byte[] padding = new byte[pad];

                        byte[] packet = BuildPacket(0x68, m_owner.GsHash, m_owner.GsToken, /*m_owner.ClassByte,*/ BitConverter.GetBytes((UInt32)0xd), temp, zero, System.Text.Encoding.ASCII.GetBytes(m_owner.Character), padding);
                        buffer.RemoveRange(0, 2);
                        byteBuffer = new byte[buffer.Count];
                        buffer.CopyTo(byteBuffer, 0);
                        continue;
                    }

                }
            }
        }
    }
}
