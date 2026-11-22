using System;
using System.Collections.Generic;
using System.Drawing;
using AnamnesisDalanMulih.Engine;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>NPC: kotak berwarna bermata dua. Tekan E di dekatnya untuk bicara.</summary>
    public sealed class Npc
    {
        public const int W = 10;
        public const int H = 14;

        public string Name;
        public float X, Y;
        public Color Color;
        public EyeStyle Eyes = EyeStyle.Two;
        public bool FacingRight;
        public Func<GameState, DialogNode> Dialog;

        public Npc(string name, float x, float y, Color color, Func<GameState, DialogNode> dialog)
        {
            Name = name; X = x; Y = y; Color = color; Dialog = dialog;
        }

        public bool InRange(Player p)
        {
            return Math.Abs(p.CenterX - (X + W / 2f)) < 28f && Math.Abs(p.CenterY - (Y + H / 2f)) < 24f;
        }

        public void Draw(Graphics g, int camX, int camY)
        {
            int x = (int)Math.Round(X) - camX;
            int y = (int)Math.Round(Y) - camY;
            BoxSprite.Draw(g, x, y, W, H, Color, Eyes, FacingRight);
            Ui.DrawTag(g, Name, x + W / 2, y + H + 2, Color);
        }
    }

    /// <summary>Item di lantai (belah ketupat outline berwarna). Diambil dengan menyentuhnya.</summary>
    public sealed class Pickup
    {
        public const int Size = 10;

        public ItemId Item;
        public float X, Y;
        public bool Taken;
        public bool Declined; // pemain memilih meninggalkannya; aktif lagi setelah menjauh

        public Pickup(ItemId item, float x, float y) { Item = item; X = x; Y = y; }

        public bool Overlaps(Player p)
        {
            return p.Overlaps(X, Y, Size, Size);
        }

        public bool Near(Player p, float dist)
        {
            return Math.Abs(p.CenterX - (X + Size / 2f)) < dist && Math.Abs(p.CenterY - (Y + Size / 2f)) < dist;
        }

        public void Draw(Graphics g, int camX, int camY, float time)
        {
            if (Taken) return;
            int cx = (int)Math.Round(X + Size / 2f) - camX;
            int cy = (int)Math.Round(Y + Size / 2f + (float)Math.Sin(time * 3f)) - camY;
            using (Pen pen = new Pen(Items.Get(Item).Color))
            {
                g.DrawPolygon(pen, new Point[]
                {
                    new Point(cx, cy - 5), new Point(cx + 5, cy), new Point(cx, cy + 5), new Point(cx - 5, cy)
                });
            }
        }
    }

    /// <summary>Pintu (outline). Tekan E di dekatnya. NextArea = -1 berarti pintu terakhir (menuju ending).</summary>
    public sealed class Door
    {
        public const int W = 16;
        public const int H = 32;

        public float X, Y;
        public int NextArea;
        public Func<GameState, bool> CanOpen;
        public DialogNode LockedDialog;

        public Door(float x, float y, int nextArea, Func<GameState, bool> canOpen, DialogNode lockedDialog)
        {
            X = x; Y = y; NextArea = nextArea; CanOpen = canOpen; LockedDialog = lockedDialog;
        }

        public bool InRange(Player p)
        {
            return Math.Abs(p.CenterX - (X + W / 2f)) < 24f && Math.Abs(p.CenterY - (Y + H / 2f)) < 28f;
        }

        public void Draw(Graphics g, int camX, int camY, bool open)
        {
            int x = (int)Math.Round(X) - camX;
            int y = (int)Math.Round(Y) - camY;
            Color c = open ? Color.FromArgb(255, 215, 70) : Color.FromArgb(120, 120, 140);
            using (Pen pen = new Pen(c))
            {
                g.DrawRectangle(pen, x, y, W - 1, H - 1);
                g.DrawEllipse(pen, x + W / 2 - 2, y + H / 2 - 2, 4, 4); // lubang kunci
            }
        }
    }

    /// <summary>Area tak terlihat: begitu pemain masuk, dialog berjalan sekali (cerita / tutorial).</summary>
    public sealed class Trigger
    {
        public Rectangle Zone;
        public string Flag;
        public Func<GameState, DialogNode> Dialog;

        public Trigger(string flag, Rectangle zone, Func<GameState, DialogNode> dialog)
        {
            Flag = flag; Zone = zone; Dialog = dialog;
        }

        public bool Contains(Player p)
        {
            return Zone.Contains((int)p.CenterX, (int)p.CenterY);
        }
    }

    /// <summary>Tempat istirahat: titik respawn + HP pulih penuh. X = tengah, Y = permukaan tanah.</summary>
    public sealed class Checkpoint
    {
        public float X, Y;
        public bool Active;

        public Checkpoint(float x, float y) { X = x; Y = y; }

        public bool Overlaps(Player p)
        {
            return p.Overlaps(X - 8f, Y - 24f, 16, 24);
        }

        public void Draw(Graphics g, int camX, int camY)
        {
            int x = (int)Math.Round(X) - camX;
            int y = (int)Math.Round(Y) - camY;
            Color c = Active ? Color.FromArgb(120, 230, 140) : Color.FromArgb(90, 90, 110);
            using (Pen pen = new Pen(c))
            {
                g.DrawLine(pen, x, y, x, y - 16);
                g.DrawPolygon(pen, new Point[] { new Point(x, y - 16), new Point(x + 7, y - 13), new Point(x, y - 10) });
            }
        }
    }

    /// <summary>Isi satu area: level, musuh, NPC, item, pintu, trigger, checkpoint.</summary>
    public sealed class AreaDef
    {
        public string Title;
        public string Level;
        public Color Bg = Color.FromArgb(18, 18, 32);
        public string[] Intro = new string[0];
        public float SpawnX, SpawnY;

        public List<Npc> Npcs = new List<Npc>();
        public List<Enemy> Enemies = new List<Enemy>();
        public List<Pickup> Pickups = new List<Pickup>();
        public List<Door> Doors = new List<Door>();
        public List<Trigger> Triggers = new List<Trigger>();
        public List<Checkpoint> Checkpoints = new List<Checkpoint>();
    }
}
