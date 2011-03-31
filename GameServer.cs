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
using System.Threading;

namespace CSharpClient
{
    class GameServer : GenericServerConnection
    {
        protected static Int32 s_gsPort = 4000;

        static readonly Int16[] packetSizes =
	    {
		    1, 8, 1, 12, 1, 1, 1, 6, 6, 11, 6, 6, 9, 13, 12, 16,
		    16, 8, 26, 14, 18, 11, 0, 0, 15, 2, 2, 3, 5, 3, 4, 6,
		    10, 12, 12, 13, 90, 90, 0, 40, 103,97, 15, 0, 8, 0, 0, 0,
		    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 34, 8,
		    13, 0, 6, 0, 0, 13, 0, 11, 11, 0, 0, 0, 16, 17, 7, 1,
		    15, 14, 42, 10, 3, 0, 0, 14, 7, 26, 40, 0, 5, 6, 38, 5,
		    7, 2, 7, 21, 0, 7, 7, 16, 21, 12, 12, 16, 16, 10, 1, 1,
		    1, 1, 1, 32, 10, 13, 6, 2, 21, 6, 13, 8, 6, 18, 5, 10,
		    4, 20, 29, 0, 0, 0, 0, 0, 0, 2, 6, 6, 11, 7, 10, 33,
		    13, 26, 6, 8, 0, 13, 9, 1, 7, 16, 17, 7, 0, 0, 7, 8,
		    10, 7, 8, 24, 3, 8, 0, 7, 0, 7, 0, 7, 0, 0, 0, 0,
		    1
	    };

        public Thread m_pingThread;

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
            m_pingThread = new Thread(PingThreadFunction);
        }

        protected void PingThreadFunction()
        {
            while (m_owner.ConnectedToGs)
            {
                List<byte> packet = new List<byte>();
                packet.Add(0x6d);
                packet.AddRange(BitConverter.GetBytes((uint)System.Environment.TickCount));
                packet.AddRange(nulls);
                packet.AddRange(nulls);
                Write(packet.ToArray());

                int sleepStep = 100;
                for (int i = 0; i < 5000 && m_owner.ConnectedToGs; i += sleepStep)
                {
                    Thread.Sleep(sleepStep);
                }
            }
        }

        protected void PingStart()
        {
            m_pingThread = new Thread(PingThreadFunction);
            m_pingThread.Start();
        }

        protected void DefaultHandler(byte type, List<byte> data)
        {
            //Console.WriteLine("{0}: [D2GS] No known handler for this packet", m_owner.Account);
        }

        protected override PacketHandler DispatchPacket(byte type)
        {
            switch (type)
            {
                case 0x00: return GameLoading;
                case 0x01: return GameFlagsPing;
                case 0x02: return StartPingThread;
                case 0x03: return LoadActData;
                case 0x0c: return NpcUpdate;
                case 0x0f: return PlayerUpdate;
                case 0x15: return PlayerReassign;
                case 0x1a: case 0x1b: case 0x1c: return ProcessExperience;
                case 0x1d: return SetPlayerLevel;
                case 0x21: case 0x22: return ItemSkillBonus;
                case 0x26: return ChatMessage;
                case 0x27: return NpcInteraction;
                case 0x5b: return PlayerJoins;
                case 0x5c: return PlayerLeaves;
                case 0x59: return InitializePlayer;
                case 0x67: return NpcMovement;
                case 0x68: return NpcMoveEntity;
                case 0x69: return NpcStateUpdate;
                case 0x6d: return NpcStoppedMoving;
                case 0x81: return MercUpdate;
                case 0x82: return PortalUpdate;
                case 0x8f: return Pong;
                case 0x95: return LifeManaPacket;
                case 0x97: return WeaponSetSwitched;
                case 0x9c: case 0x9d: return ItemAction;
                case 0xac: return NpcAssignment;
                default:   return DefaultHandler;
            }
        }

