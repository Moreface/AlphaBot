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
using System.Collections;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Xml.Serialization;

namespace CSharpClient
{
    class ClientlessBot : IDisposable
    {
        public static bool debugging = false;

        public enum GameDifficulty
        {
            NORMAL = 0,
            NIGHTMARE = 1,
            HELL = 2
        }

        public enum ClientStatus
        {
            STATUS_IDLE,
            STATUS_REALM_DOWN,
            STATUS_INVALID_CD_KEY,
            STATUS_INVALID_EXP_CD_KEY,
            STATUS_BANNED_CD_KEY,
            STATUS_BANNED_EXP_CD_KEY,
            STATUS_KEY_IN_USE,
            STATUS_EXP_KEY_IN_USE,
            STATUS_ON_MCP,
            STATUS_ACTIVE,
            STATUS_TERMINATING,
            STATUS_IN_TOWN,
            STATUS_KILLING_PINDLESKIN,
            STATUS_KILLING_ELDRITCH,
            STATUS_NOT_IN_GAME
        };

        private Object m_itemListLock;
        public Object ItemListLock { get { return m_itemListLock; } set { m_itemListLock = value; } }

        protected GameData m_gameData;
        public GameData BotGameData { get { return m_gameData; } } 

        protected String m_gameName;
        public String GameName { get { return m_gameName; } set { m_gameName = value; } }
        
        protected String m_gamePassword;
        public String GamePassword { get { return m_gamePassword; } set { m_gamePassword = value; } }
        
        protected IPAddress m_gsIp;
        public IPAddress GsIp { get { return m_gsIp; } set { m_gsIp = value; } }

        protected List<byte> m_gsHash;
        public List<byte> GsHash { get { return m_gsHash; } set { m_gsHash = value; } }

        protected List<byte> m_gsToken;
        public List<byte> GsToken { get { return m_gsToken; } set { m_gsToken = value; } }

        protected UInt16 m_mcpPort;
        public UInt16 McpPort { get { return m_mcpPort; } set { m_mcpPort = value; } }
        
        protected IPAddress m_mcpIp;
        public IPAddress McpIp { get { return m_mcpIp; } set { m_mcpIp= value; } }

        protected List<byte> m_mcpData;
        public List<byte> McpData { get { return m_mcpData; } set { m_mcpData = value; } }


        protected Boolean m_loggedin;
        public Boolean LoggedIn { get { return m_loggedin; } set { m_loggedin = value; } }
        
        // Game difficulty
        protected GameDifficulty m_difficulty;
        public GameDifficulty Difficulty { get { return m_difficulty; } set { m_difficulty = value; } }
        
        // Account Name, Password, and Character Name
        protected String m_account, m_password, m_character;
        public String Account { get { return m_account; } set { m_account = value; } }
        public String Password { get { return m_password; } set { m_password = value; } }
        public String Character { get { return m_character; } set { m_character = value; } }

        // CD-Key Values
        protected String m_classicKey, m_expansionKey;
        public String ClassicKey { get { return m_classicKey; } set { m_classicKey = value; } }
        public String ExpansionKey { get { return m_expansionKey; } set { value = m_expansionKey; } }

        // Chicken and Potion Values
        protected UInt32 m_chickenLife, m_potLife;
        public UInt32 ChickenLife { get { return m_chickenLife; } set { m_chickenLife = value; } }
        public UInt32 PotLife { get { return m_potLife; } set { m_potLife = value; } }
        // Server information
        protected String m_battleNetServer, m_realm;
        public String BattleNetServer { get { return m_battleNetServer; } set { m_battleNetServer = value; } }
        public String Realm { get { return m_realm; } set { m_realm = value; } }

        protected String m_gameExeInformation;
        public String GameExeInformation { get { return m_gameExeInformation; } set { m_gameExeInformation = value; } }
        
        protected String m_keyOwner;
        public String KeyOwner { get { return m_keyOwner; } set { m_keyOwner = value; } }

        protected String m_binaryDirectory;
        public String BinaryDirectory { get { return m_binaryDirectory; } set { m_binaryDirectory = value; } }
        
        protected ClientStatus m_status;
        public ClientStatus Status { get { return m_status; } set { m_status = value; } }

        protected UInt32 m_serverToken;
        public UInt32 ServerToken { get { return m_serverToken; } set { m_serverToken = value; } }

