using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpClient
{
    class GameData
    {
        /*
         * 
         *  Enumerations and Static Consts
         * 
         * 
         */
	    public enum CharacterClassType
	    {
		    AMAZON,
		    SORCERESS,
		    NECROMANCER,
		    PALADIN,
		    BARBARIAN,
		    DRUID,
		    ASSASSIN
	    };

        public enum CharacterSkillSetupType
        {
            UNKNOWNSETUP,
            SORCERESS_LIGHTNING,
            SORCERESS_METEOR,
            SORCERESS_ORB,
            SORCERESS_BLIZZARD,
            SORCERESS_METEORB,
            PALADIN_HAMMERDIN,
            PALADIN_SMITER
        };

        
        public enum ActType
        {
            ACT_I = 0,
            ACT_II = 1,
            ACT_III = 2,
            ACT_IV = 3,
            ACT_V = 4
        };

        public const Int32 InventoryWidth = 10;
        public const Int32 InventoryHeight = 4;

        public const Int32 StashWidth = 6;
        public const Int32 StashHeight = 8;

        public const Int32 CubeWidth = 3;
        public const Int32 CubeHeight = 4;


        public GameData()
        {
            SkillLevels = new Dictionary<Skills.Type, uint>();
            ItemSkillLevels = new Dictionary<Skills.Type, uint>();
            Players = new Dictionary<uint,Player>();
            Npcs = new Dictionary<uint,NpcEntity>();
            Items = new Dictionary<uint, ItemType>();
        }
        /*
         * 
         * Members and Properties
         * 
         */

        private CharacterSkillSetupType m_skillSetup;
        public CharacterSkillSetupType CharacterSkillSetup { get { return m_skillSetup; } set { m_skillSetup = value; } }

        protected ActType m_currentAct;
        public ActType CurrentAct { get { return m_currentAct; } set { m_currentAct = value; } }

        protected Int32 m_mapId;
        public Int32 MapId { get { return m_mapId; } set { m_mapId = value; } }
        protected Int32 m_areaId;
        public Int32 AreaId { get { return m_areaId; } set { m_areaId = value; } }

        protected Boolean m_fullyEnteredGame;
        public Boolean FullyEnteredGame { get { return m_fullyEnteredGame; } set { m_fullyEnteredGame = value; } }

        protected Boolean m_talkedToNpc;
        public Boolean TalkedToNpc { get { return m_talkedToNpc; } set { m_talkedToNpc = value; } }


        protected Int32 m_lastTeleport;
        public Int32 LastTeleport { get { return m_lastTeleport; } set { m_lastTeleport = value; } }

        protected UInt32 m_experience;
        public UInt32 Experience { get { return m_experience; } set { m_experience = value; } }

        protected UInt32 m_rightSkill;
        public UInt32 RightSkill { get { return m_rightSkill; } set { m_rightSkill = value; } }


        protected Player m_me;
        public Player Me { get { return m_me; } set { m_me = value; } }

        private Dictionary<Skills.Type, UInt32> m_skillLevels;
        public Dictionary<Skills.Type, UInt32> SkillLevels { get { return m_skillLevels; } set { m_skillLevels = value; } } 

        private Dictionary<Skills.Type, UInt32> m_itemSkillLevels;
        public Dictionary<Skills.Type, UInt32> ItemSkillLevels { get { return m_itemSkillLevels; }set { m_itemSkillLevels = value; } }

        private Dictionary<UInt32, Player> m_players;
        public Dictionary<UInt32, Player> Players { get { return m_players; } set { m_players = value; } }

        private Dictionary<UInt32, NpcEntity> m_npcs;
        public Dictionary<UInt32, NpcEntity> Npcs { get { return m_npcs; } set { m_npcs = value; } }

        private Dictionary<UInt32, ItemType> m_items;
        public Dictionary<UInt32, ItemType> Items { get { return m_items; } set { m_items = value; } }

        private Container m_inventory;
        public Container Inventory { get { return m_inventory; } set { m_inventory = value; } }
        
        private Container m_stash;
        public Container Stash { get { return m_stash; } set { m_stash = value; } }
        
        private Container m_cube;
        public Container Cube { get { return m_cube; } set { m_cube = value; } }

        private Container m_belt;
        public Container Belt { get { return m_belt; } set { m_belt = value; } }

        private Int32 m_malahId;
        public Int32 MalahId { get { return m_malahId; } set { m_malahId = value; } }

        private UInt32 m_currentLife;
        public UInt32 CurrentLife { get { return m_currentLife; } set { m_currentLife = value; } }

        protected Boolean m_firstNpcInfoPacket;
        public Boolean FirstNpcInfoPacket { get { return m_firstNpcInfoPacket; } set { m_firstNpcInfoPacket = value; } }

        private Int32 m_attackSinceLastTeleport;
        public Int32 AttacksSinceLastTeleport { get { return m_attackSinceLastTeleport; } set { m_attackSinceLastTeleport = value; } }

        private Int32 m_weaponSet;
        public Int32 WeaponSet { get { return m_weaponSet; } set { m_weaponSet = value; } }

        protected Boolean m_hasMerc;
        public Boolean HasMerc { get { return m_hasMerc; } set { m_hasMerc = value; } }

        private Int32 m_lastTimestamp;
        public Int32 LastTimestamp { get { return m_lastTimestamp; } set { m_lastTimestamp = value; } }

        /*
         * 
         * Methods
         * 
         */
        public Player GetPlayer(UInt32 id)
        {
            if (id == m_me.Id)
                return m_me;
            else
            {
                Player temp;
                bool success = m_players.TryGetValue(id,out temp);

                if (success)
                    return temp;
                else
                {
                    m_players.Add(id, new Player());
                    m_players[id].Id = id;
                    return m_players[id];
                }
            }

                
        }
    }
}