        protected ItemType ParseItem(List<byte> data)
        {
            ItemType item = new ItemType();
            item.packet = data.ToArray();

            //try
            {
                BitReader reader = new BitReader(item.packet);
                byte packet = (byte)reader.Read(8);
                item.action = (uint)reader.Read(8);
                item.category = (uint)reader.Read(8);
                byte validSize = (byte)reader.Read(8);
                item.id = (uint)reader.Read(32);

                if (packet == 0x9d)
                    reader.Read(40);

                item.equipped = reader.ReadBit();
                reader.ReadBit();
                reader.ReadBit();
                item.in_socket = reader.ReadBit();
		        item.identified = reader.ReadBit();
		        reader.ReadBit();
		        item.switched_in = reader.ReadBit();
		        item.switched_out = reader.ReadBit();

		        item.broken = reader.ReadBit();
		        reader.ReadBit();
		        item.potion = reader.ReadBit();
		        item.has_sockets = reader.ReadBit();
		        reader.ReadBit();
		        item.in_store = reader.ReadBit();
		        item.not_in_a_socket = reader.ReadBit();
		        reader.ReadBit();

		        item.ear = reader.ReadBit();
		        item.start_item = reader.ReadBit();
		        reader.ReadBit();
		        reader.ReadBit();
		        reader.ReadBit();
		        item.simple_item = reader.ReadBit();
		        item.ethereal = reader.ReadBit();
		        reader.ReadBit();

		        item.personalised = reader.ReadBit();
		        item.gambling = reader.ReadBit();
		        item.rune_word = reader.ReadBit();
		        reader.Read(5);

		        item.version = (ItemType.ItemVersionType)(reader.Read(8));

		        reader.Read(2);
		        byte destination = (byte)reader.Read(3);

        		item.ground = (destination == 0x03);

        		if(item.ground)
		        {
			        item.x = (UInt16)reader.Read(16);
			        item.y = (UInt16)reader.Read(16);
		        }
		        else
		        {
			        item.directory = (byte)reader.Read(4);
			        item.x = (byte)reader.Read(4);
			        item.y = (byte)reader.Read(3);
			        item.container = (ItemType.ItemContainerType)(reader.Read(4));
		        }

	        	item.unspecified_directory = false;

		        if(item.action == (uint)ItemType.item_action_type.add_to_shop || item.action == (uint)ItemType.item_action_type.remove_from_shop)
		        {
			        long container = (long)(item.container);
			        container |= 0x80;
			        if((container & 1) != 0)
			        {
				        container--; //remove first bit
				        item.y += 8;
			        }
			        item.container = (ItemType.ItemContainerType)(container);
		        }
		        else if(item.container == ItemType.ItemContainerType.unspecified)
		        {
		        	if(item.directory == (uint)ItemType.equipment_directory_type.not_applicable)
			        {
				        if(item.in_socket)
					    //y is ignored for this container type, x tells you the index
					        item.container = ItemType.ItemContainerType.item;
				        else if(item.action == (uint)ItemType.item_action_type.put_in_belt || item.action == (uint)ItemType.item_action_type.remove_from_belt)
				        {
					        item.container = ItemType.ItemContainerType.belt;
					        item.y = item.x / 4;
					        item.x %= 4;
				        }
			        }
			    else
				    item.unspecified_directory = true;
		        }

		        if(item.ear)
		        {
			        //item.ear_character_class =  (GameData.CharacterClassType)
                    reader.Read(3);
			        item.ear_level = (byte)reader.Read(7);
			        item.ear_name = "Fuck Off";
                    reader.Read(16*7);
			        return item;
		        }

		        byte[] code_bytes = new byte[4];
		        for(int i = 0; i < code_bytes.Length; i++)
			        code_bytes[i] = (byte)(reader.Read(8));
		        code_bytes[3] = 0;

		        item.type = System.Text.Encoding.ASCII.GetString(code_bytes).Substring(0,3);

		        ItemEntry entry;
		        if(!m_owner.m_dm.m_itemData.Get(item.type, out entry))
		        {
			        Console.WriteLine("Failed to look up item in item data table");
			        return item;
		        }

		        item.name = entry.Name;
		        item.width = (int)entry.Width;
		        item.height = (int)entry.Height;

		        item.is_gold = (item.type == "gld");

		        if(item.is_gold)
		        {
			        bool big_pile = reader.ReadBit();
			        if(big_pile)
				        item.amount = (uint)reader.Read(32);
			        else
				        item.amount = (uint)reader.Read(12);

			        return item;
		        }

		        item.used_sockets = (byte)reader.Read(3);

		        item.quality = ItemType.ItemQualityType.normal;

		        if(item.simple_item || item.gambling)
			        return item;

		        item.level = (byte)reader.Read(7);

		        item.quality = (ItemType.ItemQualityType)(reader.Read(4));


		        // EVERYTHING AFTER THIS NEEDS FIXING!
		        // Item mods will NOT be read/parsed in current version.
		        // Item code and quality is enough for a hacky beta pickit.
		        //return item;

        		item.has_graphic = reader.ReadBit();;
        		if(item.has_graphic)
			        item.graphic = (byte)reader.Read(3);

		        item.has_colour = reader.ReadBit();
		        if(item.has_colour)
			        item.colour = (UInt16)reader.Read(11);

		        if(item.identified)
		        {
			        switch(item.quality)
		        	{
			        case ItemType.ItemQualityType.inferior:
			        	item.prefix = (byte) reader.Read(3);
			        	break;
			        case ItemType.ItemQualityType.superior:
				        item.superiority = (ItemType.SuperiorItemClassType)(reader.Read(3));
				        break;
			        case ItemType.ItemQualityType.magical:
				        item.prefix = (uint)reader.Read(11);
				        item.suffix = (uint)reader.Read(11);
				        break;

			        case ItemType.ItemQualityType.crafted:
			        case ItemType.ItemQualityType.rare:
				        item.prefix = (uint)reader.Read(8) - 156;
				        item.suffix = (uint)reader.Read(8) - 1;
				        if(ClientlessBot.debugging)
				        {
                            /*
					        std::cout << "Rare prefix: " << item.prefix << std::endl;
					        std::cout << "Rare suffix: " << item.suffix << std::endl;
					        std::cout << get_char_array_string(data) << std::endl;
                             */
			        	}
				        break;

			        case ItemType.ItemQualityType.set:
				        item.set_code = (uint)reader.Read(12);
				        break;
			        case ItemType.ItemQualityType.unique:
				        if(item.type != "std") //standard of heroes exception?
					        item.unique_code = (uint)reader.Read(12);
				        break;
			    }
		    }

		    if(item.quality == ItemType.ItemQualityType.rare || item.quality == ItemType.ItemQualityType.crafted)
		    {
			    for(ulong i = 0; i < 3; i++)
			    {
				    if(reader.ReadBit())
			    		item.prefixes.Add((uint)reader.Read(11));
		    		if(reader.ReadBit())
	    				item.suffixes.Add((uint)reader.Read(11));
    			}
		    }

		    if(item.rune_word)
		    {
			    item.runeword_id = (uint)reader.Read(12);
			    item.runeword_parameter = (byte)reader.Read(4);
			    //std::cout << "runeword_id: " << item.runeword_id << ", parameter: " << item.runeword_parameter << std::endl;
    		}

		    if(item.personalised)
            {
			    item.personalised_name = "Fuck off";
                reader.Read(7*16);
            }
        

		    item.is_armor = entry.IsArmor();
		    item.is_weapon = entry.IsWeapon();

		    if(item.is_armor)
			    item.defense = (uint)reader.Read(11) - 10;

		    /*if(entry.throwable)
		    {
			    reader.Read(9);
			    reader.Read(17);
		    }
		    //special case: indestructible phase blade
		    else */
		    if(item.type == "7cr")
    			reader.Read(8);
		    else if(item.is_armor || item.is_weapon)
		    {
			    item.maximum_durability = (byte)reader.Read(8);
			    item.indestructible = (uint)((item.maximum_durability == 0) ? 1 : 0);
			    /*
			    if(!item.indestructible)
			    {
				    item.durability = reader.Read(8);
    				reader.ReadBit();
			    }
			    */

			    //D2Hackit always reads it, hmmm. Appears to work.

			    item.durability = (byte)reader.Read(8);
			    reader.ReadBit();
		    }

		    if(item.has_sockets)
    			item.sockets = (byte)reader.Read(4);

		    if(!item.identified)
    			return item;

		    if(entry.Stackable)
		    {
			    if(entry.Usable)
				    reader.Read(5);

			    item.amount = (uint)reader.Read(9);
		    }
            uint set_mods = 0;
    		if(item.quality == ItemType.ItemQualityType.set)
	    		set_mods = (byte)reader.Read(5);

    		//reader.debugging = debugging;
            return item;
        		while(true)
		        {
			        //if(debugging)
			        //	std::cout << "Reading stat ID" << std::endl;

			        uint stat_id = (uint)reader.Read(9);

			        if(stat_id == 0x1ff)
			        {
				        //if(debugging)
				        //	std::cout << "Stat terminator encountered" << std::endl;
				        break;
			        }

        			ItemType.ItemPropertyType item_property;


			        //process_item_stat(stat_id, reader, item_property);
			        //item.properties.Add(item_property);
		        }
		        //std::cout << pretty_item_stats(item) << std::endl;
	        }
	        //catch(Exception e)
        	{

        	//	Console.WriteLine("Error occured: ");
		
	        }
	        return item;
            
        }

