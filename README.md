# Anamnesis: Dalan Mulih (WinForms + GDI+, tanpa engine)

Butuh: Windows, Visual Studio 2022 (workload ".NET desktop development") dan .NET 8 SDK.
Jalankan: buka `AnamnesisDalanMulih.csproj`, tekan F5. (Atau `dotnet run` di folder ini.)

## Kontrol
| Tombol | Fungsi |
|---|---|
| A/D atau panah | Gerak |
| Space / W / Up | Lompat (tahan = lebih tinggi) |
| E | Bicara dengan NPC, buka pintu |
| 1 / 2 / 3 | Pakai item di slot (Ramuan = +1 HP) |
| Enter / E / Space | Lanjut dialog; W/S + Enter atau tombol angka untuk memilih jawaban |
| F1 | Debug: tampilkan karma, flag, dan ending saat ini |
| Esc | Kembali ke menu |

## Karakter (semuanya kotak outline buatan kode)
- Pemain: putih, satu mata.
- NPC: berwarna (Laras cyan, Mbah Sarwo emas, Bu Lastri oranye, Kang Darman pink, Pak Lurah hijau, Raka biru), dua mata.
- Musuh: merah/oranye/ungu, dua mata + alis marah, lifebar di atas kepala. Dilukai dengan MENGINJAK dari atas (1 injakan = 1 HP).
  Sentuhan dari samping = pemain kehilangan 1 HP.

## Alur cerita
- Area 1 Hutan Lupa: ambil Liontin Zamrud dan Ramuan, bantu/abaikan Laras, serahkan liontin ke Mbah Sarwo untuk belajar menginjak, kalahkan boneka latihan, pintu terbuka.
- Area 2 Desa Tanpa Suara: surat Bu Lastri, godaan Kang Darman, Pak Lurah, mini-boss Rasa Tidak Aman (HP 5) membawa Kunci Gerbang. Kunci wajib untuk membuka gerbang.
- Area 3 Gerbang Pulang: Raka yang terluka, lalu boss Depresi (HP 8). Pintu terakhir menuju ending.
- Perpindahan area permanen (tidak bisa kembali). HP pulih di tiap checkpoint dan di awal area.

## Karma dan ending
Ada 7 titik pilihan (Laras, Lastri, Darman, Lurah, Rasa Tidak Aman, Raka, Depresi), masing-masing +1 / -1 / 0.
- Karma >= 2: Good Samaritan, Karma <= -2: Criminal, selain itu: Indecisive.
- HP habis -> Game Over: "Coba lagi" (ulang area) atau "Menyerah" (= Bad Ending "Terlupakan").
- Setelah ending, layar credits tampil otomatis. Credits juga ada di menu.

## Inventori
3 slot. Kalau penuh, pemain memilih item yang dibuang (item hadiah NPC wajib membuang sesuatu, item di lantai boleh ditinggalkan).
Liontin dan Kunci tidak bisa dibuang (mencegah gim buntu).

## Aset PNG (opsional, tanpa PNG semua digambar sebagai outline)
Yang saat ini membaca PNG: pemain dan tile.
| File | Isi |
|---|---|
| Assets/Sprites/player_idle.png, player_run.png, player_jump.png | strip horizontal, frame 16x16 |
| Assets/Tiles/tileset.png | strip horizontal tile 16x16: tile 1 = tanah, tile 2 = duri |
NPC dan musuh masih memakai kotak outline (belum membaca PNG).

## Mengedit cerita / level
- Semua dialog, posisi NPC, musuh, item, pintu: `Game/Areas.cs`.
- Level: `Assets/Levels/area1.csv` .. `area3.csv` (0 kosong, 1 padat, 2 duri). Tinggi lompat maksimal sekitar 47 px, jadi rintangan maksimal 2 tile.
- Batas aman teks: sekitar 150 huruf per kotak dialog, 28 huruf per pilihan.

## Struktur
- Engine/: GameWindow (loop + render), InputState, Tilemap, Camera, Animation, AssetManager, Ui
- Game/: Player, Enemy (+Combat), Body, BoxSprite, Dialog, Items (+GameState), Entities, Areas
- Scenes/: MenuScene, IntroScene, PlayScene, GameOverScene, EndingScene, CreditsScene
