using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace CSharpClient
{
    class GenericServerConnection
    {
        static protected byte[] nulls = { 0x00, 0x00, 0x00, 0x00 };
        static protected byte[] ten = { 0x10, 0x00, 0x00, 0x00 };
        static protected byte[] six = { 0x06, 0x00, 0x00, 0x00 };
        static protected byte[] zero = { 0x00 };
        static protected String platform = "68XI", classic_id = "VD2D", lod_id = "PX2D";

        public TcpClient m_socket;
        protected NetworkStream m_stream;
        protected ClientlessBot m_owner;
        public virtual void Write(byte[] packet)
        {
            m_stream.Write(packet, 0, packet.Length);
        }

        public GenericServerConnection(ClientlessBot cb)
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