        protected void ItemAction(byte type, List<byte> data)
        {
            ItemType item = ParseItem(data);

            if(item.ground)
                m_owner.BotGameData.Items.Add(item.id, item);

            if (!item.ground && !item.unspecified_directory)
            {
                switch (item.container)
                {
                    case ItemType.ItemContainerType.inventory:
                        m_owner.BotGameData.Inventory.Add(item);
                        break;
                    case ItemType.ItemContainerType.cube:
                        m_owner.BotGameData.Cube.Add(item);
                        break;
                    case ItemType.ItemContainerType.stash:
                        m_owner.BotGameData.Stash.Add(item);
                        break;
                    case ItemType.ItemContainerType.belt:
                        m_owner.BotGameData.Belt.Add(item);
                        break;
                }
            }
        }

        protected void NpcAssignment(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            NpcEntity output;
            try
            {
                BitReader br = new BitReader(data.ToArray());
                br.ReadBitsLittleEndian(8);
                UInt32 id = (uint)br.ReadBitsLittleEndian(32);
                UInt16 npctype = (ushort)br.ReadBitsLittleEndian(16);
                UInt16 x = (ushort)br.ReadBitsLittleEndian(16);
                UInt16 y = (ushort)br.ReadBitsLittleEndian(16);
                byte life = (byte)br.ReadBitsLittleEndian(8);
                byte size = (byte)br.ReadBitsLittleEndian(8);

                output = new NpcEntity(id, npctype, life, x, y);

                Console.WriteLine("NPC id: {3}, Type: {0:X}, Life: {1:X}, Size: {2:X}", npctype, life, data.Count, id);

                int informationLength = 16;

                String[] entries;

                if (!m_owner.m_dm.m_monsterFields.Get(type, out entries))
                    Console.WriteLine("Failed to read monstats data for NPC of type {0}", type);
                if(entries.Length != informationLength)
                    Console.WriteLine("Invalid monstats entry for NPC of type {0}", type);

                bool lookupName = false;

                if (data.Count > 0x10)
                {
                    br.ReadBitsLittleEndian(4);
                    if (br.ReadBit())
                    {
                        for (int i = 0; i < informationLength; i++)
                        {
                            int bitCount;
                            int value = Int32.Parse(entries[i]);
                            if (value > 2)
                            {
                                bitCount = (int)Math.Ceiling(Math.Log((double)value) / Math.Log(2.0));
                            }
                            else
                                bitCount = 1;

                            int bits = br.ReadBitsLittleEndian(bitCount);
                        }
                    }

                    output.SuperUnique = false;

                    if (br.ReadBit())
                    {
                        output.HasFlags = true;
                        output.Flag1 = br.ReadBit();
                        output.Flag2 = br.ReadBit();
                        output.SuperUnique = br.ReadBit();
                        output.IsMinion = br.ReadBit();
                        output.Flag5 = br.ReadBit();
                    }

                    if (output.SuperUnique)
                    {
                        output.SuperUniqueId = br.ReadBitsLittleEndian(16);
                        String name;
                        if (!m_owner.m_dm.m_superUniques.Get(output.SuperUniqueId, out name))
                        {
                            Console.WriteLine("Failed to lookup super unique monster name for {0}", output.SuperUniqueId);
                            output.Name = "invalid";
                        }
                        else
                        {
                            output.Name = name;
                        }
                    }
                    else
                        lookupName = true;

                    if (data.Count > 17 && lookupName != true && output.Name != "invalid")
                    {
                        output.IsLightning = false;
                        while (true)
                        {
                            byte mod = (byte)br.ReadBitsLittleEndian(8);
                            if (mod == 0)
                                break;
                            if (mod == 0x11)
                                output.IsLightning = true;
                        }
                    }
                }
                else
                    lookupName = true;

                if (lookupName)
                {
                    String name;
                    if (!m_owner.m_dm.m_monsterNames.Get((int)output.Type, out name))
                        Console.WriteLine("Failed to Look up monster name for {0}", output.Type);
                    else
                        output.Name = name;
                }

                m_owner.BotGameData.Npcs.Add(id, output);
            }
            catch
            {

            }
            
        }

