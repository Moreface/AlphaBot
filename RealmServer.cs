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
using System.Net;

namespace CSharpClient
{
    class RealmServer : GenericServerConnection
    {
        public RealmServer(ClientlessBot cb) : base(cb)
        {
        }

        protected bool getMcpPacket(ref NetworkStream mcpStream, ref List<byte> mcpBuffer, ref List<byte> data)
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
                    m_socket.Close();
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
                    m_socket.Close();
                    return false;
                }
            }
            if (ClientlessBot.debugging)
                Console.WriteLine("");

            data = new List<byte>(mcpBuffer.GetRange(0, packetLength));
            mcpBuffer.RemoveRange(0, packetLength);
            return true;
        }

        public override byte[] BuildPacket(byte command, params IEnumerable<byte>[] args)
        {
            List<byte> packet = new List<byte>();

            List<byte> packetArray = new List<byte>();
            foreach (IEnumerable<byte> a in args)
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

        protected override PacketHandler DispatchPacket(byte type)
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

        protected void GameCreate(byte type, List<byte> data)
        {
            UInt32 result = BitConverter.ToUInt32(data.ToArray(), 9);
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
					Console.WriteLine("{0}: [MCP] Game server down (it is probably that you tried to create a nightmare/hell game with a character who doesn't have access to that difficulty yet, or gamename/password were invalid)", m_owner.Account);
					break;

				case 0x6e:
					Console.WriteLine("{0}: [MCP] Your character can't create a game because it's dead!",m_owner.Account);
					break;
			}
            if (result == 0) {
				if (ClientlessBot.debugging) Console.WriteLine("{0}: [MCP] Joining the game we just created",m_owner.Account);
				JoinGame();
			}
        }

        protected void GameJoin(byte type, List<byte> data)
        {
            UInt32 result = BitConverter.ToUInt32(data.ToArray(), 17);

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
                UInt32 ip = (UInt32) IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(data.ToArray(), 9));
				m_owner.GsIp = IPAddress.Parse(ip.ToString());
				m_owner.GsHash = data.GetRange(13, 4);
				m_owner.GsToken = data.GetRange(5, 2);

                //byte[] packeta = m_owner.m_bncs.BuildPacket(0x22, System.Text.Encoding.ASCII.GetBytes(lod_id), BitConverter.GetBytes((UInt32)0xd), System.Text.Encoding.ASCII.GetBytes(m_owner.GameName), zero, System.Text.Encoding.ASCII.GetBytes(m_owner.GamePassword), zero);
                //m_owner.m_bncs.Write(packeta);
                //byte[] packetb = m_owner.m_bncs.BuildPacket(0x10);
				//m_owner.m_bncs.Write(packetb);

                m_owner.StartGameServerThread();
			}
        }

        protected void LoginResult(byte type, List<byte> data)
        {
            UInt32 result = BitConverter.ToUInt32(data.ToArray(), 3);
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

        protected void CharacterList(byte type, List<byte> data)
        {
            UInt16 count = BitConverter.ToUInt16(data.ToArray(), 9);
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
                    String dataString = System.Text.Encoding.ASCII.GetString(data.ToArray());
                    String characterName = Utils.readNullTerminatedString(dataString, ref offset);
                    int length = data.IndexOf(0, offset)-offset;
                    List<byte> stats = data.GetRange(offset,length);
                    offset += length;
                    m_owner.ClassByte =(byte)((stats[13] - 0x01) & 0xFF);
                    byte level = stats[25];
                    byte flags = stats[26];
                    bool hardcore = (flags & 0x04) != 0;
                    bool dead = (flags & 0x08) != 0;
                    bool expansion = (flags & 0x20) != 0;
                    String coreString = hardcore ? "Hardcore" : "Softcore";
                    String versionString = expansion ? "Expansion" : "Classic";
                    String classType;

                    switch (m_owner.ClassByte)
                    {
                        case 0: classType = "Amazon";
                            break;
                        case 1: classType = "Sorceress";
                            break;
                        case 2: classType = "Necromancer";
                            break;
                        case 3: classType = "Paladin";
                            break;
                        case 4: classType = "Barbarian";
                            break;
                        case 5: classType = "Druid";
                            break;
                        case 6: classType = "Assassin";
                            break;
                        default: classType = "Unknown";
                            break;
                    }

                    Console.WriteLine("{0}: [MCP] {1}. {2}, Level: {3}, {6} ({4}|{5})", m_owner.Account, i, characterName, level, coreString, versionString, classType);

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
                m_stream.Write(packet, 0, packet.Length);
            }
        }

        protected void LoginRealm(byte type, List<byte> data)
        {
            UInt32 result = BitConverter.ToUInt32(data.ToArray(), 3);
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
                m_stream.Write(packet, 0, packet.Length);
            }
            else
            {
                byte[] packet = BuildPacket(0x07, System.Text.Encoding.ASCII.GetBytes(m_owner.Character), zero);
                m_stream.Write(packet, 0, packet.Length);
            }

        }

        protected void VoidRequest(byte type, List<byte> data)
        {
            Console.WriteLine("{0}: [MCP] Unknown Packet Received... Ignoring packet type: {1:X} ...", m_owner.Account,type);
        }

        public void CreateGameThreadFunction()
        {
            while (true) {

                //Replace this with mutex  or semaphore
		        while (m_owner.Status != ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME)
			        System.Threading.Thread.Sleep(1000);

		        System.Threading.Thread.Sleep(30000);
		        if (m_owner.FirstGame)
			        System.Threading.Thread.Sleep(30000);
                if(m_owner.Status == ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME) 
                {
       		        MakeGame();    
                }
                System.Threading.Thread.Sleep(5000);
	        }
        }

        protected void JoinGame()
        {
            Write(BuildPacket(0x04,BitConverter.GetBytes(m_owner.GameRequestId),System.Text.Encoding.ASCII.GetBytes(m_owner.GameName), zero, System.Text.Encoding.ASCII.GetBytes(m_owner.GamePassword),zero));
            m_owner.GameRequestId++;
        }
        protected void MakeGame()
        {
            if (m_owner.Password.Length == 0)
	            m_owner.Password = "xa1";

            m_owner.GameName = Utils.RandomString(10);
            if (m_owner.FailedGame) {
	            Console.WriteLine("{0}: [BNCS] Last game failed, sleeping.", m_owner.Account);
	            //debug_log.write("[" + nil::timestamp() + "] Last game failed, sleeping.\n");
	            System.Threading.Thread.Sleep(30000);
            }

            // We assume the game fails every game, until it proves otherwise at end of botthread.
            m_owner.FailedGame = true;

            Console.WriteLine("{0}: [MCP] Creating game \"{1}\" with password \"{2}\"",m_owner.Account,m_owner.GameName,m_owner.GamePassword);
            //debug_log.write("[" + nil::timestamp() + "] Creating game \"" + game_name + "\" with password \"" + game_password + "\"\n");
                
            byte[] temp = {0x01,0xff,0x08 };
            byte[] packet = BuildPacket(0x03, BitConverter.GetBytes((UInt16)m_owner.GameRequestId), BitConverter.GetBytes(Utils.GetDifficulty(m_owner.Difficulty)), temp, System.Text.Encoding.ASCII.GetBytes(m_owner.GameName), zero,
                            System.Text.Encoding.ASCII.GetBytes(m_owner.GamePassword), zero, zero);
                
            Write(packet);
            m_owner.GameRequestId++;
        }

        public override void ThreadFunction()
        {
           Console.Write("{0}: [MCP] Connecting to realm server {1}:{2} ...........",m_owner.Account, m_owner.McpIp,m_owner.McpPort);

            m_socket.Connect(m_owner.McpIp,(int)m_owner.McpPort);
            m_stream= m_socket.GetStream();
            if (m_stream.CanWrite)
            {
                Console.WriteLine(" Connected");
            }
            else
            {
                Console.WriteLine(" Failed To connect");
                return;
            }
            m_stream.WriteByte(0x01);
            byte[] packet = BuildPacket((byte)0x01, m_owner.McpData);

            PrintPacket(packet);

            m_stream.Write(packet, 0, packet.Length);

            List<byte> data = new List<byte>();
            List<byte> mcpBuffer = new List<byte>();
            while (true)
            {
                if (!getMcpPacket(ref m_stream, ref mcpBuffer, ref  data))
                    break;

                byte identifier = data.ToArray()[2];

                DispatchPacket(identifier)(identifier, data);
            }
            Console.WriteLine("{0}: [MCP] Disconnected from Realm Server", m_owner.Account);
        }

    }
}