        protected UInt16 m_gameRequestId;
        public UInt16 GameRequestId { get { return m_gameRequestId; } set { m_gameRequestId = value; } }
        
        Boolean m_inGame;
        public Boolean InGame { get { return m_inGame; } set { m_inGame = value; } }

        protected Boolean m_firstGame;
        public Boolean FirstGame { get { return m_firstGame; } set { m_firstGame = value; } }

        protected Boolean m_failedGame;
        public Boolean FailedGame { get { return m_failedGame; } set { m_failedGame = value; } }

        protected Boolean m_connectedToGs;
        public Boolean ConnectedToGs { get { return m_connectedToGs; } set { m_connectedToGs = value; } }

        private Byte m_classByte;
        public Byte ClassByte { get { return m_classByte; } set { m_classByte = value; } }

        private UInt32 m_characterLevel;
        public UInt32 CharacterLevel { get { return m_characterLevel; } set { m_characterLevel = value; } }
        
        public BattleNetCS m_bncs;
        public RealmServer m_mcp;
        public GameServer m_gs;
        public DataManager m_dm;
        protected Thread m_bncsThread;
        protected Thread m_mcpThread;
        protected Thread m_gameCreationThread;
        protected Thread m_gsThread;

        public Player Me { get { return BotGameData.Me; } }

        public void DetermineCharacterSkillSetup()
        {
            if (Me.Class == GameData.CharacterClassType.SORCERESS) {
		        if (BotGameData.SkillLevels[Skills.Type.blizzard] >= 15 && BotGameData.SkillLevels[Skills.Type.glacial_spike] >= 8 && BotGameData.SkillLevels[Skills.Type.ice_blast] >= 8) {
			        Console.WriteLine("{0}: [D2GS] Using Blizzard/Glacial Spike/Ice Blast Sorceress setup.",Account);
			        BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.SORCERESS_BLIZZARD;
		        } else if (BotGameData.SkillLevels[Skills.Type.meteor] >= 15 && BotGameData.SkillLevels[Skills.Type.fire_ball] >= 15 && BotGameData.SkillLevels[Skills.Type.frozen_orb] == 0) {
			        Console.WriteLine("{0}: [D2GS] Using Meteor/Fireball Sorceress setup.",Account);
			        BotGameData.CharacterSkillSetup =GameData.CharacterSkillSetupType.SORCERESS_METEOR;
		        } else if (BotGameData.SkillLevels[Skills.Type.lightning] >= 15 || BotGameData.SkillLevels[Skills.Type.chain_lightning] >= 15) {
			        Console.WriteLine("{0}: [D2GS] Using Lightning/Chain Lightning Sorceress setup" ,Account);
			        BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.SORCERESS_LIGHTNING;
		        } else if (BotGameData.SkillLevels[Skills.Type.fire_ball] >= 15 && BotGameData.SkillLevels[Skills.Type.frozen_orb] >= 15) {
			        Console.WriteLine("{0}: [D2GS] Using Meteorb Sorceress setup",Account);
			        BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.SORCERESS_METEORB;
		        } else {
			        Console.WriteLine("Unknown Sorceress skill setup");
			        BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.UNKNOWNSETUP;
		        }
	        } else if (Me.Class == GameData.CharacterClassType.PALADIN) {
                if (BotGameData.SkillLevels[Skills.Type.blessed_hammer] >= 15 && BotGameData.SkillLevels[Skills.Type.concentration] >= 15) {
                    Console.WriteLine("{0}: [D2GS] Using Hammerdin Paladin setup.",Account);
                    BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.PALADIN_HAMMERDIN;
                } else if (BotGameData.SkillLevels[Skills.Type.smite] >= 15 && BotGameData.SkillLevels[Skills.Type.fanaticism] >= 15) {
                    Console.WriteLine("{0}: [D2GS] Using Smiter Paladin setup." ,Account);
                    BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.PALADIN_SMITER;
                } else if (BotGameData.SkillLevels[Skills.Type.vengeance] >= 15 && BotGameData.SkillLevels[Skills.Type.conviction] >= 15) {
                    Console.WriteLine("{0}: [D2GS] Using Adam's Noob Smiter Paladin setup." ,Account);
                    BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.PALADIN_SMITER;
                }else {
                    Console.WriteLine("{0}: [D2GS] Unknown Paladin skill setup." ,Account);
                    BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.UNKNOWNSETUP;
                }
	        } else {
		        Console.WriteLine("No configuration available for this character class");
		        BotGameData.CharacterSkillSetup = GameData.CharacterSkillSetupType.UNKNOWNSETUP;
	        }
        }