        protected void WeaponSetSwitched(byte type, List<byte> data)
        {
            if (m_owner.BotGameData.WeaponSet == 0)
                m_owner.BotGameData.WeaponSet = 1;
            else
                m_owner.BotGameData.WeaponSet = 0;
        }

        protected void LifeManaPacket(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            if (BitConverter.ToUInt16(packet, 6) == 0x0000)
                return;

            UInt32 plife = (uint)BitConverter.ToUInt16(packet, 1) & 0x7FFF;
            if (m_owner.BotGameData.CurrentLife == 0)
                m_owner.BotGameData.CurrentLife = plife;

            if (plife < m_owner.BotGameData.CurrentLife && plife > 0)
            {
                UInt32 damage = m_owner.BotGameData.CurrentLife - plife;
                Console.WriteLine("{0}: [D2GS] {1} damage was dealt to {2} ({3} left)", m_owner.Account, damage, m_owner.Character, plife);
                if (plife <= m_owner.ChickenLife)
                {
                    Console.WriteLine("{0}: [D2GS] Chickening with {1} left!", m_owner.Account, plife);
                    m_owner.LeaveGame();
                }
                else if (plife <= m_owner.PotLife)
                {
                    Console.WriteLine("{0}: [D2GS] Attempting to use potion with {1} life left.", m_owner.Account, plife);
                    m_owner.UsePotion();
                }
            }

            m_owner.BotGameData.CurrentLife = plife;
        }

