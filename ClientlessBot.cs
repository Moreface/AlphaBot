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

namespace CSharpClient
{
    class ClientlessBot
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
        /*
         * 
         * In Game API
         * 
         * 
         */

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

        public virtual void ReceivedGameServerPacket(List<byte> data)
        {

        }

        public virtual void BotThreadFunction()
        {

        }

        public void JoinGame()
        {
            m_mcp.Write(m_mcp.BuildPacket(0x04, BitConverter.GetBytes(GameRequestId), System.Text.Encoding.ASCII.GetBytes(GameName), GenericServerConnection.zero, System.Text.Encoding.ASCII.GetBytes(GamePassword), GenericServerConnection.zero));
            GameRequestId++;
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

        public void LeaveGame()
        {
            InGame = false;
            ConnectedToGs = false;

            Console.WriteLine("{0}: [D2GS] Leaving the game.", Account);
            m_gs.Write(m_gs.BuildPacket(0x69));

            Thread.Sleep(500);
            m_gs.m_socket.Close();
            if(m_gs.m_pingThread.IsAlive)
                m_gs.m_pingThread.Join();   
            if(m_gsThread.IsAlive)
                m_gsThread.Join();
            Status = ClientStatus.STATUS_NOT_IN_GAME;
        }

        public void UsePotion()
        {
            var a = from n in BotGameData.Belt.m_items 
                    where n.type == "rv1" select n;

            foreach (var pot in a)
            {
                m_gs.Write(m_gs.BuildPacket(0x26, BitConverter.GetBytes(pot.id), GenericServerConnection.nulls, GenericServerConnection.nulls));
                BotGameData.Belt.m_items.Remove(pot);
                break;
            }
            
        }

        public void Start()
        {
            m_bncsThread.Start();
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

        ~ClientlessBot()
        {
            if(m_gs.m_socket.Connected)
                LeaveGame();
        }

        public void StartMcpThread()
        {
            m_mcpThread.Start();
        }

        public void StartGameCreationThread()
        {
            Console.WriteLine("{0}: [BOT]  Game creation thread started.", m_account);
            m_bncs.m_socket.Close();
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
    }
}