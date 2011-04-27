
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Data.SQLite;

namespace BattleNet
{
    class AlphaBot : ClientlessBot
    {
        public const byte D2GS_WORLDOBJECT	= 0x51;
        public const byte D2GS_ASSIGNPLAYER	= 0x59;
        public const byte D2GS_ASSIGNNPC	= 0xAC;
        // Bosses to kill
        protected Boolean m_pindle, m_eldritch, m_shenk;
        public Boolean Pindle { get { return m_pindle; } set { m_pindle = value; } }
        public Boolean Eldritch { get { return m_eldritch; } set { m_eldritch = value; } }
        public Boolean Shenk { get { return m_shenk; } set { m_shenk = value; } }

        protected Entity m_redPortal, m_harrogathWp, m_act1Wp;
        protected Thread m_botThread;

        public override void ReceivedGameServerPacket(List<byte> data)
        {
            byte[] packet = data.ToArray();
            switch (packet[0])
            {
                case D2GS_WORLDOBJECT:
                    if (packet[1] == 0x02)
                    {
                        UInt32 obj = BitConverter.ToUInt16(packet, 6);
                        // Pindles portal
                        if (obj == 0x003c)
                        {
                            m_redPortal.Id = BitConverter.ToUInt32(packet, 2);
                            m_redPortal.Location.X = BitConverter.ToUInt16(packet, 8);
                            m_redPortal.Location.Y = BitConverter.ToUInt16(packet, 10);

                            if (debugging) 
                                Console.WriteLine("{0}: [D2GS] Received red portal ID and coordinates", Account);
                        }
                        // A5 WP
                        if (obj == 0x01ad)
                        {
                            m_harrogathWp.Id = BitConverter.ToUInt32(packet, 2);
                            m_harrogathWp.Location.X = BitConverter.ToUInt16(packet, 8);
                            m_harrogathWp.Location.Y = BitConverter.ToUInt16(packet, 10);

                            if (debugging) 
                                Console.WriteLine("{0}: [D2GS] Received A5 WP id and coordinates", Account);
                        }
                        // A1 WP
                        if (obj == 0x0077)
                        {
                            m_act1Wp.Id = BitConverter.ToUInt32(packet, 2);
                            m_act1Wp.Location.X = BitConverter.ToUInt16(packet, 8);
                            m_act1Wp.Location.Y = BitConverter.ToUInt16(packet, 10);
                            if(debugging)
                                Console.WriteLine("{0}: [D2GS] Received A1 WP id and coordinates", Account);
                        }
                    }
                    break;
                case D2GS_ASSIGNPLAYER: // Player has entered the game.
				    if (!InGame) {
                        m_botThread = new Thread(BotThreadFunction);
                        m_botThread.Start();
					    Status = ClientStatus.STATUS_IN_TOWN;
					    InGame = true;
				    }
                    break;
            }
        }

        public bool VisitMalah()
        {
            NpcEntity malah = GetNpc("Malah");
            if (malah != null && malah != default(NpcEntity))
                TalkToTrader(malah.Id);
            else
            {
                LeaveGame();
                return false;
            }

            if (GetSkillLevel(Skills.Type.book_of_townportal) < 10)
            {
                Thread.Sleep(300);
                SendPacket(0x38, GenericServer.one, BitConverter.GetBytes(malah.Id), GenericServer.nulls);
                Thread.Sleep(2000);
                Item n = (from item in BotGameData.Items
                              where item.Value.action == (uint)Item.Action.add_to_shop
                              && item.Value.type == "tsc"
                              select item).FirstOrDefault().Value;

                Console.WriteLine("{0}: [D2GS] Buying TPs", Account);
                byte[] temp = { 0x02, 0x00, 0x00, 0x00 };
                for (int i = 0; i < 9; i++)
                {
                    SendPacket(0x32, BitConverter.GetBytes(malah.Id), BitConverter.GetBytes(n.id), GenericServer.nulls, temp);
                    Thread.Sleep(200);
                }
                Thread.Sleep(500);
            }
            if (malah != null && malah != default(NpcEntity))
                SendPacket(0x30, GenericServer.one, BitConverter.GetBytes(malah.Id));
            else
            {
                LeaveGame();
                return false;
            }

            Thread.Sleep(300);
            return true;
        }