        protected void Pong(byte type, List<byte> data)
        {
            m_owner.BotGameData.LastTimestamp = System.Environment.TickCount;
        }

        protected void PortalUpdate(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray(); 
            int offset = 5;
            String name = BitConverter.ToString(packet, offset, 15);
            if (name.Substring(0, 8) == m_owner.BotGameData.Me.Name.Substring(0, 8))
            {
                Console.WriteLine("{0}: [D2GS] Received new portal id", m_owner.Account);
                m_owner.BotGameData.Me.PortalId = BitConverter.ToUInt32(packet, 21);
            }
        }

        protected void MercUpdate(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 4);
            UInt32 mercId = BitConverter.ToUInt32(packet, 8);
            Player currentPlayer = m_owner.BotGameData.GetPlayer(id);
            currentPlayer.HasMecenary = true;
            currentPlayer.MercenaryId = mercId;
            if (id == m_owner.BotGameData.Me.Id)
                m_owner.BotGameData.HasMerc = true;
        }

        protected void NpcStoppedMoving(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 1);
            UInt16 x = BitConverter.ToUInt16(packet,5);
            UInt16 y = BitConverter.ToUInt16(packet, 7);
            byte life = packet[9];

            if (!m_owner.BotGameData.Npcs.ContainsKey(id))
            {
                Console.WriteLine("{0}: [D2GS] Npc ({1}) not found in map... Adding with type = 0", m_owner.Account, id);
                m_owner.BotGameData.Npcs.Add(id, new NpcEntity(id, 0, life, x, y));
            }
            m_owner.BotGameData.Npcs[id].Moving = false;
            m_owner.BotGameData.Npcs[id].Location = new Coordinate(x, y);
            m_owner.BotGameData.Npcs[id].Life = life;
        }

        protected void NpcStateUpdate(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 1);
            byte state = packet[5];

            if (!m_owner.BotGameData.Npcs.ContainsKey(id))
            {
                Console.WriteLine("{0}: [D2GS] Npc ({1}) not found in map... Adding with type = 0", m_owner.Account, id);
                m_owner.BotGameData.Npcs.Add(id, new NpcEntity(id, 0, 0, 0, 0));
            }
            if (state == 0x09 || state == 0x08)
                m_owner.BotGameData.Npcs[id].Life = 0;
            else
                m_owner.BotGameData.Npcs[id].Life = packet[10];

            m_owner.BotGameData.Npcs[id].Location.X = BitConverter.ToUInt16(packet,6);
            m_owner.BotGameData.Npcs[id].Location.Y = BitConverter.ToUInt16(packet, 8);
        }

        protected void NpcMoveEntity(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 1);
            byte movementType = packet[5];
            Int32 x = BitConverter.ToUInt16(packet, 6);
            Int32 y = BitConverter.ToUInt16(packet, 8);
            bool running;
            if (movementType == 0x18)
                running = true;
            else if (movementType == 0x00)
                running = false;
            else
                return;

            if (!m_owner.BotGameData.Npcs.ContainsKey(id))
            {
                Console.WriteLine("{0}: [D2GS] Npc ({1}) not found in map... Adding with type = 0", m_owner.Account, id);
                m_owner.BotGameData.Npcs.Add(id, new NpcEntity(id, 0, 0, x, y));
            }
                m_owner.BotGameData.Npcs[id].Moving = true;
                m_owner.BotGameData.Npcs[id].Running = running;
                m_owner.BotGameData.Npcs[id].TargetLocation = new Coordinate(x, y);
        }


        protected void NpcMovement(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 1);
            byte movementType = packet[5];
            Int32 x = BitConverter.ToUInt16(packet, 6);
            Int32 y = BitConverter.ToUInt16(packet, 8);
            bool running;
            if (movementType == 0x17)
                running = true;
            else if (movementType == 0x01)
                running = false;
            else
                return;

            if (!m_owner.BotGameData.Npcs.ContainsKey(id))
            {
                Console.WriteLine("{0}: [D2GS] Npc ({1}) not found in map... Adding with type = 0", m_owner.Account, id);
                m_owner.BotGameData.Npcs.Add(id, new NpcEntity(id, 0, 0, x, y));
            }
            m_owner.BotGameData.Npcs[id].Moving = true;
            m_owner.BotGameData.Npcs[id].Running = running;
            m_owner.BotGameData.Npcs[id].TargetLocation = new Coordinate(x, y);
            
        }

        protected void InitializePlayer(byte type, List<byte> data)
        {
            if (!m_owner.BotGameData.Me.Initialized)
            {
                byte[] packet = data.ToArray();
                UInt32 id = BitConverter.ToUInt32(packet, 1);
                GameData.CharacterClassType charClass = (GameData.CharacterClassType)data[5];
                UInt32 x = BitConverter.ToUInt16(packet,22);
                UInt32 y = BitConverter.ToUInt16(packet, 24);
                Player newPlayer = new Player(m_owner.Character, id, charClass, m_owner.CharacterLevel,(int)x,(int)y);
                m_owner.BotGameData.Me = newPlayer;
            }
        }

        protected void PlayerJoins(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 3);
            if (id != m_owner.BotGameData.Me.Id)
            {
                String name = BitConverter.ToString(packet, 8, 15);
                GameData.CharacterClassType charClass = (GameData.CharacterClassType)data[7];
                UInt32 level = BitConverter.ToUInt16(packet, 24);
                Player newPlayer = new Player(name, id, charClass, level);
                m_owner.BotGameData.Players.Add(id, newPlayer);
            }
        }

        protected void PlayerLeaves(byte type, List<byte> data)
        {
            UInt32 id = BitConverter.ToUInt32(data.ToArray(), 1);
            m_owner.BotGameData.Players.Remove(id);
        }
      
        protected void NpcInteraction(byte type, List<byte> data)
        {
            if (m_owner.BotGameData.FirstNpcInfoPacket)
                m_owner.BotGameData.FirstNpcInfoPacket = false;
            else
            {
                Console.WriteLine("{0}: [D2GS] Talking to an NPC.", m_owner.Account);
                m_owner.BotGameData.TalkedToNpc = true;
                UInt32 id = BitConverter.ToUInt32(data.ToArray(), 2);
                Write(BuildPacket(0x2f, one, BitConverter.GetBytes(id)));
            }
        }

        protected void ChatMessage(byte type, List<byte> data)
        {
        }

        protected void ItemSkillBonus(byte type, List<byte> data)
        {
            UInt32 skill,amount;
            skill = BitConverter.ToUInt16(data.ToArray(), 7);
            if (type == 0x21)
                amount = data[10];
            else
                amount = data[9];

            //Console.WriteLine("Setting Skill: {0} bonus to {1}", skill, amount);
            m_owner.BotGameData.ItemSkillLevels[(Skills.Type)skill] = amount;
        }

        protected void SetPlayerLevel(byte type, List<byte> data)
        {
            if (data[1] == 0x0c)
            {
                m_owner.BotGameData.Me.Level = data[2];
                //Console.WriteLine("Setting Player Level: {0}", data[2]);
            }
        }

        protected void ProcessExperience(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 exp = 0;
            if (type == 0x1a)
                exp = data[1];
            else if (type == 0x1b)
                exp = BitConverter.ToUInt16(packet, 1);
            else if (type == 0x1c)
                exp = BitConverter.ToUInt32(packet, 1);
            m_owner.BotGameData.Experience += exp;
        }

        protected void PlayerReassign(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 id = BitConverter.ToUInt32(packet, 2);
            Player current_player = m_owner.BotGameData.GetPlayer(id);
            current_player.Location = new Coordinate(BitConverter.ToUInt16(packet, 6), BitConverter.ToUInt16(packet, 8));
        }

        protected void PlayerUpdate(byte type, List<byte> data)
        {
            byte[] packet = data.ToArray();
            UInt32 playerId = BitConverter.ToUInt32(packet, 2);
            Player current_player = m_owner.BotGameData.GetPlayer(playerId);
            current_player.Location = new Coordinate(BitConverter.ToUInt16(packet,7),BitConverter.ToUInt16(packet,9));
            current_player.DirectoryKnown = true;
        }

        protected void NpcUpdate(byte type, List<byte> data)
        {
            UInt32 id = BitConverter.ToUInt32(data.ToArray(), 2);
            m_owner.BotGameData.Npcs[id].Life = data[8];
        }

        protected void LoadActData(byte type, List<byte> data)
        {
            Console.WriteLine("{0}: [D2GS] Loading Act Data", m_owner.Account);
            m_owner.BotGameData.CurrentAct = (GameData.ActType)data[1];
            m_owner.BotGameData.MapId = BitConverter.ToInt32(data.ToArray(), 2);
            m_owner.BotGameData.AreaId = BitConverter.ToInt32(data.ToArray(), 6);
            if (!m_owner.BotGameData.FullyEnteredGame)
            {
                m_owner.BotGameData.FullyEnteredGame = true;
                Console.WriteLine("{0}: [D2GS] Fully Entered Game.", m_owner.Account);
            }
        }

        protected void StartPingThread(byte type, List<byte> data)
        {
            //if (ClientlessBot.debugging)
            Console.WriteLine("{0}: [D2GS] Game is done loading. Joining Game", m_owner.Account);
            m_stream.WriteByte(0x6b);
            //if (ClientlessBot.debugging)
                Console.WriteLine("{0}: [D2GS] Starting Ping Thread", m_owner.Account);

            PingStart();
        }
   
        protected void GameFlagsPing(byte type, List<byte> data)
        {
            //if (ClientlessBot.debugging)
                 Console.WriteLine("{0}: [D2GS] Game is loading, please wait...", m_owner.Account);
            List<byte> packet = new List<byte>();
            packet.Add(0x6d);
            packet.AddRange(BitConverter.GetBytes((uint)System.Environment.TickCount));
            packet.AddRange(nulls);
            packet.AddRange(nulls);
            byte[] bytePacket = packet.ToArray();
            Write(bytePacket);
        }

        protected void GameLoading(byte type, List<byte> data)
        {
            //if (ClientlessBot.debugging)
                Console.WriteLine("{0}: [D2GS] Game is loading, please wait...", m_owner.Account);
        }

        public override void ThreadFunction()
        {

            //Initialize the game's data
            m_owner.InitializeGameData();

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
            Int32 bytesRead = 0;
            while (true)
            {
                try
                {
                    /*
                    while (m_stream.DataAvailable)
                    {
                        buffer.Add((byte)m_stream.ReadByte());
                        if(!m_stream.DataAvailable)
                            Console.WriteLine("{0}: [D2GS] Finished Reading to Buffer...", m_owner.Account);
                    }
                    */
                    if (m_stream.DataAvailable)
                    {
                        bytesRead = m_stream.Read(byteBuffer, 0, byteBuffer.Length);
                        buffer.AddRange(new List<byte>(byteBuffer).GetRange(0, bytesRead));
                    }

                }
                catch
                {
                    //if (ClientlessBot.debugging)
                    Console.WriteLine("{0}: [D2GS] Disconnected from game server", m_owner.Account);
                    if (m_owner.ConnectedToGs)
                    {
                        m_owner.ConnectedToGs = false;
                        if (m_pingThread.IsAlive)
                            m_pingThread.Join();
                        // Join threads
                    }
                    m_owner.Status = ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME;
                    break;
                }
                

                while (true)
                {
                    UInt16 receivedPacket = 0;
                    if(buffer.Count >= 2)
                        receivedPacket = BitConverter.ToUInt16(buffer.ToArray(),0);
                    if (buffer.Count >= 2 && receivedPacket == (UInt16)0x01af)
                    {
                        //if (ClientlessBot.debugging)
                            Console.WriteLine("{0}: [D2GS] Logging on to game server", m_owner.Account);

                        byte[] temp = {0x50, 0xcc, 0x5d, 0xed, 
                                       0xb6, 0x19, 0xa5, 0x91};

                        Int32 pad = 16 - m_owner.Character.Length;

                        byte[] padding = new byte[pad];
                        byte[] characterClass = { m_owner.ClassByte };
                        byte[] joinpacket = BuildPacket(0x68, m_owner.GsHash, m_owner.GsToken, characterClass, BitConverter.GetBytes((UInt32)0xd), temp, zero, System.Text.Encoding.ASCII.GetBytes(m_owner.Character), padding);
                        Write(joinpacket);
                        Console.WriteLine("{0}: [D2GS] Join packet sent to server", m_owner.Account);
                        buffer.RemoveRange(0, 2);
                    }

                    if (buffer.Count < 2 || (buffer[0] >= 0xF0 && buffer.Count < 3))
                    {
                        break;
                    }

                    Int32 headerSize;
                    Int32 length = Huffman.GetPacketSize(buffer, out headerSize);
                    if (length > buffer.Count)
                        break;

                    byte[] compressedPacket = buffer.GetRange(headerSize, length).ToArray();
                    buffer.RemoveRange(0, length+headerSize);


                    byte[] decompressedPacket;
                    Huffman.Decompress(compressedPacket,out decompressedPacket);
                    List<byte> packet = new List<byte>(decompressedPacket);
                    while (packet.Count != 0)
                    {
                        Int32 packetSize;
                        if(!GetPacketSize(packet,out packetSize))
                        {
                            Console.WriteLine("{0}: [D2GS] Failed to determine packet length",m_owner.Account);
                            break;
                        }
                        List<byte> actualPacket = new List<byte>(packet.GetRange(0,packetSize));
                        packet.RemoveRange(0, packetSize);

                        byte identifier = actualPacket[0];
                        DispatchPacket(identifier)(identifier, actualPacket);
                        m_owner.ReceivedGameServerPacket(actualPacket);
                    }
                }
            }
        }

        // This was taken from Redvex according to qqbot source
        bool GetPacketSize(List<byte> input, out Int32 output)
        {
	        byte identifier = input[0];

	        Int32 size = input.Count;

	        switch(identifier)
	        {
	        case 0x26:
                    if (GetChatPacketSize(input,out output))
			        return true;
		        break;
	        case 0x5b:
		        if(size >= 3)
		        {
			        output = BitConverter.ToInt16(input.ToArray(), 1);
			        return true;
		        }
		        break;
	        case 0x94:
		        if(size >= 2)
		        {
			        output = input[1] * 3 + 6;
			        return true;
		        }
		        break;
	        case 0xa8:
	        case 0xaa:
		        if(size >= 7)
		        {
			        output = (byte)input[6];
			        return true;
		        }
		        break;
	        case 0xac:
		        if(size >= 13)
		        {
			        output = (byte)input[12];
			        return true;
		        }
		        break;
	        case 0xae:
		        if(size >= 3)
		        {
			        output = 3 + BitConverter.ToInt16(input.ToArray(), 1);
			        return true;
		        }
		        break;
	        case 0x9c:
		        if(size >= 3)
		        {
			        output = input[2];
			        return true;
		        }
		        break;
	        case 0x9d:
		        if(size >= 3)
		        {
			        output = input[2];
			        return true;
		        }
		        break;
	        default:
		        if(identifier < packetSizes.Length)
		        {
			        output = packetSizes[identifier];
			        return output != 0;
		        }
		        break;
	        }
            output = 0;
	        return false;
        }

        bool GetChatPacketSize(List<byte> input, out Int32 output)
        {
            output = 0;
	        if(input.Count < 12)
		        return false;

	        const Int32 initial_offset = 10;

            Int32 name_offset = input.IndexOf(0,initial_offset);

	        if(name_offset == -1)
		        return false;

            name_offset -= initial_offset;

	        Int32 message_offset = input.IndexOf(0,initial_offset + name_offset + 1);

	        if(message_offset == -1)
		        return false;

            message_offset = message_offset - initial_offset - name_offset -1;

	        output = initial_offset + name_offset + 1 + message_offset + 1;

	        return true;
        }

    }
}
