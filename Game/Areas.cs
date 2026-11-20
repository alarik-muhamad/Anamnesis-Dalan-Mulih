using System;
using System.Drawing;

namespace AnamnesisDalanMulih.Game
{
    /// <summary>
    /// Cerita + isi tiga area. Semua teks dialog ada di sini, silakan diedit.
    /// Karma: tiap pilihan moral memanggil AddKarma(+1 / -1). Ending dihitung dari total karma.
    /// Batas aman teks: sekitar 150 karakter per kotak dialog, 28 karakter per pilihan.
    /// </summary>
    public static class Areas
    {
        public const int Count = 3;

        // Tanah ada di baris 10 (y = 160). Posisi dalam tile.
        private const int GroundRow = 10;
        private static float TileX(int col) { return col * 16f; }
        private static float Feet(int row, int h) { return row * 16f - h; }

        public static AreaDef Build(int index)
        {
            switch (index)
            {
                case 0: return Area1();
                case 1: return Area2();
                default: return Area3();
            }
        }

        // ---------------------------------------------------------------- helper
        private static Enemy Cemas(int col, int minCol, int maxCol, int row)
        {
            Enemy e = new Enemy("Cemas", TileX(col) + 2f, Feet(row, 12), 12, 12, 2, 28f, Palette.Cemas);
            e.MinX = TileX(minCol);
            e.MaxX = TileX(maxCol + 1);
            return e;
        }

        private static Trigger Zone(string flag, int col, int widthCols, Func<GameState, DialogNode> dialog)
        {
            return new Trigger(flag, new Rectangle((int)TileX(col), 0, widthCols * 16, 192), dialog);
        }

        private static DialogNode Whisper(params string[] lines)
        {
            DialogNode[] nodes = new DialogNode[lines.Length];
            for (int i = 0; i < lines.Length; i++) nodes[i] = Dlg.Say("Bisikan", lines[i]);
            return Dlg.Chain(nodes);
        }

        // ================================================================ AREA 1
        private static AreaDef Area1()
        {
            AreaDef a = new AreaDef();
            a.Title = "AREA 1 - HUTAN LUPA";
            a.Level = "Levels/area1.csv";
            a.Bg = Color.FromArgb(12, 20, 24);
            a.Intro = new string[]
            {
                "Gelap. Dingin. Tanah lembap di bawah punggungmu.",
                "Kamu tidak ingat namamu. Tidak ingat dari mana kamu datang.",
                "Hanya satu bisikan yang terus berulang: \"Mulih... jalan pulang.\""
            };
            a.SpawnX = TileX(1) + 3f;
            a.SpawnY = Feet(GroundRow, Player.Height);

            // Cerita & tutorial
            a.Triggers.Add(Zone("a1_wake", 0, 5, s => Whisper(
                "Bangun... dengarkan aku.",
                "Mulih. Jalan pulang ada di timur. Ikuti aku.",
                "(A/D atau panah: berjalan. Space: melompat. E: bicara.)")));
            a.Triggers.Add(Zone("a1_pendant", 8, 2, s => Whisper(
                "Ada yang berkilau di atas sana. Kamu... pernah memilikinya.",
                "Kamu hanya sanggup membawa tiga barang. Pilihlah dengan bijak.")));

            // Item
            a.Pickups.Add(new Pickup(ItemId.EmeraldPendant, TileX(12) + 3f, 8 * 16f - Pickup.Size - 1f));
            a.Pickups.Add(new Pickup(ItemId.Herb, TileX(32) + 3f, 6 * 16f - Pickup.Size - 1f));

            // Checkpoint
            a.Checkpoints.Add(new Checkpoint(TileX(8) + 8f, GroundRow * 16f));
            a.Checkpoints.Add(new Checkpoint(TileX(38) + 8f, GroundRow * 16f));

            // NPC
            a.Npcs.Add(new Npc("Laras", TileX(40) + 3f, Feet(GroundRow, Npc.H), Palette.Laras, LarasDialog));
            a.Npcs.Add(new Npc("Mbah Sarwo", TileX(57) + 3f, Feet(GroundRow, Npc.H), Palette.Sarwo, SarwoDialog));

            // Boneka latihan: tidak berbahaya, 3 injakan untuk mengalahkannya
            Enemy dummy = new Enemy("Bayangan Latihan", TileX(52) + 2f, Feet(GroundRow, 12), 12, 12, 3, 0f, Palette.Dummy);
            dummy.Harmless = true;
            dummy.OnDefeat = h =>
            {
                h.State.Set("dummy_done");
                if (h.State.Flag("learned"))
                    h.StartDialog(Dlg.Say("Mbah Sarwo", "Tiga injakan, tiga luka. Kau cepat belajar. Pintu timur terbuka sekarang."));
                else
                    h.StartDialog(Dlg.Say("Mbah Sarwo", "Kau sudah paham caranya sendiri. Tapi pintu itu hanya mengenali yang mau belajar. Bicaralah padaku."));
            };
            a.Enemies.Add(dummy);

            // Pintu ke Area 2
            a.Doors.Add(new Door(TileX(61), GroundRow * 16f - Door.H, 1,
                s => s.Flag("learned") && s.Flag("dummy_done"),
                Dlg.Say("Bisikan", "Pintu ini tidak mau terbuka. Mungkin ada yang harus kamu pelajari dulu.")));
            return a;
        }