        protected static List<ItemType> m_pickitList = new List<ItemType>();

        protected static void InitializePickit()
        {
            FileStream fs = new FileStream("pickit.xml", FileMode.Open);
            XmlSerializer x = new XmlSerializer(typeof(List<ItemType>));
            m_pickitList = (List<ItemType>)x.Deserialize(fs);

            foreach (ItemType i in m_pickitList)
            {
                Console.WriteLine("{0}: {1}, {2}, Ethereal:{3}, {4}", i.name, i.type, i.quality, i.ethereal, i.sockets);
            }
        }

        /*
         * 
         * In Game API
         * 
         * 
         */

        public void Start()
        {
            m_bncsThread.Start();
        }

        public int Time()
        {
            return System.Environment.TickCount / 1000;
        }

        public void SendPacket(byte command, params IEnumerable<byte>[] args)
        {
            m_gs.Write(m_gs.BuildPacket(command, args));
        }

        public virtual void ReceivedGameServerPacket(List<byte> data)
        {

        }

        public void CreateGameThreadFunction()
        {
            while (true)
            {
                //Replace this with mutex  or semaphore
                while (Status != ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME)
                    System.Threading.Thread.Sleep(1000);

                System.Threading.Thread.Sleep(30000);
                if (FirstGame)
                    System.Threading.Thread.Sleep(30000);
                if (Status == ClientlessBot.ClientStatus.STATUS_NOT_IN_GAME)
                {
                    MakeGame();
                }
                System.Threading.Thread.Sleep(5000);
            }
        }

        public virtual void BotThreadFunction()
        {

        }

        // MCP Functions
        public void JoinGame()
        {
            m_mcp.Write(m_mcp.BuildPacket(0x04, BitConverter.GetBytes(GameRequestId), System.Text.Encoding.ASCII.GetBytes(GameName), GenericServerConnection.zero, System.Text.Encoding.ASCII.GetBytes(GamePassword), GenericServerConnection.zero));
        }

        public void MakeGame()
        {
            if (Password.Length == 0)
                Password = "xa1";

            GameName = Utils.RandomString(10);
            if (FailedGame)
            {
                Console.WriteLine("{0}: [BNCS] Last game failed, sleeping.", Account);
                //debug_log.write("[" + nil::timestamp() + "] Last game failed, sleeping.\n");
                System.Threading.Thread.Sleep(30000);
            }

            // We assume the game fails every game, until it proves otherwise at end of botthread.
            FailedGame = true;

            Console.WriteLine("{0}: [MCP] Creating game \"{1}\" with password \"{2}\"", Account, GameName, GamePassword);
            //debug_log.write("[" + nil::timestamp() + "] Creating game \"" + game_name + "\" with password \"" + game_password + "\"\n");

            byte[] temp = { 0x01, 0xff, 0x08 };
            byte[] packet = m_mcp.BuildPacket(0x03, BitConverter.GetBytes((UInt16)GameRequestId), BitConverter.GetBytes(Utils.GetDifficulty(Difficulty)), temp, System.Text.Encoding.ASCII.GetBytes(GameName), GenericServerConnection.zero,
                            System.Text.Encoding.ASCII.GetBytes(GamePassword), GenericServerConnection.zero, GenericServerConnection.zero);

            m_mcp.Write(packet);
            GameRequestId++;
        }

        // D2GS Functions
        public bool Attack(UInt32 id)
        {
            if (!ConnectedToGs)
                return false;
            switch (BotGameData.CharacterSkillSetup)
            {
                case GameData.CharacterSkillSetupType.SORCERESS_LIGHTNING:
                    if(BotGameData.RightSkill != (uint)Skills.Type.chain_lightning)
                        SwitchSkill((uint)Skills.Type.chain_lightning);
                    Thread.Sleep(300);
                    CastOnObject(id);
                    break;
                case GameData.CharacterSkillSetupType.SORCERESS_BLIZZARD:
                    if (BotGameData.RightSkill != (uint)Skills.Type.blizzard)
                        SwitchSkill((uint)Skills.Type.blizzard);
                    Thread.Sleep(300);
                    CastOnObject(id);
                    break;
                case GameData.CharacterSkillSetupType.SORCERESS_METEOR:
                    break;
                case GameData.CharacterSkillSetupType.SORCERESS_METEORB:
                    break;
            }
            return true;
        }