        public bool ReviveMerc()
        {
            if (!BotGameData.HasMerc)
            {
                Console.WriteLine("{0}: [D2GS] Reviving Merc", Account);
                MoveTo(5082, 5080);
                MoveTo(5060, 5076);

                NpcEntity qual = GetNpc("Qual-Kehk");
                if (qual != null && qual != default(NpcEntity))
                    TalkToTrader(qual.Id);
                else
                {
                    LeaveGame();
                    return false;
                }
                byte[] three = { 0x03, 0x00, 0x00, 0x00 };
                SendPacket(0x38, three, BitConverter.GetBytes(qual.Id), GenericServer.nulls);
                Thread.Sleep(300);
                SendPacket(0x62, BitConverter.GetBytes(qual.Id));
                Thread.Sleep(300);
                SendPacket(0x38, three, BitConverter.GetBytes(qual.Id), GenericServer.nulls);
                Thread.Sleep(300);
                SendPacket(0x30, GenericServer.one, BitConverter.GetBytes(qual.Id));
                Thread.Sleep(300);

                MoveTo(5060, 5076);
                MoveTo(5082, 5080);
                MoveTo(5081, 5076);
            }
            return true;
        }

        void EnterRedPortal()
        {
            Thread.Sleep(700);
            byte[] two = { 0x02, 0x00, 0x00, 0x00 };
            SendPacket(0x13, two, BitConverter.GetBytes(m_redPortal.Id));
            Thread.Sleep(500);
        }

        public void DoPindle()
        {
            UInt32 curLife = 0;

            if (Pindle)
            {
                MoveTo(5089, 5019);
                MoveTo(5090, 5030);
                MoveTo(5082, 5033);
                MoveTo(5074, 5033);

                if (!VisitMalah())
                    return;

                MoveTo(5073, 5032);
                MoveTo(5073, 5044);
                MoveTo(5078, 5055);
                MoveTo(5081, 5065);
                MoveTo(5081, 5076);

                if (!ReviveMerc())
                    return;

                MoveTo(5082, 5087);
                MoveTo(5085, 5098);
                MoveTo(5088, 5110);
                MoveTo(5093, 5121);
                MoveTo(5103, 5124);
                MoveTo(5111, 5121);

                EnterRedPortal();

                Status = ClientStatus.STATUS_KILLING_PINDLESKIN;
                Console.WriteLine("{0}: [D2GS] Killing Pindleskin", Account);

                Precast();

                SwitchSkill(0x36);
                Thread.Sleep(300);

                CastOnCoord(10064, 13286);
                Thread.Sleep(300);
                CastOnCoord(10061, 13260);
                Thread.Sleep(300);
                CastOnCoord(10058, 13236);
                Thread.Sleep(300);
                if (ClientlessBot.debugging)
                    Console.WriteLine("Current Position: ({0},{1})", Me.Location.X, Me.Location.Y);
                
                NpcEntity pindle = GetNpc("Pindleskin");
                if (pindle == default(NpcEntity))
                {
                    Thread.Sleep(500);
                    pindle = GetNpc("Pindleskin");
                    if (pindle == default(NpcEntity))
                    {
                        Console.WriteLine("{0}: [D2GS] Unable to find Pindleskin, probably got stuck.", Account);
                        LeaveGame();
                        return;
                    }
                }
                curLife = BotGameData.Npcs[pindle.Id].Life;
                if (BotGameData.Npcs[pindle.Id].IsLightning && BotGameData.CharacterSkillSetup == GameData.CharacterSkillSetupType.SORCERESS_LIGHTNING && Difficulty == GameDifficulty.HELL)
                {
                    LeaveGame();
                    return;
                }
                while (BotGameData.Npcs[pindle.Id].Life > 0 && m_gs.m_socket.Connected)
                {
                    if (!Attack(pindle.Id))
                    {
                        LeaveGame();
                        return;
                    }
                    if (curLife > BotGameData.Npcs[pindle.Id].Life)
                    {
                        curLife = BotGameData.Npcs[pindle.Id].Life;
                        //Console.WriteLine("{0}: [D2GS] Pindleskins Life: {1}", Account, curLife);
                    }
                }
                Console.WriteLine("{0}: [D2GS] {1} is dead. Killing minions", Account, pindle.Name);
                 
                NpcEntity monster;
                while (GetAliveNpc("Defiled Warrior", 20, out monster) && m_gs.m_socket.Connected)
                {
                    curLife = BotGameData.Npcs[monster.Id].Life;
                    Console.WriteLine("{0}: [D2GS] Killing Defiled Warrior", Account);
                    while (BotGameData.Npcs[monster.Id].Life > 0 && m_gs.m_socket.Connected)
                    {
                        if (!Attack(monster.Id))
                        {
                            LeaveGame();
                            return;
                        }
                        if (curLife > BotGameData.Npcs[monster.Id].Life)
                        {
                            curLife = BotGameData.Npcs[monster.Id].Life;
                            //Console.WriteLine("{0}: [D2GS] Defiled Warriors Life: {1}", Account, curLife);
                        }
                    }
                }
                Console.WriteLine("{0}: [D2GS] Minions are dead, looting...", Account);
                PickItems();

                //if (!TownPortal())
                //{
                    //LeaveGame();
                    //return;
                //}
            }
        }

