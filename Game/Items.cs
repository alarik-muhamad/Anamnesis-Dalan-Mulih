using System.Collections.Generic;
using System.Drawing;

namespace AnamnesisDalanMulih.Game
{
    public enum ItemId { None = 0, EmeraldPendant, Herb, Letter, GateKey }

    public enum Ending { Indecisive, Criminal, GoodSamaritan, Bad }

    public sealed class ItemInfo
    {
        public string Name;
        public char Glyph;
        public Color Color;
        public bool Important; // item penting tidak bisa dibuang (cegah soft-lock)
        public bool Usable;

        public ItemInfo(string name, char glyph, Color color, bool important, bool usable)
        {
            Name = name; Glyph = glyph; Color = color; Important = important; Usable = usable;
        }
    }

    public static class Items
    {
        private static readonly Dictionary<ItemId, ItemInfo> Table = new Dictionary<ItemId, ItemInfo>
        {
            { ItemId.EmeraldPendant, new ItemInfo("Liontin Zamrud", 'L', Color.FromArgb(60, 230, 150), true, false) },
            { ItemId.Herb, new ItemInfo("Ramuan", 'R', Color.FromArgb(240, 150, 200), false, true) },
            { ItemId.Letter, new ItemInfo("Surat Bu Lastri", 'S', Color.FromArgb(240, 230, 170), false, false) },
            { ItemId.GateKey, new ItemInfo("Kunci Gerbang", 'K', Color.FromArgb(255, 215, 70), true, false) }
        };

        public static ItemInfo Get(ItemId id) { return Table[id]; }
    }

    /// <summary>Data yang dibawa antar area: HP, karma, inventori 3 slot, dan flag cerita.</summary>
    public sealed class GameState
    {
        public const int SlotCount = 3;

        public int MaxHp = 3;
        public int Hp = 3;
        public int Karma;
        public int GoodActs;
        public int BadActs;
        public int Deaths;
        public ItemId[] Slots = new ItemId[SlotCount];
        public HashSet<string> Flags = new HashSet<string>();

        public bool Flag(string name) { return Flags.Contains(name); }
        public void Set(string name) { Flags.Add(name); }

        public bool Has(ItemId id)
        {
            for (int i = 0; i < SlotCount; i++) if (Slots[i] == id) return true;
            return false;
        }

        public bool HasFreeSlot
        {
            get
            {
                for (int i = 0; i < SlotCount; i++) if (Slots[i] == ItemId.None) return true;
                return false;
            }
        }

        public bool TryAdd(ItemId id)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Slots[i] == ItemId.None) { Slots[i] = id; return true; }
            }
            return false;
        }

        public bool Remove(ItemId id)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Slots[i] == id) { Slots[i] = ItemId.None; return true; }
            }
            return false;
        }

        public void AddKarma(int delta)
        {
            Karma += delta;
            if (delta > 0) GoodActs++;
            else if (delta < 0) BadActs++;
        }

        /// <summary>Karma >= 2 = Good Samaritan, <= -2 = Criminal, selain itu Indecisive.</summary>
        public Ending GetEnding()
        {
            if (Karma >= 2) return Ending.GoodSamaritan;
            if (Karma <= -2) return Ending.Criminal;
            return Ending.Indecisive;
        }

        public GameState Clone()
        {
            GameState c = new GameState();
            c.MaxHp = MaxHp;
            c.Hp = Hp;
            c.Karma = Karma;
            c.GoodActs = GoodActs;
            c.BadActs = BadActs;
            c.Deaths = Deaths;
            c.Slots = (ItemId[])Slots.Clone();
            c.Flags = new HashSet<string>(Flags);
            return c;
        }
    }
}