        public virtual void Pickit()
        {
            var picking_items = (from i in BotGameData.Items
                                 where i.Value.ground select i.Value);

            var intr = picking_items.Intersect(m_pickitList,new ItemEntryComparer());
            foreach (var i in intr)
            {
                Console.WriteLine("{0}: {1}, {2}, Ethereal:{3}, {4}", i.name, i.type, i.quality, i.ethereal, i.sockets);
                if (BotGameData.Belt.m_items.Count >= 16 && i.type == "rv1")
                    continue;

                SwitchSkill((uint)Skills.Type.teleport);
                Thread.Sleep(200);
                CastOnCoord((ushort)i.x, (ushort)i.y);
                Thread.Sleep(400);
                SendPacket(0x16, new byte[] { 0x04, 0x00, 0x00, 0x00 }, BitConverter.GetBytes(i.id), GenericServerConnection.nulls);
                Thread.Sleep(500);
            }

        }

        public void CastOnCoord(UInt16 x, UInt16 y)
        {
            SendPacket(0x0c, BitConverter.GetBytes(x), BitConverter.GetBytes(y));
            Thread.Sleep(200);
        }

        public void CastOnObject(uint id)
        {
            SendPacket(0x0d,GenericServerConnection.one, BitConverter.GetBytes(id));
            Thread.Sleep(200);
        }

        public void CastOnSelf()
        {
            SendPacket(0x0c, BitConverter.GetBytes((UInt16)BotGameData.Me.Location.X), BitConverter.GetBytes((UInt16)BotGameData.Me.Location.Y));
            Thread.Sleep(200);
        }

        public bool GetAliveNpc(String name, double range, out NpcEntity output)
        {
            var n = (from npc in BotGameData.Npcs
                     where npc.Value.Name == name
                     && npc.Value.Life > 0
                     && (range == 0 || range > Me.Location.Distance(npc.Value.Location))
                     select npc).FirstOrDefault();
            if (n.Value == null)
            {
                output = default(NpcEntity);
                return false;
            }
            output = n.Value;
            return true;
        }

        public NpcEntity GetNpc(String name)
        {
            NpcEntity npc = (from n in BotGameData.Npcs
                             where n.Value.Name == name
                             select n).FirstOrDefault().Value;
            return npc;
        }

        public UInt32 GetSkillLevel(Skills.Type skill)
        {
            return BotGameData.SkillLevels[skill] + BotGameData.ItemSkillLevels[skill];
        }

        public void LeaveGame()
        {
            InGame = false;
            ConnectedToGs = false;

            Console.WriteLine("{0}: [D2GS] Leaving the game.", Account);
            SendPacket(0x69);

            Thread.Sleep(500);

            m_gs.Kill();
            
            if (m_gs.m_pingThread.IsAlive)
                m_gs.m_pingThread.Join();
            if (m_gsThread.IsAlive)
                m_gsThread.Join();

            Status = ClientStatus.STATUS_NOT_IN_GAME;
        }

        public void MoveTo(UInt16 x, UInt16 y)
        {
            MoveTo(new Coordinate(x, y));
        }

        public void MoveTo(Coordinate target)
        {
            int time = Time();
            if (time - BotGameData.LastTeleport > 5)
            {
                SendPacket(0x5f, BitConverter.GetBytes((UInt16)target.X), BitConverter.GetBytes((UInt16)target.Y));
                BotGameData.LastTeleport = time;
                Thread.Sleep(120);
            }
            else
            {
                double distance = BotGameData.Me.Location.Distance(target);
                SendPacket(0x03, BitConverter.GetBytes((UInt16)target.X), BitConverter.GetBytes((UInt16)target.Y));
                Thread.Sleep((int)(distance * 80));
            }
            BotGameData.Me.Location = target;
        }

        public virtual void Precast()
        {

        }

        public void RequestReassignment()
        {
            SendPacket(0x4b, GenericServerConnection.nulls, BitConverter.GetBytes(Me.Id));
        }