        public void MoveToAct5()
        {
            if (BotGameData.CurrentAct == GameData.ActType.ACT_I)
            {
                Console.WriteLine("{0}: [D2GS] Moving to Act 5", Account);
                MoveTo(m_act1Wp.Location);
                byte[] temp = { 0x02, 0x00, 0x00, 0x00 };
                SendPacket(0x13, temp, BitConverter.GetBytes(m_act1Wp.Id));
                Thread.Sleep(300);
                byte[] tempa = { 0x6D, 0x00, 0x00, 0x00 };
                SendPacket(0x49, BitConverter.GetBytes(m_act1Wp.Id), tempa);
                Thread.Sleep(300);
                MoveTo(5105, 5050);
                MoveTo(5100, 5025);
                MoveTo(5096, 5018);
            }
        }

        public override void BotThreadFunction()
        {
            Int32 startTime = Time();
            if (ClientlessBot.debugging)
                Console.WriteLine("{0}: [D2GS] Bot is in town.", Account);

            Thread.Sleep(500);

            StashItems();

            MoveToAct5();

            if (BotGameData.WeaponSet != 0)
                WeaponSwap();

            DoPindle();
           
            FailedGame = false;
            LeaveGame();
            Int32 endTime = Time() - startTime;
            Console.WriteLine("{0}: [BOT] Run took {1} seconds.", Account, endTime);
        }   

        public AlphaBot(bool pindle, bool eld, bool shenk, DataManager dm, String bnetServer, String account, String password, String classicKey, String expansionKey, uint potlife, uint chickenlife, String binaryDirectory, GameDifficulty difficulty, String gamepass) :
            base(dm,bnetServer,account,password,classicKey,expansionKey,potlife,chickenlife,binaryDirectory,difficulty,gamepass)
        {
            Pindle = pindle;
            Eldritch = eld;
            Shenk = shenk;
            m_redPortal = new Entity();
            m_harrogathWp = new Entity();
            m_act1Wp = new Entity();
        }

        static void Main(string[] args)
        {
            
            DataManager dm = new DataManager("data");
            Pickit.InitializePickit();
            AlphaBot cb;
            AlphaBot cb2;
            AlphaBot cb3;
            if (args.Length < 4)
                Console.WriteLine("Must supply command line args");
            else
            {             
               
                cb.Start();
                Thread.Sleep(5000);
                cb2.Start();
                Thread.Sleep(5000);
                cb3.Start();
            }
            Console.ReadKey();
            return;
        }

    }
}
