
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Serialization;

namespace CSharpClient
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

        protected static List<ItemType> m_pickitList = new List<ItemType>();

        protected static void InitializePickit()
        {
            FileStream fs = new FileStream("pickit.xml", FileMode.Open);
            XmlSerializer x = new XmlSerializer(typeof(List<ItemType>));
            m_pickitList = (List<ItemType>)x.Deserialize(fs);

            foreach (ItemType i in m_pickitList)
            {
                Console.WriteLine("{0}: {1}, {2}, Ethereal:{3}", i.name, i.type, i.quality, i.ethereal);
            }
        }

        public override void ReceivedGameServerPacket(List<byte> data)
        {
            byte[] packet = data.ToArray();
            switch (packet[0])
            {
                case D2GS_WORLDOBJECT: // Assigns objects (shrines, portals, torches, stash, chests...)
                    //data_pointer = packet.c_str();
                    if (packet[1] == 0x02)
                    {
                        UInt32 obj = BitConverter.ToUInt16(packet, 6);
                        // Pindles portal
                        if (obj == 0x003c)
                        {
                            m_redPortal.Id = BitConverter.ToUInt32(packet, 2);
                            m_redPortal.Location.X = BitConverter.ToUInt16(packet, 8);
                            m_redPortal.Location.Y = BitConverter.ToUInt16(packet, 10);

                            //if (debugging) 
                            Console.WriteLine("{0}: [D2GS] Received red portal ID and coordinates", Account);
                        }
                        // A5 WP
                        if (obj == 0x01ad)
                        {
                            m_harrogathWp.Id = BitConverter.ToUInt32(packet, 2);
                            m_harrogathWp.Location.X = BitConverter.ToUInt16(packet, 8);
                            m_harrogathWp.Location.Y = BitConverter.ToUInt16(packet, 10);

                            //if (debugging) 
                                Console.WriteLine("{0}: [D2GS] Received A5 WP id and coordinates", Account);
                        }
                        // A1 WP
                        if (obj == 0x0077)
                        {
                            m_act1Wp.Id = BitConverter.ToUInt32(packet, 2);
                            m_act1Wp.Location.X = BitConverter.ToUInt16(packet, 8);
                            m_act1Wp.Location.Y = BitConverter.ToUInt16(packet, 10);
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

        public override void BotThreadFunction()
        {
            //Console.WriteLine("{0}: [D2GS] Bot is in town.", Account);
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
            DataManager dm = new DataManager("data\\");
            AlphaBot.InitializePickit();
            AlphaBot cb = new AlphaBot(true,false,false,dm, "useast.battle.net", args[0], args[1], args[2], args[3], 200, 100, "data\\", GameDifficulty.NIGHTMARE, "xa1");
            cb.Start();
            Console.ReadKey();
            return;
        }

    }
}
