using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CSharpClient
{



    class DataManager
    {

        public ItemDataType m_itemData;
        public PlainTextDataType m_experiences,
                            m_magicalPrefixes,
                            m_magicalSuffixes,
                            m_rarePrefixes,
                            m_rareSuffixes,
                            m_uniqueItems,
                            m_monsterNames,
                            m_monsterFields,
                            m_superUniques,
                            m_itemProperties,
                            m_skills;

        public DataManager(String dataDirectory)
        {
            String[] fileNames =
            {
          		"experience.txt",
		        "magical_prefixes.txt",
		        "magical_suffixes.txt",
		        "rare_prefixes.txt",
		        "rare_suffixes.txt",
		        "unique_items.txt",
		        "monster_names.txt",
		        "monster_fields.txt",
		        "super_uniques.txt",
		        "item_properties.txt",
		        "skills.txt"
            };

            String itemDataFile = Path.Combine(dataDirectory, "item_data.txt");
            m_itemData = new ItemDataType(itemDataFile);
            m_experiences = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[0]));
            m_magicalPrefixes = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[1]));
            m_magicalSuffixes = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[2]));
            m_rarePrefixes = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[3]));
            m_rareSuffixes = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[4]));
            m_uniqueItems = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[5]));
            m_monsterNames = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[6]));
            m_monsterFields = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[7]));
            m_superUniques = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[8]));
            m_itemProperties = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[9]));
            m_skills = new PlainTextDataType(Path.Combine(dataDirectory, fileNames[10]));

            return;
        }

    }

    class ItemDataType
    {
        private List<ItemEntry> m_items;
        public List<ItemEntry> Items { get { return m_items; } } 

        public ItemDataType()
        {
        }

        public ItemDataType(String file)
        {
            m_items = new List<ItemEntry>();
            Dictionary<String,ItemType.ItemClassificationType> classificationMap = new Dictionary<string,ItemType.ItemClassificationType>();
            classificationMap["Amazon Bow"] = ItemType.ItemClassificationType.amazon_bow;
            classificationMap["Amazon Javelin"] = ItemType.ItemClassificationType.amazon_javelin;
            classificationMap["Amazon Spear"] = ItemType.ItemClassificationType.amazon_spear;
            classificationMap["Amulet"] = ItemType.ItemClassificationType.amulet;
            classificationMap["Antidote Potion"] = ItemType.ItemClassificationType.antidote_potion;
            classificationMap["Armor"] = ItemType.ItemClassificationType.armor;
            classificationMap["Arrows"] = ItemType.ItemClassificationType.arrows;
            classificationMap["Assassin Katar"] = ItemType.ItemClassificationType.assassin_katar;
            classificationMap["Axe"] = ItemType.ItemClassificationType.axe;
            classificationMap["Barbarian Helm"] = ItemType.ItemClassificationType.barbarian_helm;
            classificationMap["Belt"] = ItemType.ItemClassificationType.belt;
            classificationMap["Body Part"] = ItemType.ItemClassificationType.body_part;
            classificationMap["Bolts"] = ItemType.ItemClassificationType.bolts;
            classificationMap["Boots"] = ItemType.ItemClassificationType.boots;
            classificationMap["Bow"] = ItemType.ItemClassificationType.bow;
            classificationMap["Circlet"] = ItemType.ItemClassificationType.circlet;
            classificationMap["Club"] = ItemType.ItemClassificationType.club;
            classificationMap["Crossbow"] = ItemType.ItemClassificationType.crossbow;
            classificationMap["Dagger"] = ItemType.ItemClassificationType.dagger;
            classificationMap["Druid Pelt"] = ItemType.ItemClassificationType.druid_pelt;
            classificationMap["Ear"] = ItemType.ItemClassificationType.ear;
            classificationMap["Elixir"] = ItemType.ItemClassificationType.elixir;
            classificationMap["Gem"] = ItemType.ItemClassificationType.gem;
            classificationMap["Gloves"] = ItemType.ItemClassificationType.gloves;
            classificationMap["Gold"] = ItemType.ItemClassificationType.gold;
            classificationMap["Grand Charm"] = ItemType.ItemClassificationType.grand_charm;
            classificationMap["Hammer"] = ItemType.ItemClassificationType.hammer;
            classificationMap["Health Potion"] = ItemType.ItemClassificationType.health_potion;
            classificationMap["Helm"] = ItemType.ItemClassificationType.helm;
            classificationMap["Herb"] = ItemType.ItemClassificationType.herb;
            classificationMap["Javelin"] = ItemType.ItemClassificationType.javelin;
            classificationMap["Jewel"] = ItemType.ItemClassificationType.jewel;
            classificationMap["Key"] = ItemType.ItemClassificationType.key;
            classificationMap["Large Charm"] = ItemType.ItemClassificationType.large_charm;
            classificationMap["Mace"] = ItemType.ItemClassificationType.mace;
            classificationMap["Mana Potion"] = ItemType.ItemClassificationType.mana_potion;
            classificationMap["Necromancer Shrunken Head"] = ItemType.ItemClassificationType.necromancer_shrunken_head;
            classificationMap["Paladin Shield"] = ItemType.ItemClassificationType.paladin_shield;
            classificationMap["Polearm"] = ItemType.ItemClassificationType.polearm;
            classificationMap["Quest Item"] = ItemType.ItemClassificationType.quest_item;
            classificationMap["Rejuvenation Potion"] = ItemType.ItemClassificationType.rejuvenation_potion;
            classificationMap["Ring"] = ItemType.ItemClassificationType.ring;
            classificationMap["Rune"] = ItemType.ItemClassificationType.rune;
            classificationMap["Scepter"] = ItemType.ItemClassificationType.scepter;
            classificationMap["Scroll"] = ItemType.ItemClassificationType.scroll;
            classificationMap["Shield"] = ItemType.ItemClassificationType.shield;
            classificationMap["Small Charm"] = ItemType.ItemClassificationType.small_charm;
            classificationMap["Sorceress Orb"] = ItemType.ItemClassificationType.sorceress_orb;
            classificationMap["Spear"] = ItemType.ItemClassificationType.spear;
            classificationMap["Staff"] = ItemType.ItemClassificationType.staff;
            classificationMap["Stamina Potion"] = ItemType.ItemClassificationType.stamina_potion;
            classificationMap["Sword"] = ItemType.ItemClassificationType.sword;
            classificationMap["Thawing Potion"] = ItemType.ItemClassificationType.thawing_potion;
            classificationMap["Throwing Axe"] = ItemType.ItemClassificationType.throwing_axe;
            classificationMap["Throwing Knife"] = ItemType.ItemClassificationType.throwing_knife;
            classificationMap["Throwing Potion"] = ItemType.ItemClassificationType.throwing_potion;
            classificationMap["Tome"] = ItemType.ItemClassificationType.tome;
            classificationMap["Torch"] = ItemType.ItemClassificationType.torch;
            classificationMap["Wand"] = ItemType.ItemClassificationType.wand;

            List<string> lines = new List<string>();

            using (StreamReader r = new StreamReader(file))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            foreach (string line in lines)
            {
                try
                {
                    String[] tokens = line.Split('|');
                    if (tokens.Length == 0) 
                        continue;
                    if (tokens.Length != 8)
                    {
                        Console.WriteLine("Invalid Token Count: {0}", tokens.Length);
                        throw new Exception("Unable to parse item data");
                    }
                    String name = tokens[0];
                    String code = tokens[1];
                    String classification_string = tokens[2];
                    UInt32 width = UInt32.Parse(tokens[3]);
                    UInt32 height = UInt32.Parse(tokens[4]);
                    bool stackable = UInt32.Parse(tokens[5]) != 0;
                    bool usable = UInt32.Parse(tokens[6]) != 0;
                    bool throwable = UInt32.Parse(tokens[7]) != 0;
                    ItemType.ItemClassificationType classification;
                    if (!classificationMap.TryGetValue(classification_string, out classification))
                        throw new Exception("Unable to parse item classification");
                    ItemEntry i = new ItemEntry(name, code, classification, width, height, stackable, usable, throwable);
                    m_items.Add(i);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing ItemDataType: {0}", e.ToString());
                }
            }
        }

        public Boolean Get(String code, out ItemEntry output)
        {
            var items = from n in m_items where n.Type == code select n;

            foreach (ItemEntry i in items)
            {
                output = i;
                return true;
            }
            output = null;
            return false;
        }

    }
    class PlainTextDataType
    {
        private List<String[]> m_lines;

        public PlainTextDataType(String file)
        {
            m_lines = new List<string[]>();
            List<string> lines = new List<string>();

            using (StreamReader r = new StreamReader(file))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            foreach (String line in lines)
            {
                String[] tokens = line.Split('|');
                m_lines.Add(tokens);
            }
        }

        public Boolean Get(int offset, out String output)
        {
            if (offset < 0 || offset >= m_lines.Count)
            {
                output = "";
                return false;
            }
            String[] line = m_lines[offset];
            if (line.Length == 0)
                output = "";
            else
                output = line[0];
            return true;
        }

        public Boolean Get(int offset, out String[] output)
        {
            if (offset < 0 || offset >= m_lines.Count)
            {
                output = null;
                return false;
            }
            output = m_lines[offset];
            return true;
        }
    }

    class BinaryDataType
    {
        private List<byte> m_data;
        public BinaryDataType(String file)
        {
            m_data = new List<byte>(File.ReadAllBytes(file));
        }

        public Boolean Get(int offset, int length, out byte[] output)
        {
            if(offset < 0 || offset+length > m_data.Count)
            {
                output = null;
                return false;
            }
            output = m_data.GetRange(offset, length).ToArray();
            return true;
        }
    }

}