        public virtual void StashItems()
        {
            bool onCursor = false;
            List<ItemType> items;
            lock (ItemListLock)
            {
                 items = new List<ItemType>(BotGameData.Items.Values);
            }
            foreach (ItemType i in items)
            {
                onCursor = false;

                if (i.action == (uint)ItemType.item_action_type.to_cursor)
                    onCursor = true;
                else if (i.container == ItemType.ItemContainerType.inventory)
                    onCursor = false;
                else
                    continue;

                if (i.type == "tbk" || i.type == "cm1" || i.type == "cm2")
                    continue;

                Coordinate stashLocation;
                if (!BotGameData.Stash.FindFreeSpace(i, out stashLocation))
                {
                    continue;
                }

                Console.WriteLine("{0}: [D2GS] Stashing item {1}, at {2}, {3}", Account, i.name, stashLocation.X, stashLocation.Y);

                if (!onCursor)
                {
                    SendPacket(0x19, BitConverter.GetBytes((UInt32)i.id));
                    Thread.Sleep(500);
                }

                SendPacket(0x18, BitConverter.GetBytes((UInt32)i.id), BitConverter.GetBytes((UInt32)stashLocation.X), BitConverter.GetBytes((UInt32)stashLocation.Y), new byte[] { 0x04, 0x00, 0x00, 0x00 });
                Thread.Sleep(400);
            }
        }

        public void SwitchSkill(uint skill)
        {
            BotGameData.RightSkill = skill;
            byte[] temp = {0xFF, 0xFF, 0xFF , 0xFF };
            SendPacket(0x3c, BitConverter.GetBytes(skill), temp);
            Thread.Sleep(100);
        }

        public bool TalkToHealer(UInt32 id)
        {
            if (!TalkToTrader(id))
                return false;

            SendPacket(0x30, GenericServerConnection.one, BitConverter.GetBytes(id));

            return true;
        }

        public bool TalkToTrader(UInt32 id)
        {
            BotGameData.TalkedToNpc = false;
            NpcEntity npc = BotGameData.Npcs[id];

            double distance = BotGameData.Me.Location.Distance(npc.Location);

            //if(debugging)
            Console.WriteLine("{0}: [D2GS] Attempting to talk to NPC",Account);

            SendPacket(0x59, GenericServerConnection.one, BitConverter.GetBytes(id),
                        BitConverter.GetBytes((UInt16)BotGameData.Me.Location.X), GenericServerConnection.zero, GenericServerConnection.zero,
                        BitConverter.GetBytes((UInt16)BotGameData.Me.Location.Y), GenericServerConnection.zero, GenericServerConnection.zero);

            int sleepStep = 200;
            for (int timeDifference = (int)distance * 120; timeDifference > 0; timeDifference -= sleepStep)
            {
                SendPacket(0x04, GenericServerConnection.one, BitConverter.GetBytes(id));
                Thread.Sleep(Math.Min(sleepStep,timeDifference));
            }

            SendPacket(0x13, GenericServerConnection.one, BitConverter.GetBytes(id));
            Thread.Sleep(200);
            SendPacket(0x2f, GenericServerConnection.one, BitConverter.GetBytes(id));

            int timeoutStep = 100;
            for (long npc_timeout = 4000; npc_timeout > 0 && !BotGameData.TalkedToNpc; npc_timeout -= timeoutStep)
		            Thread.Sleep(timeoutStep);

            if (!BotGameData.TalkedToNpc)
            {
                Console.WriteLine("{0}: [D2GS] Failed to talk to NPC", Account);
                return false;
            }
            return true;
        }

        public bool TownPortal()
        {
            BotGameData.Me.PortalId = 0;
            SwitchSkill(0xDC);
            CastOnSelf();
        	int timeoutStep = 100;
	        for (int npc_timeout = 2000; npc_timeout > 0 && BotGameData.Me.PortalId == 0; npc_timeout -= timeoutStep)
        		Thread.Sleep(timeoutStep);

            if (BotGameData.Me.PortalId == 0)
            {
                Console.WriteLine("{0}: [D2GS] Failed to take town portal.", Account);
                return false;
            }

            byte[] temp = {0x02,0x00,0x00,0x00}; 
            SendPacket(0x13, temp, BitConverter.GetBytes(BotGameData.Me.PortalId));
	        
            Thread.Sleep(400);

            Status = ClientStatus.STATUS_IN_TOWN;

            return true;
        }

