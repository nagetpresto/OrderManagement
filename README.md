# Order Management API

REST API untuk order management dengan ASP.NET Core (.NET 10), Clean Architecture, dan PostgreSQL.

Fokus: concurrency.

## Cara menjalankan

Butuh .NET 10 SDK dan Docker.

```bash
docker compose up -d

dotnet tool install --global dotnet-ef
dotnet ef database update --project src/Adaya.Orders.Infrastructure --startup-project src/Adaya.Orders.Api

dotnet run --project src/Adaya.Orders.Api --urls http://localhost:5080
```

Endpoint bisa dicoba di http://localhost:5080/scalar/v1

## Database

Kenapa PostgreSQL? Karena punya yang dibutuhkan untuk case concurrency:

- row lock per baris: digunakan ketika ada request bersamaan di baris yg sama
- kolom `xmin`: otomatis berubah setiap baris diupdate, jadi bisa dipakai untuk mendeteksi kalau baris sudah diubah orang lain
- unique index dan check constraint: penjagaan langsung dari database

SQLite tidak dipilih karena kuncinya per database, bukan per baris.
<br>
SQL Server tidak dipilih karena butuh instalasi/lisensi tersendiri, sedangkan PostgreSQL gratis dan bisa jalan lewat Docker.

## Idempotency

Menggunakan header `Idempotency-Key` + ada unique index `(CustomerId, IdempotencyKey)` --> satu key cuma bisa punya satu order.

Kalau key yang sama dikirim lagi:
- isinya sama: order yang sudah ada dikembalikan (`200`), stok tidak berkurang lagi
- isinya beda: ditolak `422`

Kenapa:
- Hash isi request: tidak bisa bedakan klik dua kali dengan memang sengaja pesan dua kali.
- Cek dulu di kode lalu simpan: dua request bersamaan sama-sama melihat "belum ada".
- Redis: nambah sistem baru.

## Strategi concurrency

Kombinasi + semua perubahan stok dan order jalan dalam satu transaksi, jadi kalau ada yang gagal, semuanya dibatalkan.

### A: dua order rebutan stok

Stok: 15<br>Order A: minta 10<br>Order B: minta 10

Stok dikurangi dengan satu perintah SQL yang sekaligus mengecek:

```sql
UPDATE "Products"
SET "StockQuantity" = "StockQuantity" - @qty
WHERE "Id" = @id AND "StockQuantity" >= @qty;
```
Kalau ada baris yang berubah --> tandanya stok ada dan menang
<br>
Kalau tidak ada baris yang berubah --> tandanya stok kurang dan order ditolak.

Baca stok dulu tidak dipilih karena nanti dua request akan baca stok == 15

### B: dua admin mengubah status barengan

Pakai `xmin`.<br>Saat order dibaca, versinya diingat.<br>Saat disimpan, EF menambahkan syarat "versi masih sama".<br>Kalau sudah diubah orang lain, tidak ada baris yang cocok dan admin yang kalah ditolak (`409`).

### C: dua request kembar dengan key yang sama

Pakai pengecekan "key sudah ada?" + unique index.<br>Yang kalah transaksinya dibatalkan tetapi return order pemenang, jadi tidak bikin order baru.

## Race condition lain

### 1. Cancel dobel, stok kembali dua kali

Dua request cancel untuk order yang sama datang barengan, dua-duanya membaca status `Confirmed`. Tanpa pencegahan, stok bertambah dua kali.

Dicegah pakai `xmin`: cancel pertama mengunci baris order, yang kedua menunggu lalu gagal sebelum sempat mengembalikan stok. Status disimpan dulu, baru stok dikembalikan, dalam satu transaksi. Kalau cancel kedua datang setelah yang pertama selesai, ditolak karena `Cancelled` tidak bisa diubah lagi.

### 2. Deadlock

Order A berisi produk [1, 2], order B berisi [2, 1]. A mengunci produk 1 dan menunggu produk 2, B sebaliknya. Keduanya saling tunggu.

Dicegah dengan mengurutkan item berdasarkan `productId` sebelum stok diubah, jadi semua order mengunci dengan urutan yang sama.

### 3. Stok terpotong tapi order gagal tersimpan

Stok sudah dikurangi, lalu penyimpanan order gagal (misalnya kalah balapan idempotency, atau aplikasi mati). Stok jadi berkurang tanpa ada ordernya.

Dicegah dengan menaruh potong stok dan simpan order dalam satu transaksi. Kalau gagal, dua-duanya dibatalkan.

## Testing

Integration test butuh Docker menyala.

```bash
dotnet test                                      # semua
dotnet test tests/Adaya.Orders.UnitTests         # unit test saja
dotnet test tests/Adaya.Orders.IntegrationTests  # integration test saja

# satu skenario
dotnet test tests/Adaya.Orders.IntegrationTests --filter "FullyQualifiedName~ScenarioA"
dotnet test tests/Adaya.Orders.IntegrationTests --filter "FullyQualifiedName~ScenarioB"
dotnet test tests/Adaya.Orders.IntegrationTests --filter "FullyQualifiedName~ScenarioC"
dotnet test tests/Adaya.Orders.IntegrationTests --filter "FullyQualifiedName~DoubleCancel"
```

Integration test pakai PostgreSQL asli lewat Testcontainers, bukan SQLite atau database palsu, karena yang diuji adalah perilaku database-nya (row lock, `xmin`, unique index). Request ditahan dulu lalu dilepas barengan supaya balapannya benar-benar terjadi. Test tidak menebak siapa yang menang, tapi memeriksa hal yang harus selalu benar: tepat satu yang menang dan datanya konsisten.

Daftar test:
- `ChangeStatus_FollowsTransitionTable` (unit): 25 kombinasi status, 5 valid, 20 harus ditolak.
- `ScenarioA_...`: stok 15, dua order minta 10, hanya satu sukses, stok akhir 5.
- `ScenarioB_...`: Ship dan Cancel barengan, tepat satu menang, stok sesuai pemenang. Diulang 5 kali.
- `ScenarioC_...`: 5 request kembar, hanya 1 order terbentuk, stok terpotong sekali. Dites dengan stok 10 dan 100.
- `DoubleCancel_...`: dua cancel barengan, stok kembali sekali.