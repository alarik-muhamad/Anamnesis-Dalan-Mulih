using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AnamnesisDalanMulih.Engine
{
    /// <summary>
    /// Memuat PNG dari folder Assets/ (path relatif terhadap .exe).
    /// Kalau file belum ada, TryLoadImage mengembalikan null dan pemanggil
    /// menggambar outline sederhana lewat kode. Tidak ada gambar yang dibuat otomatis.
    /// </summary>
    public static class AssetManager
    {
        public static readonly string Root = Path.Combine(AppContext.BaseDirectory, "Assets");

        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static string PathOf(string relativePath)
        {
            return Path.Combine(Root, relativePath);
        }

        public static Bitmap TryLoadImage(string relativePath)
        {
            Bitmap cached;
            if (Cache.TryGetValue(relativePath, out cached)) return cached;

            Bitmap result = null;
            string full = PathOf(relativePath);
            if (File.Exists(full))
            {
                // Salin ke Bitmap baru supaya file PNG tidak terkunci
                using (Bitmap tmp = new Bitmap(full))
                {
                    result = new Bitmap(tmp);
                }
            }

            Cache[relativePath] = result; // null juga disimpan, supaya tidak cek disk terus
            return result;
        }
    }
}