        private static DialogNode LarasDialog(GameState s)
        {
            if (s.Flag("laras_done"))
                return Dlg.Say("Laras", "Terima kasih sudah mampir. Hati-hati di depan, ya.");

            DialogNode intro = Dlg.Say("Laras", "Aduh... kakiku terkilir. Berhari-hari aku tersesat di hutan ini. Kamu juga?");
            if (!s.Has(ItemId.Herb))
                return Dlg.Chain(intro, Dlg.Say("Laras", "Kalau ada ramuan, aku butuh sekali. Tadi kulihat ada yang tumbuh di ranting tinggi, di sebelah barat."));

            DialogNode ask = Dlg.Say("Laras", "Itu ramuan, ya? Aku sangat membutuhkannya. Tapi itu milikmu, aku tak berhak memaksa.");
            DialogNode thanks = Dlg.Say("Laras", "Terima kasih. Namaku Laras. Kalau kamu menemukan jalan pulang, ceritakan padaku.");
            DialogNode cold = Dlg.Say("Laras", "...Begitu ya. Aku mengerti. Semoga kamu sampai.");

            ask.Choices.Add(new DialogChoice("Berikan ramuan", h =>
            {
                h.State.Remove(ItemId.Herb);
                h.State.AddKarma(1);
                h.State.Set("laras_done");
            }, thanks));
            ask.Choices.Add(new DialogChoice("Simpan ramuan", h =>
            {
                h.State.AddKarma(-1);
                h.State.Set("laras_done");
            }, cold));
            ask.Choices.Add(new DialogChoice("Nanti dulu", null, null));

            intro.Next = ask;
            return intro;
        }

