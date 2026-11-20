using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>
    /// Peta tile dari file CSV (Tiled bisa export CSV).
    /// 0 = kosong, 1 = padat (tanah), 2 = bahaya (duri).
    /// Kalau tileset PNG ada: strip horizontal, tile id N diambil dari kolom ke-(N-1).
    /// Kalau belum ada: tile digambar sebagai outline.
    /// </summary>
    public sealed class Tilemap
    {
        public const int TileSize = 16;

        private static readonly Pen SolidPen = new Pen(Color.FromArgb(150, 150, 170));
        private static readonly Pen HazardPen = new Pen(Color.FromArgb(220, 70, 70));

        private readonly int[,] _tiles; // [baris, kolom]
        private readonly Bitmap _tileset; // boleh null

        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public int PixelWidth { get { return Columns * TileSize; } }
        public int PixelHeight { get { return Rows * TileSize; } }

        public Tilemap(string levelRelativePath, Bitmap tilesetOrNull)
        {
            _tileset = tilesetOrNull;

            string full = AssetManager.PathOf(levelRelativePath);
            if (!File.Exists(full))
                throw new FileNotFoundException("File level tidak ditemukan: " + full);

            List<int[]> rows = new List<int[]>();
            int maxCols = 0;
            foreach (string line in File.ReadAllLines(full))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] parts = line.Split(',');
                int[] row = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    row[i] = int.Parse(parts[i].Trim());
                rows.Add(row);
                if (row.Length > maxCols) maxCols = row.Length;
            }

            Rows = rows.Count;
            Columns = maxCols;
            _tiles = new int[Rows, Columns];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    _tiles[r, c] = rows[r][c];
        }

        public int GetTile(int col, int row)
        {
            if (col < 0 || col >= Columns) return 1;   // sisi kiri/kanan dianggap dinding
            if (row < 0 || row >= Rows) return 0;      // atas/bawah terbuka (jatuh ke jurang)
            return _tiles[row, col];
        }

        public bool IsSolid(int col, int row) { return GetTile(col, row) == 1; }
        public bool IsHazard(int col, int row) { return GetTile(col, row) == 2; }

        public void Draw(Graphics g, int camX, int camY)
        {
            int c0 = Math.Max(0, camX / TileSize);
            int c1 = Math.Min(Columns - 1, (camX + GameWindow.ViewW) / TileSize);
            int r0 = Math.Max(0, camY / TileSize);
            int r1 = Math.Min(Rows - 1, (camY + GameWindow.ViewH) / TileSize);

            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    int id = _tiles[r, c];
                    if (id <= 0) continue;

                    int x = c * TileSize - camX;
                    int y = r * TileSize - camY;

                    if (_tileset != null)
                    {
                        Rectangle src = new Rectangle((id - 1) * TileSize, 0, TileSize, TileSize);
                        if (src.Right > _tileset.Width) continue;
                        g.DrawImage(_tileset, new Rectangle(x, y, TileSize, TileSize), src, GraphicsUnit.Pixel);
                    }
                    else if (id == 1)
                    {
                        g.DrawRectangle(SolidPen, x, y, TileSize - 1, TileSize - 1);
                    }
                    else if (id == 2)
                    {
                        g.DrawPolygon(HazardPen, new Point[]
                        {
                            new Point(x, y + TileSize - 1),
                            new Point(x + TileSize / 2, y + 2),
                            new Point(x + TileSize - 1, y + TileSize - 1)
                        });
                    }
                }
            }
        }
    }
}
