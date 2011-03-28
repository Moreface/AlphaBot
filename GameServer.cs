using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace CSharpClient
{
    class GameServer
    {
        private static Int32 s_gsPort = 4000;
        private ClientlessBot m_owner;

        TcpClient m_gsSocket;
        NetworkStream m_gsStream;

        public void Write(byte[] packet)
        {
            m_gsStream.Write(packet, 0, packet.Length);
        }

        public GameServer(ClientlessBot cb)
        {
            m_owner = cb;
            m_gsSocket = new TcpClient();
        }
        public void GameServerThreadFunction()
        {
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
        }
    }
}