        private static DialogNode SarwoDialog(GameState s)
        {
            if (s.Flag("learned") && s.Flag("dummy_done"))
                return Dlg.Say("Mbah Sarwo", "Pergilah. Di depan, bayangan-bayangan itu bukan musuh biasa. Mereka lahir dari dalam dirimu.");

            if (s.Flag("learned"))
                return Dlg.Chain(
                    Dlg.Say("Mbah Sarwo", "Injak kepala bayangan dari atas. Satu injakan, satu luka. Jangan sentuh dari samping."),
                    Dlg.Say("Mbah Sarwo", "Coba pada bayangan latihan di sana. Tiga injakan cukup."));

            if (s.Has(ItemId.EmeraldPendant))
            {
                DialogNode n1 = Dlg.Say("Mbah Sarwo", "Liontin zamrud itu... dari mana kamu dapat? Simpan dulu ceritanya.");
                DialogNode n2 = Dlg.Say("Mbah Sarwo", "Aku bisa mengajarimu bertarung. Imbalannya liontin itu. Ia terlalu berat dibawa tanpa tahu asalnya.");
                DialogNode learn = Dlg.Chain(
                    Dlg.Say("Mbah Sarwo", "Dengar: bayangan hanya bisa dilukai dari atas. Lompat, injak, ulangi."),
                    Dlg.Say("Mbah Sarwo", "Coba pada bayangan latihan di sana. Bayangan sungguhan akan melukaimu."));
                DialogNode refuse = Dlg.Say("Mbah Sarwo", "Tanpa ilmu, pintu timur takkan terbuka. Pikirkan lagi.");

                n2.Choices.Add(new DialogChoice("Serahkan liontin", h =>
                {
                    h.State.Remove(ItemId.EmeraldPendant);
                    h.State.Set("learned");
                    h.Toast("Liontin diserahkan");
                }, learn));
                n2.Choices.Add(new DialogChoice("Simpan liontin", null, refuse));
                n1.Next = n2;
                return n1;
            }

            return Dlg.Chain(
                Dlg.Say("Mbah Sarwo", "Kau belum membawa sesuatu yang berarti. Ada yang berkilau di hutan ini. Carilah."),
                Dlg.Say("Mbah Sarwo", "Tanpa itu, aku tak punya alasan untuk mengajarimu."));
        }

        // ================================================================ AREA 2
        private static AreaDef Area2()
        {
            AreaDef a = new AreaDef();
            a.Title = "AREA 2 - DESA TANPA SUARA";
            a.Level = "Levels/area2.csv";
            a.Bg = Color.FromArgb(24, 18, 22);
            a.Intro = new string[]
            {
                "Pintu itu menutup di belakangmu. Kamu tidak bisa kembali.",
                "Di depan, sebuah desa. Lampu-lampunya menyala, tapi tak ada suara."
            };
            a.SpawnX = TileX(1) + 3f;
            a.SpawnY = Feet(GroundRow, Player.Height);

            a.Triggers.Add(Zone("a2_start", 0, 5, s => Whisper(
                "Desa ini menyala, tapi tak bersuara.",
                "Orang-orangnya masih di sini. Mungkin mereka hanya takut.")));
            a.Triggers.Add(Zone("a2_arena", 70, 2, s => Dlg.Chain(
                Dlg.Say("Rasa Tidak Aman", "J-jangan mendekat! Aku tidak cukup baik untuk dilihat siapa pun."),
                Dlg.Say("Bisikan", "Ia memegang kunci gerbang. Injak ia dari atas."))));

            a.Checkpoints.Add(new Checkpoint(TileX(6) + 8f, GroundRow * 16f));
            a.Checkpoints.Add(new Checkpoint(TileX(40) + 8f, GroundRow * 16f));
            a.Checkpoints.Add(new Checkpoint(TileX(64) + 8f, GroundRow * 16f));

            a.Pickups.Add(new Pickup(ItemId.Herb, TileX(51) + 3f, 6 * 16f - Pickup.Size - 1f));

            a.Npcs.Add(new Npc("Bu Lastri", TileX(12) + 3f, Feet(GroundRow, Npc.H), Palette.Lastri, LastriDialog));
            a.Npcs.Add(new Npc("Kang Darman", TileX(56) + 3f, Feet(GroundRow, Npc.H), Palette.Darman, DarmanDialog));
            a.Npcs.Add(new Npc("Pak Lurah", TileX(67) + 3f, Feet(GroundRow, Npc.H), Palette.Lurah, LurahDialog));

            a.Enemies.Add(Cemas(19, 15, 25, GroundRow));
            a.Enemies.Add(Cemas(46, 42, 53, GroundRow));

            // Mini-boss: Rasa Tidak Aman
            Enemy boss = new Enemy("Rasa Tidak Aman", TileX(76), Feet(GroundRow, 16), 16, 16, 5, 38f, Palette.TidakAman);
            boss.Hops = true;
            boss.MinX = TileX(71);
            boss.MaxX = TileX(81);
            boss.OnDefeat = InsecurityDefeated;
            a.Enemies.Add(boss);

            a.Doors.Add(new Door(TileX(82), GroundRow * 16f - Door.H, 2,
                s => s.Has(ItemId.GateKey),
                Dlg.Say("Bisikan", "Gerbang ini terkunci. Kuncinya dipegang bayangan penakut di depan.")));
            return a;
        }

