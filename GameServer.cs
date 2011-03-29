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
using System.Net.Sockets;
using System.Collections;

namespace CSharpClient
{
    class GameServer : GenericServerConnection
    {
        protected static Int32 s_gsPort = 4000;

        public override byte[] BuildPacket(byte command, params IEnumerable<byte>[] args)
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

        public GameServer(ClientlessBot cb) : base(cb)
        {
        }
        public override void ThreadFunction()
        {

            //Initialize the game's data

            Console.Write("{0}: [D2GS] Connecting to Game Server {1}:{2} .......",m_owner.Account,m_owner.GsIp,s_gsPort);
            try
            {
                m_socket.Connect(m_owner.GsIp, s_gsPort);
                m_stream = m_socket.GetStream();
                Console.WriteLine(" Connected");
            }
            catch
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
                if (!m_stream.DataAvailable)
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
                bytesRead = m_stream.Read(byteBuffer, 0, byteBuffer.Length);

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

                        byte[] packet = BuildPacket(0x68, m_owner.GsHash, m_owner.GsToken, new List<byte>(m_owner.ClassByte) , BitConverter.GetBytes((UInt32)0xd), temp, zero, System.Text.Encoding.ASCII.GetBytes(m_owner.Character), padding);
                        buffer.RemoveRange(0, 2);
                        continue;
                    }

                    if (buffer.Count < 2 || (buffer[0] >= 0xF0 && buffer.Count < 3))
                    {
                        break;
                    }

                }
            }
        }
    }
}