        public virtual void UsePotion()
        {
            ItemType pot = (from n in BotGameData.Belt.m_items 
                    where n.type == "rv1" select n).FirstOrDefault();

            if (pot == default(ItemType))
            {
                Console.WriteLine("{0}: [D2GS] No potions found in belt!", Account);
                return;
            }
            SendPacket(0x26, BitConverter.GetBytes(pot.id), GenericServerConnection.nulls, GenericServerConnection.nulls);
            BotGameData.Belt.m_items.Remove(pot);
        }

        public void WeaponSwap()
        {

        }

        /*
         * 
         * Constructor
         * 
         * 
         */
        public ClientlessBot(DataManager dm, String bnetServer, String account, String password, String classicKey, String expansionKey, uint potlife, uint chickenlife, String binaryDirectory, GameDifficulty difficulty, String gamepass)
        {
            m_battleNetServer = bnetServer;
            m_account = account;
            m_password = password;
            m_binaryDirectory = binaryDirectory;
            m_classicKey = classicKey;
            m_expansionKey = expansionKey;
            m_difficulty = difficulty;
            m_gamePassword = gamepass;
            m_potLife = potlife;
            m_chickenLife = chickenlife;
            m_keyOwner = "AlphaBot";
            m_gameExeInformation = "Game.exe 03/09/10 04:10:51 61440";

            m_dm = dm;
            m_bncs = new BattleNetCS(this);
            m_mcp = new RealmServer(this);
            m_gs = new GameServer(this);
            m_bncsThread = new Thread(m_bncs.ThreadFunction);
            m_mcpThread = new Thread(m_mcp.ThreadFunction);
            m_gameData = new GameData();
            m_gameCreationThread = new Thread(CreateGameThreadFunction);
            m_itemListLock = new Object();
        }


        private ClientlessBot(DataManager dm)
        {
            m_gameExeInformation = "Game.exe 03/09/10 04:10:51 61440";
            m_dm = dm;
            m_bncs = new BattleNetCS(this);
            m_mcp = new RealmServer(this);
            m_gs = new GameServer(this);
            m_bncsThread = new Thread(m_bncs.ThreadFunction);
            m_mcpThread = new Thread(m_mcp.ThreadFunction);
            m_gameData = new GameData();
            m_gameCreationThread = new Thread(CreateGameThreadFunction);
        }

        public void StartMcpThread()
        {
            m_mcpThread.Start();
        }

        public void StartGameCreationThread()
        {
            Console.WriteLine("{0}: [BOT]  Game creation thread started.", m_account);
            m_bncs.Kill();
            m_gameCreationThread.Start();
        }

        public void StartGameServerThread()
        {
            m_gsThread = new Thread(m_gs.ThreadFunction);
            m_gsThread.Start();
        }

        public void InitializeGameData()
        {
            m_gameData.FullyEnteredGame = false;
            m_gameData.LastTeleport = 0;
            m_gameData.Experience = 0;
            m_gameData.Me = new Player();

            m_gameData.SkillLevels.Clear();
            m_gameData.ItemSkillLevels.Clear();
            m_gameData.Players.Clear();
            m_gameData.Npcs.Clear();
            m_gameData.Items.Clear();

            m_gameData.Inventory = new Container("Inventory", GameData.InventoryWidth, GameData.InventoryHeight);
            m_gameData.Stash = new Container("Stash", GameData.StashWidth, GameData.StashHeight);
            m_gameData.Cube = new Container("Cube", GameData.CubeWidth, GameData.CubeHeight);
            m_gameData.Belt = new Container("Belt", 4, 4);

            m_gameData.MalahId = 0;
            m_gameData.CurrentLife = 0;
            m_gameData.FirstNpcInfoPacket = true;
            m_gameData.AttacksSinceLastTeleport = 0;
            m_gameData.WeaponSet = 0;
            m_gameData.HasMerc = false;
        }

        #region IDisposable Members

        private bool m_disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!this.m_disposed)
            {
                if (disposing)
                {
                    if (m_gs.m_socket.Connected)
                        LeaveGame();
                }
            }

            m_disposed = true;
        }

        #endregion

        ~ClientlessBot()
        {
            Dispose(false);    
        }

    }
}