        private static void InsecurityDefeated(IGameHost h)
        {
            DialogNode keyNode = Dlg.Say("Bisikan", "Sebuah kunci jatuh ke tanah. Hangat, seolah baru dilepas dari genggaman.");
            keyNode.OnEnter = host => host.GiveItem(ItemId.GateKey);

            DialogNode spared = Dlg.Say("Rasa Tidak Aman", "Terima kasih... Ambil kuncinya. Aku tak pantas menyimpannya.");
            DialogNode killed = Dlg.Say("Bisikan", "Bayangan itu lenyap tanpa suara. Tak ada yang menangis untuknya.");
            spared.Next = keyNode;
            killed.Next = keyNode;

            DialogNode ask = Dlg.Say("Rasa Tidak Aman", "Aku... hanya takut. Kamu juga takut, kan? Tolong... jangan akhiri aku.");
            ask.Choices.Add(new DialogChoice("Ampuni dia", hh => hh.State.AddKarma(1), spared));
            ask.Choices.Add(new DialogChoice("Habisi dia", hh => hh.State.AddKarma(-1), killed));
            h.StartDialog(ask);
        }

        private static DialogNode LastriDialog(GameState s)
        {
            if (s.Flag("lastri_asked"))
            {
                if (s.Has(ItemId.Letter))
                    return Dlg.Say("Bu Lastri", "Pak Lurah ada di ujung desa. Sampaikan salamku, ya.");
                if (s.Flag("letter_delivered"))
                    return Dlg.Say("Bu Lastri", "Suratku sudah sampai? Syukurlah. Kamu anak yang baik.");
                return Dlg.Say("Bu Lastri", "Suratku... tak sampai, ya? Ah, sudahlah. Mungkin memang bukan rezekiku.");
            }

            DialogNode n1 = Dlg.Say("Bu Lastri", "Nak, kamu dari arah hutan? Desa ini sepi sejak bayangan-bayangan itu datang.");
            DialogNode n2 = Dlg.Say("Bu Lastri", "Aku punya surat untuk Pak Lurah di ujung desa, tapi kakiku tak kuat berjalan. Maukah kamu mengantarkannya?");
            DialogNode yes = Dlg.Say("Bu Lastri", "Terima kasih, Nak. Hati-hati dengan Kang Darman, ia selalu ingin tahu urusan orang.");
            DialogNode no = Dlg.Say("Bu Lastri", "Ya sudah. Semua orang punya urusan masing-masing.");

            n2.Choices.Add(new DialogChoice("Antarkan suratnya", h =>
            {
                h.State.Set("lastri_asked");
                h.State.AddKarma(1);
                h.GiveItem(ItemId.Letter);
            }, yes));
            n2.Choices.Add(new DialogChoice("Bukan urusanku", h =>
            {
                h.State.Set("lastri_asked");
                h.State.AddKarma(-1);
            }, no));
            n2.Choices.Add(new DialogChoice("Nanti dulu", null, null));

            n1.Next = n2;
            return n1;
        }

