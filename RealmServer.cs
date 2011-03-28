using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Net;

namespace CSharpClient
{
    class RealmServer
    {
        String platform = "68XI", classic_id = "VD2D", lod_id = "PX2D";

        static byte[] nulls = { 0x00, 0x00, 0x00, 0x00 };
        static byte[] ten = { 0x10, 0x00, 0x00, 0x00 };
        static byte[] six = { 0x06, 0x00, 0x00, 0x00 };
        static byte[] zero = { 0x00 };

        private ClientlessBot m_owner;

        TcpClient m_mcpSocket;
        NetworkStream m_mcpStream;

        public void Write(byte[] packet)
        {
            m_mcpStream.Write(packet, 0, packet.Length);
        }

        public RealmServer(ClientlessBot cb)
        {
            m_owner = cb;
            m_mcpSocket = new TcpClient();
        }

        private bool getMcpPacket(ref NetworkStream mcpStream, ref ArrayList mcpBuffer, ref ArrayList data)
        {
            while (mcpBuffer.Count < 3)
            {
                try
                {
                    byte temp = (byte)mcpStream.ReadByte();
                    mcpBuffer.Add(temp);
                    if (ClientlessBot.debugging)
                    {
                        Console.Write("{0:X2} ", (byte)temp);
                    }
                }
                catch
                {
                    Console.WriteLine("\n{0}: [MCP] Lost Connection to MCP", m_owner.Account);
                    m_mcpSocket.Close();
                    return false;
                }
            }
            if (ClientlessBot.debugging)
                Console.WriteLine("");
            byte[] bytes = new byte[mcpBuffer.Count];
            mcpBuffer.CopyTo(bytes);

            short packetLength = BitConverter.ToInt16(bytes, 0);

            while (packetLength > mcpBuffer.Count)
            {
                try
                {
                    byte temp = (byte)mcpStream.ReadByte();
                    mcpBuffer.Add(temp);
                    if (ClientlessBot.debugging)
                    {
                        Console.Write("{0:X2} ", (byte)temp);
                    }
                }
                catch
                {
                    Console.WriteLine("\n{0}: [MCP] Lost Connection to MCP", m_owner.Account);
                    m_mcpSocket.Close();
                    return false;
                }
            }
            if (ClientlessBot.debugging)
                Console.WriteLine("");

            data = new ArrayList(mcpBuffer.GetRange(0, packetLength));
            mcpBuffer.RemoveRange(0, packetLength);
            return true;
        }

        private byte[] BuildPacket(byte command, params ICollection[] args)
        {
            ArrayList packet = new ArrayList();

            ArrayList packetArray = new ArrayList();
            foreach (ICollection a in args)
            {
                packetArray.AddRange(a);
            }

            UInt16 arrayCount = (UInt16)(packetArray.Count + 3);
            packet.AddRange(BitConverter.GetBytes(arrayCount));
            packet.Add((byte)command);
            packet.AddRange(packetArray);

            byte[] bytes = new byte[arrayCount];
            packet.CopyTo(bytes);
            return bytes;
        }

        private delegate void PacketHandler(byte type, ArrayList data, byte[] dataBytes);

        private PacketHandler DispatchPacket(byte type)
        {
            switch (type)
            {
                // Cases in order that they should be received
                case 0x01:
                    return LoginRealm;
                case 0x19:
                    return CharacterList;
                case 0x07:
                    return LoginResult;
                case 0x03:
                    return GameCreate;
                case 0x04:
                    return GameJoin;
                case 0x12:
                default:
                    return VoidRequest;
            }
        }

