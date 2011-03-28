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
    class BattleNetCS
    {
        static int s_bncsPort = 6112;

        static byte[] nulls = { 0x00, 0x00, 0x00, 0x00 };
        static byte[] ten = { 0x10, 0x00, 0x00, 0x00 };
        static byte[] six = { 0x06, 0x00, 0x00, 0x00 };
        static byte[] zero = { 0x00 };

        String platform = "68XI", classic_id = "VD2D", lod_id = "PX2D";

        static byte[] AuthInfoPacket =
	    {
		    0xff, 0x50, 0x3a, 0x00, 0x00, 0x00, 0x00, 0x00,
		    0x36, 0x38, 0x58, 0x49, 0x50, 0x58, 0x32, 0x44,
		    0x0d, 0x00, 0x00, 0x00, 0x53, 0x55, 0x6e, 0x65,
		    0x55, 0xb4, 0x47, 0x40, 0x88, 0xff, 0xff, 0xff,
		    0x09, 0x04, 0x00, 0x00, 0x09, 0x04, 0x00, 0x00,
		    0x55, 0x53, 0x41, 0x00, 0x55, 0x6e, 0x69, 0x74,
		    0x65, 0x64, 0x20, 0x53, 0x74, 0x61, 0x74, 0x65,
		    0x73, 0x00
	    };

        public TcpClient m_bncsSocket;
        public NetworkStream m_bncsStream;

        ClientlessBot m_owner;

        public BattleNetCS(ClientlessBot cb)
        {
            m_owner = cb;
            m_bncsSocket = new TcpClient();
        }

        private bool getBncsPacket(ref NetworkStream bncsStream, ref List<byte> bncsBuffer, ref List<byte> data)
        {
            while (bncsBuffer.Count < 4)
            {
                try
                {
                    byte temp = (byte)bncsStream.ReadByte();
                    bncsBuffer.Add(temp);
                    if (ClientlessBot.debugging)
                    {
                        Console.Write("{0:X2} ", (byte)temp);
                    }
                }
                catch
                {
                    Console.WriteLine("\n{0}: [BNCS] Disconnected From BNCS", m_owner.Account);
                    m_bncsSocket.Close();
                    return false;
                }
            }
            if (ClientlessBot.debugging)
                Console.WriteLine("");
            byte[] bytes = new byte[bncsBuffer.Count];
            bncsBuffer.CopyTo(bytes);

            short packetLength = BitConverter.ToInt16(bytes, 2);

            while (packetLength > bncsBuffer.Count)
            {
                try
                {
                    byte temp = (byte)bncsStream.ReadByte();
                    bncsBuffer.Add(temp);
                    if (ClientlessBot.debugging)
                    {
                        Console.Write("{0:X2} ", (byte)temp);
                    }
                }
                catch
                {
                    Console.WriteLine("\n{0}: [BNCS] Disconnected From BNCS", m_owner.Account);
                    m_bncsSocket.Close();
                    return false;
                }
            }
            if (ClientlessBot.debugging)
                Console.WriteLine("");
            data = new List<byte>(bncsBuffer.GetRange(0, packetLength));
            bncsBuffer.RemoveRange(0, packetLength);
            return true;
        }

        public void Write(byte[] packet)
        {
            m_bncsStream.Write(packet, 0, packet.Length);
        }

        public byte[] BuildPacket(byte command, params IEnumerable<byte>[] args)
        {
            List<byte> packet = new List<byte>();
            packet.Add((byte)0xFF);
            packet.Add((byte)command);
            List<byte> packetArray = new List<byte>();
            foreach (IEnumerable<byte> a in args)
            {
                packetArray.AddRange(a);
            }

            UInt16 arrayCount = (UInt16)(packetArray.Count + 4);
            packet.AddRange(BitConverter.GetBytes(arrayCount));

            packet.AddRange(packetArray);

            byte[] bytes = new byte[arrayCount];
            packet.CopyTo(bytes);
            return bytes;
        }

        private delegate void PacketHandler(ulong type, List<byte> data);

        private PacketHandler DispatchPacket(ulong type)
        {
            switch (type)
            {
                // Cases in order that they should be received
                case 0x00:
                case 0x25: return PingRequest;
                case 0x50: return AuthInfoRequest;
                case 0x51: return AuthCheck;
                case 0x33: return AccountLogin;
                case 0x3a: return LoginResult;
                case 0x40: return RealmList;
                case 0x3e: return StartMcp;
                case 0x0a: return EnterChat;
                case 0x15: return HandleAdvertising;
                default:   return VoidRequest;
            }
        }

        private void HandleAdvertising(ulong type, List<byte> data)
        {
            UInt32 ad_id = BitConverter.ToUInt32(data.ToArray(), 4);
            if (ClientlessBot.debugging) Console.WriteLine("{0}: [BNCS] Received advertising data, sending back display confirmation", m_owner.Account);
            byte[] packet = BuildPacket((byte)0x21, System.Text.Encoding.ASCII.GetBytes(platform), System.Text.Encoding.ASCII.GetBytes(lod_id), BitConverter.GetBytes(ad_id), zero, zero);
            m_bncsStream.Write(packet, 0, packet.Length);
        }

        private void EnterChat(ulong type, List<byte> data)
        {
            Console.WriteLine("{0}: [BNCS] Entered the chat.", m_owner.Account);
            byte[] packeta = {0xFF, 0x46, 0x04, 0x00 };
            m_bncsStream.Write(packeta, 0, packeta.Length);
            byte[] packetb = BuildPacket((byte)0x15, System.Text.Encoding.ASCII.GetBytes(platform), System.Text.Encoding.ASCII.GetBytes(lod_id), BitConverter.GetBytes((uint)System.Environment.TickCount));
            m_bncsStream.Write(packetb,0,packetb.Length);

            m_owner.StartGameCreationThread();
        }

        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        private void StartMcp(ulong type, List<byte> data)
        {
            if (data.Count<= 12)
            {
                Console.WriteLine("{0}: [BNCS] Failed to log on to realm:", m_owner.Account); ;
				return;
			}

            UInt32 ip = (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(data.ToArray(), 20));
            m_owner.McpPort = ReverseBytes(BitConverter.ToUInt16(data.ToArray(), 24));
            
            m_owner.McpIp = IPAddress.Parse(ip.ToString());

            Int32 offset = 28;
            List<byte> temp = new List<byte>(data.GetRange(4,16));
            temp.AddRange(data.GetRange(offset, data.Count - offset));
            m_owner.McpData = temp;

            m_owner.StartMcpThread();
        }

        private void RealmList(ulong type, List<byte> data)
        {
            UInt32 count = BitConverter.ToUInt32(data.ToArray(), 8);
			Int32 offset = 12;
			
            if (ClientlessBot.debugging) 
                Console.WriteLine("{0}: [BNCS] List of realms:", m_owner.Account);

			for(ulong i = 1; i <= count; i++)
			{
				offset += 4;
				String realmTitle = Utils.readNullTerminatedString(System.Text.Encoding.ASCII.GetString(data.ToArray()), ref offset);
                String realmDescription = Utils.readNullTerminatedString(System.Text.Encoding.ASCII.GetString(data.ToArray()), ref offset);
				if (ClientlessBot.debugging) 
                    Console.WriteLine("{0}: [BNCS] {1}. {2}, {3}",m_owner.Account,i, realmTitle,realmDescription);

				if(m_owner.Realm == null && i == 1)
				{
					if (ClientlessBot.debugging) 
                        Console.WriteLine("{0}: [BNCS] No realm was specified in the ini so we're going to connect to {1}", m_owner.Account, realmTitle);
					m_owner.Realm = realmTitle;
				}
			}

			if (m_owner.LoggedIn) {
               //make_game();
			} else {

                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [BNCS] Logging on to the realm {1}", m_owner.Account, m_owner.Realm);

				UInt32 clientToken = 1;
                byte[] packet = BuildPacket((byte)0x3e, BitConverter.GetBytes(clientToken), Bsha1.DoubleHash(clientToken, m_owner.ServerToken, "password"), System.Text.Encoding.ASCII.GetBytes(m_owner.Realm), zero);

                byte[] temp = System.Text.Encoding.ASCII.GetBytes(m_owner.Realm);

                if (ClientlessBot.debugging)
                {
                    Console.WriteLine("\tSize of realm string: {0}", temp.Length);

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
				m_bncsStream.Write(packet,0,packet.Length);
			}
        }

        private void LoginResult(ulong type, List<byte> data)
		{
            UInt32 result = BitConverter.ToUInt32(data.ToArray(), 4);
			switch(result)
			{
				case 0x00:
                    if (ClientlessBot.debugging) Console.WriteLine("{0}: [BNCS] Successfully logged into the account ", m_owner.Account);
                    break;
				case 0x01:
					Console.WriteLine("{0}: [BNCS] Account does not exist", m_owner.Account);
					break;

				case 0x02:
					Console.WriteLine("{0}: [BNCS] Invalid password specified" , m_owner.Account);
					break;

				case 0x06:
					Console.WriteLine("{0}: [BNCS] Account has been closed" , m_owner.Account);
					break;

				default:
                    Console.WriteLine("{0}: [BNCS] Unknown login error ({1})", m_owner.Account,result);
					break;
			}

            if (result == 0)
            {
                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [BNCS] Requesting Realm List", m_owner.Account);
                byte[] packet = { 0xFF, 0x40, 0x04, 0x00 };
                m_bncsStream.Write(packet, 0, 4);
            }
            else
                return;
		}

        private void AccountLogin(ulong type, List<byte> data)
        {
			if (ClientlessBot.debugging) 
                Console.WriteLine("{0}: [BNCS] Logging into the account",m_owner.Account);
			UInt32 client_token = (uint)System.Environment.TickCount;
			List<byte> hash = Bsha1.DoubleHash(client_token, m_owner.ServerToken, m_owner.Password);

            byte[] packet = BuildPacket((byte)0x3a, BitConverter.GetBytes(client_token), BitConverter.GetBytes(m_owner.ServerToken), hash, System.Text.Encoding.ASCII.GetBytes(m_owner.Account), zero);

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
            m_bncsStream.Write(packet, 0, packet.Length);
		}
        
        private void VoidRequest(ulong type, List<byte> data)
        {
            Console.WriteLine("{0}: [BNCS] Unknown Packet Received... Ignoring...", m_owner.Account);
        }

        private void PingRequest(ulong type, List<byte> data)
        {
            Console.Write("{0}: [BNCS] Replying to Ping request ........", m_owner.Account);
            m_bncsStream.Write(data.ToArray(), 0, data.Count);
            Console.WriteLine("Done");
        }

        private void AuthCheck(ulong type, List<byte> data)
        {
            if (ClientlessBot.debugging)
                Console.WriteLine("{0}: [BNCS] Auth Check:", m_owner.Account);
            ulong result = BitConverter.ToUInt32(data.ToArray(), 4);
            String info = BitConverter.ToString(data.ToArray(), 8);

            if (!handleAuthCheckResult(result, info))
                return;
        }

        private void AuthInfoRequest(ulong type, List<byte> data)
        {
            if (ClientlessBot.debugging)
                Console.WriteLine("{0}: [BNCS] Received Auth Info Packet", m_owner.Account);

            m_owner.ServerToken = BitConverter.ToUInt32(data.ToArray(), 8);
            List<byte> temp = data.GetRange(16, 8);
            byte[] tbytes = new byte[temp.Count];
            temp.CopyTo(tbytes);

            String mpq_file_time = System.Text.Encoding.ASCII.GetString(tbytes);

            int offset;
            if (data.ToArray()[24] == 0x76)
                offset = 24;
            else
                offset = 24;
            Console.WriteLine((char)data.ToArray()[24]);

            String mpq_file_name = Utils.readNullTerminatedString(System.Text.Encoding.ASCII.GetString(data.ToArray()), ref offset);
            Console.WriteLine(mpq_file_name[0]);
            String formula_string = Utils.readNullTerminatedString(System.Text.Encoding.ASCII.GetString(data.ToArray()), ref offset);
            Console.WriteLine(formula_string[0]);

            if (ClientlessBot.debugging)
            {
                Console.WriteLine("\tServer Token: {0}",  m_owner.ServerToken);
                Console.WriteLine("\tMPQ File Time: {0}", mpq_file_time);
                Console.WriteLine("\tMPQ File Name: {0}", mpq_file_name);
                Console.WriteLine("\tFormula String: {0}", formula_string);
            }
            uint exe_checksum = 0;
            switch (CheckRevision.DoCheck(formula_string, mpq_file_name,  m_owner.BinaryDirectory, ref exe_checksum))
            {
                case CheckRevision.CheckRevisionResult.CHECK_REVISION_SUCCESS:
                    if (ClientlessBot.debugging)
                        Console.WriteLine("\t\tCheck Revision SUCCESS");
                    break;
                default:
                    if (ClientlessBot.debugging)
                        Console.WriteLine("\t\tCheck Revision Failed");
                    break;
            }

            uint client_token = (uint)System.Environment.TickCount;

            List<byte> classic_hash = new List<byte>(), lod_hash = new List<byte>(), classic_public = new List<byte>(), lod_public = new List<byte>();


            if (CdKey.GetD2KeyHash(m_owner.ClassicKey, ref  client_token, m_owner.ServerToken, ref classic_hash, ref classic_public))
            {
                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [BNCS] Successfully generated the classic CD key hash", m_owner.Account);
            }
            else
            {
                Console.WriteLine("{0}: [BNCS] CD key is invalid", m_owner.Account);
                m_owner.Status = ClientlessBot.ClientStatus.STATUS_INVALID_CD_KEY;
            }

            if (CdKey.GetD2KeyHash(m_owner.ExpansionKey, ref client_token, m_owner.ServerToken, ref  lod_hash, ref lod_public))
            {
                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [BNCS] Successfully generated the lod CD key hash", m_owner.Account);
            }
            else
            {
                Console.WriteLine("{0}: [BNCS] Expansion CD key is invalid", m_owner.Account);
                m_owner.Status = ClientlessBot.ClientStatus.STATUS_INVALID_EXP_CD_KEY;
            }


            byte[] packet =  BuildPacket((byte)0x51, BitConverter.GetBytes(client_token), BitConverter.GetBytes(0x01000001), BitConverter.GetBytes(exe_checksum),
                            BitConverter.GetBytes(0x00000002), nulls, ten, six, classic_public, nulls, classic_hash, ten, BitConverter.GetBytes((UInt32)10),
                            lod_public, nulls, lod_hash, System.Text.Encoding.UTF8.GetBytes(m_owner.GameExeInformation), zero, System.Text.Encoding.ASCII.GetBytes(m_owner.KeyOwner),zero);

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
            m_bncsStream.Write(packet, 0, packet.Length);
        }

        public void BncsThreadFunction()
        {
            m_owner.GameRequestId = 0x02;
            m_owner.InGame = false;

            Console.Write("{0}: [BNCS] Connecting to {1}:{2} .........",  m_owner.Account,  m_owner.BattleNetServer, s_bncsPort);
            // Establish connection
            m_bncsSocket.Connect(m_owner.BattleNetServer, s_bncsPort);
            m_bncsStream = m_bncsSocket.GetStream();
            if (m_bncsStream.CanWrite)
            {
                Console.WriteLine(" Connected");
            }
            else
            {
                Console.WriteLine(" Failed To connect");
                return;
            }

            m_bncsStream.WriteByte(0x01);
            m_bncsStream.Write(AuthInfoPacket, 0, AuthInfoPacket.Length);

            List<byte> bncsBuffer = new List<byte>();
            List<byte> data = new List<byte>();
            while (true)
            {

                if (!getBncsPacket(ref m_bncsStream, ref bncsBuffer, ref data))
                {
                    break;
                }
                
                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [BNCS] Received Packet From Server",  m_owner.Account);

                if (ClientlessBot.debugging)
                {
                    Console.WriteLine("\tPacket Data: ");
                    Console.Write("\t\t");
                    for (int i = 0; i < data.Count; i++)
                    {
                        Console.Write("{0:X2} ", (byte)data[i]);
                    }
                    Console.WriteLine("");
                }

                ulong type = data.ToArray()[1];
                if (ClientlessBot.debugging)
                    Console.WriteLine("\tPacket Type: 0x{0:X}", type);
                DispatchPacket(type)(type, data);
             }            
        }

        private bool handleAuthCheckResult(ulong result, string info)
        {
            switch (result)
            {
                case 0x000:
                    Console.WriteLine("{0}: [BNCS] Successfully logged on to Battle.net", m_owner.Account);
                    break;
                case 0x100:
                    Console.WriteLine("{0}: [BNCS] Outdated game version", m_owner.Account);
                    break;
                case 0x101:
                    Console.WriteLine("{0}: [BNCS] Invalid version", m_owner.Account);
                    break;
                case 0x102:
                    Console.WriteLine("{0}: [BNCS] Game version must be downgraded to {1}", m_owner.Account, info);
                    break;

                case 0x200:
                    Console.WriteLine("{0}: [BNCS] Invalid CD key", m_owner.Account);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_INVALID_CD_KEY;
                    break;
                case 0x210:
                    Console.WriteLine("{0}: [BNCS] Invalid Expansion CD key", m_owner.Account);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_INVALID_EXP_CD_KEY;
                    break;

                case 0x201:
                    Console.WriteLine("{0}: [BNCS] CD key is being used by {1}", m_owner.Account, info);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_KEY_IN_USE;
                    break;
                case 0x211:
                    Console.WriteLine("{0}: [BNCS] Expansion CD key is being used by {1}", m_owner.Account, info);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_EXP_KEY_IN_USE;
                    break;

                case 0x202:
                    Console.WriteLine("{0}: [BNCS] This CD key has been banned", m_owner.Account, info);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_BANNED_CD_KEY;
                    break;
                case 0x212:
                    Console.WriteLine("{0}: [BNCS] This Expansion CD key has been banned", m_owner.Account);
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_BANNED_EXP_CD_KEY;
                    break;

                case 0x203:
                    Console.WriteLine("{0}: [BNCS] This CD key is meant to be used with another product", m_owner.Account);
                    break;

                default:
                    Console.WriteLine("{0}: [BNCS] Failed to log on to Battle.net ({1})", m_owner.Account, result);
                    break;
            }
            if (result == 0)
            {
                if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [BNCS] Requesting ini file time", m_owner.Account);
                List<byte> args = new List<byte>(BitConverter.GetBytes(0x80000004));
                byte[] nulls = { 0x00, 0x00, 0x00, 0x00 };
                args.AddRange(nulls);
                args.AddRange(System.Text.Encoding.UTF8.GetBytes("bnserver-D2DV.ini"));
                args.Add((byte)0x00);
               byte[] packet = BuildPacket((byte)0x33, args);

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
                m_bncsStream.Write(packet, 0, packet.Length);

                return true;
            }

            return false;

        }
    }
}