        private static DialogNode DarmanDialog(GameState s)
        {
            if (!s.Has(ItemId.Letter))
            {
                if (s.Flag("letter_sold"))
                    return Dlg.Say("Kang Darman", "Transaksi yang menyenangkan. Jangan bilang siapa-siapa, ya.");
                return Dlg.Say("Kang Darman", "Hmph. Warung Lastri laku, daganganku sepi. Kalau ada yang bisa bantu... ah, sudahlah.");
            }

            if (s.Flag("darman_refused"))
                return Dlg.Say("Kang Darman", "Masih bawa surat itu? Dasar keras kepala.");

            DialogNode n1 = Dlg.Say("Kang Darman", "Heh, itu surat Lastri, kan? Isinya minta bantuan ke Pak Lurah. Kalau sampai, aku yang rugi.");
            DialogNode n2 = Dlg.Say("Kang Darman", "Jual padaku. Kuberi satu ramuan. Tak ada yang akan tahu.");
            DialogNode sold = Dlg.Say("Kang Darman", "Pintar. Dunia ini milik yang berani mengambil.");
            DialogNode refused = Dlg.Say("Kang Darman", "Cih. Sok suci.");

            n2.Choices.Add(new DialogChoice("Jual suratnya", h =>
            {
                h.State.Remove(ItemId.Letter);
                h.State.AddKarma(-1);
                h.State.Set("letter_sold");
                h.GiveItem(ItemId.Herb);
            }, sold));
            n2.Choices.Add(new DialogChoice("Tolak", h =>
            {
                h.State.AddKarma(1);
                h.State.Set("darman_refused");
            }, refused));
            n2.Choices.Add(new DialogChoice("Nanti dulu", null, null));

            n1.Next = n2;
            return n1;
        }

        private static DialogNode LurahDialog(GameState s)
        {
            DialogNode story = Dlg.Chain(
                Dlg.Say("Pak Lurah", "Desa ini kehilangan suaranya sejak bayangan-bayangan datang. Yang paling penakut membawa kunci gerbang timur."),
                Dlg.Say("Pak Lurah", "Injak ia dari atas. Jangan beri ia ruang untuk bersembunyi."));

            if (!s.Has(ItemId.Letter) || s.Flag("letter_delivered"))
            {
                if (s.Flag("letter_delivered"))
                    return Dlg.Say("Pak Lurah", "Surat Lastri sudah kubaca. Kamu orang baik. Gerbangnya ada di ujung sana.");
                return story;
            }

            DialogNode ask = Dlg.Say("Pak Lurah", "Eh, itu surat dari Lastri? Berikan padaku, Nak.");
            DialogNode thanks = Dlg.Say("Pak Lurah", "Terima kasih. Ini sedikit ramuan sebagai balasan.");
            ask.Choices.Add(new DialogChoice("Serahkan surat", h =>
            {
                h.State.Remove(ItemId.Letter);
                h.State.AddKarma(1);
                h.State.Set("letter_delivered");
                h.GiveItem(ItemId.Herb);
            }, thanks));
            ask.Choices.Add(new DialogChoice("Simpan dulu", null, null));

            story.Next.Next = ask;
            return story;
        }

        // ================================================================ AREA 3
        private static AreaDef Area3()
        {
            AreaDef a = new AreaDef();
            a.Title = "AREA 3 - GERBANG PULANG";
            a.Level = "Levels/area3.csv";
            a.Bg = Color.FromArgb(16, 14, 30);
            a.Intro = new string[]
            {
                "Kunci itu hangat di tanganmu. Atau mungkin tanganmu yang dingin.",
                "Bisikan itu semakin dekat. Hampir tidak lagi terdengar seperti bisikan."
            };
            a.SpawnX = TileX(1) + 3f;
            a.SpawnY = Feet(GroundRow, Player.Height);

            a.Triggers.Add(Zone("a3_start", 0, 5, s => Whisper(
                "Hampir sampai. Kenapa langkahmu terasa begitu berat?",
                "Di ujung jalan ini, kamu akan tahu siapa dirimu.")));
            a.Triggers.Add(Zone("a3_boss", 50, 2, s => Dlg.Chain(
                Dlg.Say("Bisikan", "Di sinilah aku berhenti bicara. Dan dia mulai."),
                Dlg.Say("Depresi", "Kenapa kamu terus berjalan? Tidak ada yang menunggumu di sana."),
                Dlg.Say("Depresi", "Tinggal saja. Tidurlah. Semuanya lebih tenang begitu."))));

            a.Checkpoints.Add(new Checkpoint(TileX(4) + 8f, GroundRow * 16f));
            a.Checkpoints.Add(new Checkpoint(TileX(38) + 8f, GroundRow * 16f));
            a.Checkpoints.Add(new Checkpoint(TileX(49) + 8f, GroundRow * 16f));

            a.Pickups.Add(new Pickup(ItemId.Herb, TileX(14) + 3f, 6 * 16f - Pickup.Size - 1f));

            a.Npcs.Add(new Npc("Raka", TileX(19) + 3f, Feet(GroundRow, Npc.H), Palette.Raka, RakaDialog));

            a.Enemies.Add(Cemas(12, 9, 16, GroundRow));
            a.Enemies.Add(Cemas(30, 26, 35, GroundRow));
            a.Enemies.Add(Cemas(44, 40, 47, GroundRow));

            // Boss terakhir: Depresi. Besar, lambat, dan melompat-lompat.
            Enemy boss = new Enemy("Depresi", TileX(60), Feet(GroundRow, 24), 24, 24, 8, 32f, Palette.Depresi);
            boss.Hops = true;
            boss.MinX = TileX(52);
            boss.MaxX = TileX(68);
            boss.OnDefeat = DepressionDefeated;
            a.Enemies.Add(boss);

            a.Doors.Add(new Door(TileX(70), GroundRow * 16f - Door.H, -1,
                s => s.Flag("boss_done"),
                Dlg.Say("Bisikan", "Pintu ini bergetar. Bayangan itu masih menghalangi jalan pulang.")));
            return a;
        }