        private void GameCreate(byte type, ArrayList data, byte[] dataBytes)
        {
            UInt32 result = BitConverter.ToUInt32(dataBytes, 9);
            switch (result) {
				case 0x00:
					if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP] Game has been created successfully",m_owner.Account);
					break;

				case 0x1e:
					Console.WriteLine("{0}: [MCP] Invalid game name specified", m_owner.Account);;
					//make_game();
					break;

				case 0x1f:
					Console.WriteLine("{0}: [MCP] This game already exists",m_owner.Account);
					//make_game();
					break;

				case 0x20:
					Console.WriteLine("{0}: [MCP] Game server down (it is probable that you tried to create a nightmare/hell game with a character who doesn't have access to that difficulty yet, or gamename/password were invalid)", m_owner.Account);
					break;

				case 0x6e:
					Console.WriteLine("{0}: [MCP] Your character can't create a game because it's dead!",m_owner.Account);
					break;
			}
            if (result == 0) {
				if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP] Joining the game we just created",m_owner.Account);
				m_owner.JoinGame();
			}
        }

        private void GameJoin(byte type, ArrayList data, byte[] dataBytes)
        {
            UInt32 result = BitConverter.ToUInt32(dataBytes, 17);

			switch(result) {
				case 0x00:
					if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP] Successfully joined the game",m_owner.Account);
					break;

				case 0x29:
					Console.WriteLine("{0}: [MCP] Password is incorrect",m_owner.Account);
					break;

				case 0x2A:
					Console.WriteLine("{0}: [MCP] Game does not exist" ,m_owner.Account);

					break;

				case 0x2B:
					Console.WriteLine("{0}: [MCP] Game is full" ,m_owner.Account);
					break;

				case 0x2C:
					Console.WriteLine("{0}: [MCP] You do not meet the level requirements for this game" ,m_owner.Account);
					break;

				case 0x71:
					Console.WriteLine("{0}: [MCP] A non-hardcore character cannot join a game created by a Hardcore character." ,m_owner.Account);
					break;

				case 0x73:
					Console.WriteLine("{0}: [MCP] Unable to join a Nightmare game." ,m_owner.Account);
					break;

				case 0x74:
					Console.WriteLine("{0}: [MCP] Unable to join a Hell game." ,m_owner.Account);
					break;

				case 0x78:
					Console.WriteLine("{0}: [MCP] A non-expansion character cannot join a game created by an Expansion character." ,m_owner.Account);
					break;

				case 0x79:
					Console.WriteLine("{0}: [MCP] A Expansion character cannot join a game created by a non-expansion character." ,m_owner.Account);
					break;

				case 0x7D:
					Console.WriteLine("{0}: [MCP] A non-ladder character cannot join a game created by a Ladder character." ,m_owner.Account);
					break;
			}

			if (result == 0) {
				UInt32 ip = BitConverter.ToUInt32(dataBytes, 9);
				m_owner.GsIp = IPAddress.Parse(ip.ToString());
				m_owner.GsHash = data.GetRange(13, 4);
				m_owner.GsToken = data.GetRange(5, 2);

                byte[] packeta = m_owner.m_bncs.BuildPacket(0x22, System.Text.Encoding.ASCII.GetBytes(lod_id), BitConverter.GetBytes((UInt32)0xd), System.Text.Encoding.ASCII.GetBytes(m_owner.GameName), zero, System.Text.Encoding.ASCII.GetBytes(m_owner.GamePassword), zero);
                m_owner.m_bncs.Write(packeta);
                byte[] packetb = m_owner.m_bncs.BuildPacket(0x10);
				m_owner.m_bncs.Write(packetb);

                m_owner.StartGameServerThread();
			}
        }

        private void LoginResult(byte type, ArrayList data, byte[] dataBytes)
        {
            UInt32 result = BitConverter.ToUInt32(dataBytes, 3);
            if (result != 0)
            {
                Console.WriteLine("{0}: [MCP] Failed to log into character {1}", m_owner.Account, m_owner.Character);
                throw new Exception();
            }
            if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP]  Successfully logged into character",m_owner.Account);

			if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP]  Requesting channel list",m_owner.Account);
            byte[] packet = m_owner.m_bncs.BuildPacket(0x0b, System.Text.Encoding.ASCII.GetBytes(lod_id));
			m_owner.m_bncs.Write(packet);

			if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP]  Entering the chat server",m_owner.Account);
            byte[] comma = {0x2C};
            byte[] packetb =  m_owner.m_bncs.BuildPacket(0x0a,System.Text.Encoding.ASCII.GetBytes(m_owner.Character),zero, System.Text.Encoding.ASCII.GetBytes(m_owner.Realm), comma ,System.Text.Encoding.ASCII.GetBytes(m_owner.Character), zero);
             m_owner.m_bncs.Write(packetb);
			

			if (!m_owner.LoggedIn) {
				if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP]  Requesting MOTD",m_owner.Account);
			    byte[] packetc = BuildPacket(0x12);
                
                Write(packetc);
				m_owner.LoggedIn = true;
			}
            m_owner.Status= ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME;
        }

        private void CharacterList(byte type, ArrayList data, byte[] dataBytes)
        {
            UInt16 count = BitConverter.ToUInt16(dataBytes, 9);
            if (count == 0)
            {
                Console.WriteLine("{0}: [MCP] There are no characters on this account", m_owner.Account);
                m_owner.Status = ClientlessBot.ClientStatus.STATUS_ON_MCP;
            }
            else
            {
                bool foundCharacter = false;
                bool selectFirstCharacter = false;
                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [MCP] List of characters on this account", m_owner.Account);
                int offset = 11;

                for (int i = 1; i <= count; i++)
                {
                    offset += 4;
                    String dataString = System.Text.Encoding.ASCII.GetString(dataBytes);
                    String characterName = Utils.readNullTerminatedString(dataString, ref offset);
                    int oldoffset = offset;
                    String stats = Utils.readNullTerminatedString(dataString, ref offset);
                    ArrayList statList = data.GetRange(oldoffset, offset - oldoffset);
                    // This section needs to be finished to gather character info
                    /*
                     *
                     * 
                     * Some stats stuff needs to go here... too lazy........
                     * 
                     * 
                     */


                    if (m_owner.Character == null && i == 1)
                    {
                        selectFirstCharacter = true;
                        m_owner.Character = characterName;
                    }

                    if (m_owner.Character.Equals(characterName))
                    {
                        foundCharacter = true;
                    }
                }
                if (selectFirstCharacter)
                    Console.WriteLine("{0}: [MCP] No character specified, chose first character", m_owner.Account);
                if (!foundCharacter)
                {
                    Console.WriteLine("{0}: [MCP] Unable to locate character specified", m_owner.Account);
                    return;
                }

                Console.WriteLine("{0}: [MCP] Logging on to character {1}", m_owner.Account, m_owner.Character);

                byte[] packet = BuildPacket(0x07, System.Text.Encoding.ASCII.GetBytes(m_owner.Character), zero);
                m_mcpStream.Write(packet, 0, packet.Length);
            }
        }

        private void LoginRealm(byte type, ArrayList data, byte[] dataBytes)
        {
            UInt32 result = BitConverter.ToUInt32(dataBytes, 3);
            switch (result)
            {
                case 0x00:
                    if (ClientlessBot.debugging)
                        Console.WriteLine("{0}: [MCP] Successfully Logged on to the Realm Server", m_owner.Account);
                    break;
                case 0x7e:
                    Console.WriteLine("{0}: [MCP] Your CD-Key has been banned from this realm!", m_owner.Account);
                    break;
                case 0x7f:
                    Console.WriteLine("{0}: [MCP] Your IP has been temporarily banned", m_owner.Account);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_REALM_DOWN;
                    //terminate connection
                    break;
                default:
                    Console.WriteLine("{0}: [MCP] Unknown Logon Error Occured...", m_owner.Account);
                    break;
            }
            if (result != 0)
                return;

            if (!m_owner.LoggedIn)
            {
                Console.WriteLine("{0}: [MCP] Requesting Character list...", m_owner.Account);
                byte[] packet = BuildPacket(0x19, BitConverter.GetBytes(8));
                m_mcpStream.Write(packet, 0, packet.Length);
            }
            else
            {
                byte[] packet = BuildPacket(0x07, System.Text.Encoding.ASCII.GetBytes(m_owner.Character), zero);
                m_mcpStream.Write(packet, 0, packet.Length);
            }

        }

        private void VoidRequest(byte type, ArrayList data, byte[] dataBytes)
        {
            Console.WriteLine("{0}: [MCP] Unknown Packet Received... Ignoring packet type: {1:X} ...", m_owner.Account,type);
        }

        public void McpThreadFunction()
        {
           Console.Write("{0}: [MCP] Connecting to realm server {1}:{2} ...........",m_owner.Account, m_owner.McpIp,m_owner.McpPort);

            m_mcpSocket.Connect(m_owner.McpIp,(int)m_owner.McpPort);
            m_mcpStream= m_mcpSocket.GetStream();
            if (m_mcpStream.CanWrite)
            {
                Console.WriteLine(" Connected");
            }
            else
            {
                Console.WriteLine(" Failed To connect");
                return;
            }
            m_mcpStream.WriteByte(0x01);
            byte[] packet = BuildPacket((byte)0x01, m_owner.McpData);
            m_mcpStream.Write(packet, 0, packet.Length);

            ArrayList data = new ArrayList();
            ArrayList mcpBuffer = new ArrayList();
            while (true)
            {
                if (!getMcpPacket(ref m_mcpStream, ref mcpBuffer, ref  data))
                    break;
                byte[] dataBytes = new byte[data.Count];
                data.CopyTo(dataBytes);

                byte identifier = dataBytes[2];

                DispatchPacket(identifier)(identifier, data, dataBytes);
            }
            Console.WriteLine("{0}: [MCP] Disconnected from Realm Server", m_owner.Account);
        }

    }
}