        private static void DepressionDefeated(IGameHost h)
        {
            DialogNode open = Dlg.Say("Bisikan", "Pintu pulang terbuka. Tinggal satu langkah lagi.");
            open.OnEnter = host => host.State.Set("boss_done");

            DialogNode hug = Dlg.Say("Depresi", "...Hangat. Ternyata aku selalu bagian darimu.");
            DialogNode smash = Dlg.Say("Bisikan", "Bayangan itu pecah berkeping. Sunyi sekali setelahnya.");
            DialogNode pass = Dlg.Say("Depresi", "Kamu memilih untuk tidak memilih. Itu pun sebuah pilihan.");
            hug.Next = open;
            smash.Next = open;
            pass.Next = open;

            DialogNode ask = Dlg.Say("Depresi", "Aku... hanya ingin kamu berhenti merasa sakit. Itu saja.");
            ask.Choices.Add(new DialogChoice("Peluk bayangan itu", hh => hh.State.AddKarma(1), hug));
            ask.Choices.Add(new DialogChoice("Hancurkan dia", hh => hh.State.AddKarma(-1), smash));
            ask.Choices.Add(new DialogChoice("Lewati saja", null, pass));
            h.StartDialog(ask);
        }

        private static DialogNode RakaDialog(GameState s)
        {
            if (s.Flag("raka_done"))
                return Dlg.Say("Raka", "Pergilah. Jangan biarkan bayangan itu menemukanmu juga.");

            DialogNode ask = Dlg.Say("Raka", "Uhh... bayangan di depan menghajarku. Ramuanku habis. Tolong...");
            DialogNode thanks = Dlg.Say("Raka", "Terima kasih... Kamu orang baik. Ingat: bayangan terakhir itu bukan musuh biasa.");
            DialogNode loot = Dlg.Say("Raka", "Kau... kau tega. Ambil saja semuanya.");
            DialogNode sorry = Dlg.Say("Raka", "Ya... ya sudah. Aku mengerti.");

            DialogChoice give = new DialogChoice("Berikan ramuan", h =>
            {
                h.State.Remove(ItemId.Herb);
                h.State.AddKarma(1);
                h.State.Set("raka_done");
            }, thanks);
            give.Condition = st => st.Has(ItemId.Herb);

            ask.Choices.Add(give);
            ask.Choices.Add(new DialogChoice("Ambil ramuannya", h =>
            {
                h.State.AddKarma(-1);
                h.State.Set("raka_done");
                h.GiveItem(ItemId.Herb);
            }, loot));
            ask.Choices.Add(new DialogChoice("Maaf, tak bisa", h => h.State.Set("raka_done"), sorry));
            return ask;
        }
    }
}
