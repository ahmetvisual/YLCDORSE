using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class FirmaBilgileriModel
    {
        public int Id { get; set; } = 1;             // Singleton: her zaman 1
        public string TamUnvan { get; set; } = "";
        public string KisaUnvan { get; set; } = "";
        public string VergiDairesi { get; set; } = "";
        public string VergiNo { get; set; } = "";
        public string MersisNo { get; set; } = "";
        public string TicaretSicilNo { get; set; } = "";
        public string AdresSatir1 { get; set; } = "";
        public string AdresSatir2 { get; set; } = "";
        public string Sehir { get; set; } = "";
        public string Ulke { get; set; } = "";
        public string PostaKodu { get; set; } = "";
        public string Telefon { get; set; } = "";
        public string Faks { get; set; } = "";
        public string Email { get; set; } = "";
        public string Web { get; set; } = "";
        public byte[]? LogoBytes { get; set; }
        public string? LogoMime { get; set; }
        public byte[]? KapakFotoBytes { get; set; }
        public string? KapakFotoMime { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
        public string GuncelleyenKullanici { get; set; } = "";
    }

    public class FirmaHesabiModel
    {
        public int Id { get; set; }
        public string BankaAdi { get; set; } = "";
        public string ParaBirimi { get; set; } = "TL";
        public string Sube { get; set; } = "";
        public string IBAN { get; set; } = "";
        public string HesapNo { get; set; } = "";
        public string SwiftKodu { get; set; } = "";
        public bool AktifMi { get; set; } = true;
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// Firma (sirket) bilgileri ve banka hesaplari (IBAN listesi). Singleton firma kaydi
    /// (Id=1 sabit) + 1:N hesap satirlari. Quote PDF'ine placeholder olarak yansir.
    /// </summary>
    public class FirmaService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private bool _schemaEnsured;
        private static readonly SemaphoreSlim _schemaLock = new(1, 1);

        public FirmaService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            await _schemaLock.WaitAsync();
            try
            {
                if (_schemaEnsured) return;
                _schemaEnsured = true;

                var steps = new[]
                {
                    """
                    CREATE TABLE IF NOT EXISTS "YLFirmaBilgileri" (
                        "Id"                    INTEGER PRIMARY KEY,
                        "TamUnvan"              TEXT NOT NULL DEFAULT '',
                        "KisaUnvan"             TEXT NOT NULL DEFAULT '',
                        "VergiDairesi"          TEXT NOT NULL DEFAULT '',
                        "VergiNo"               TEXT NOT NULL DEFAULT '',
                        "MersisNo"              TEXT NOT NULL DEFAULT '',
                        "TicaretSicilNo"        TEXT NOT NULL DEFAULT '',
                        "AdresSatir1"           TEXT NOT NULL DEFAULT '',
                        "AdresSatir2"           TEXT NOT NULL DEFAULT '',
                        "Sehir"                 TEXT NOT NULL DEFAULT '',
                        "Ulke"                  TEXT NOT NULL DEFAULT '',
                        "PostaKodu"             TEXT NOT NULL DEFAULT '',
                        "Telefon"               TEXT NOT NULL DEFAULT '',
                        "Faks"                  TEXT NOT NULL DEFAULT '',
                        "Email"                 TEXT NOT NULL DEFAULT '',
                        "Web"                   TEXT NOT NULL DEFAULT '',
                        "LogoBytes"             BYTEA,
                        "LogoMime"              TEXT,
                        "KapakFotoBytes"        BYTEA,
                        "KapakFotoMime"         TEXT,
                        "GuncellenmeTarihi"     TIMESTAMP,
                        "GuncelleyenKullanici"  TEXT NOT NULL DEFAULT ''
                    )
                    """,
                    """
                    INSERT INTO "YLFirmaBilgileri" ("Id") VALUES (1)
                    ON CONFLICT ("Id") DO NOTHING
                    """,
                    """
                    CREATE TABLE IF NOT EXISTS "YLFirmaHesaplari" (
                        "Id"         SERIAL PRIMARY KEY,
                        "BankaAdi"   TEXT NOT NULL DEFAULT '',
                        "ParaBirimi" TEXT NOT NULL DEFAULT 'TL',
                        "Sube"       TEXT NOT NULL DEFAULT '',
                        "IBAN"       TEXT NOT NULL DEFAULT '',
                        "HesapNo"    TEXT NOT NULL DEFAULT '',
                        "SwiftKodu"  TEXT NOT NULL DEFAULT '',
                        "AktifMi"    BOOLEAN NOT NULL DEFAULT TRUE,
                        "SortOrder"  INTEGER NOT NULL DEFAULT 0
                    )
                    """,
                    """CREATE INDEX IF NOT EXISTS idx_ylfirmahesaplari_aktif ON "YLFirmaHesaplari"("AktifMi", "SortOrder")""",
                };

                foreach (var sql in steps)
                {
                    try
                    {
                        using var cmd = new NpgsqlCommand(sql, conn);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch { /* mevcut tablo/kolon — atla */ }
                }

                // ─── Otomatik IBAN seed — yalniz tablo bossa calisir ───────
                // Kaynak: barandrive/HESAP BILGILERI.xlsx (22 banka hesabi).
                // Kullanici UI'dan IBAN ekledi/duzenlediyse bu blok atlanir,
                // kullanicinin ozellestirmesi korunur.
                try
                {
                    using var checkCmd = new NpgsqlCommand(
                        @"SELECT COUNT(*) FROM ""YLFirmaHesaplari""", conn);
                    var rowCount = Convert.ToInt64(
                        await checkCmd.ExecuteScalarAsync() ?? 0L);
                    if (rowCount == 0)
                    {
                        const string seedSql = """
                            INSERT INTO "YLFirmaHesaplari"
                                ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
                            VALUES
                                ('GARANTİ BBVA (Dorse)', 'TL', '595 - 6297007 SİLİVRİ', 'TR440006200059500006297007', TRUE, 10),
                                ('GARANTİ BBVA (Dorse)', 'EUR', '595 - 9093069 SİLİVRİ', 'TR850006200059500009093070', TRUE, 20),
                                ('GARANTİ BBVA (Dorse)', 'USD', '595 - 9093070 SİLİVRİ', 'TR150006200059500009093069', TRUE, 30),
                                ('TÜRKİYE HALK BANKASI A.Ş. (Dorse)', 'TL', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR940001200137800010100081', TRUE, 40),
                                ('TÜRKİYE HALK BANKASI A.Ş. (Dorse)', 'EUR', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR130001200137800058100049', TRUE, 50),
                                ('TÜRKİYE HALK BANKASI A.Ş. (Dorse)', 'USD', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR580001200137800053100053', TRUE, 60),
                                ('TÜRKİYE VAKIFLAR BANKASI T.A.O. (Dorse)', 'TL', 'Hurrıyet Tekırdag Subesı-S01047', 'TR370001500158007297036415', TRUE, 70),
                                ('TÜRKİYE VAKIFLAR BANKASI T.A.O. (Dorse)', 'EUR', 'Hurrıyet Tekırdag Subesı-S01047', 'TR660001500158048018275802', TRUE, 80),
                                ('TÜRKİYE VAKIFLAR BANKASI T.A.O. (Dorse)', 'USD', 'Hurrıyet Tekırdag Subesı-S01047', 'TR750001500158048018257889', TRUE, 90),
                                ('TÜRKİYE İŞ BANKASI A.Ş. (Dorse)', 'TL', '1522 - Emlakkent Çorlu Tekirdağ Şubesi', 'TR310006400000115220138841', TRUE, 100),
                                ('TÜRKİYE İŞ BANKASI A.Ş. (Dorse)', 'EUR', '1523 - Emlakkent Çorlu Tekirdağ Şubesi', 'TR390006400000215220019382', TRUE, 110),
                                ('TÜRKİYE CUMHURİYETİ ZİRAAT BANKASI A.Ş. (Dorse)', 'TL', 'Çorlu Tekirdağ Tic. Şubesi', 'TR250001002145016074835004', TRUE, 120),
                                ('TÜRKİYE CUMHURİYETİ ZİRAAT BANKASI A.Ş. (Dorse)', 'EUR', 'Çorlu Tekirdağ Tic. Şubesi', 'TR410001002145016074835007', TRUE, 130),
                                ('TÜRKİYE CUMHURİYETİ ZİRAAT BANKASI A.Ş. (Dorse)', 'USD', 'Çorlu Tekirdağ Tic. Şubesi', 'TR680001002145016074835006', TRUE, 140),
                                ('TÜRKİYE EMLAK KATILIM BANKASI A.Ş. (Dorse)', 'TL', 'Çorlu Şube 088', 'TR820021100000073761800001', TRUE, 150),
                                ('TÜRKİYE EMLAK KATILIM BANKASI A.Ş. (Dorse)', 'EUR', 'EMLATRISXXX Swift', 'TR710021100000073761800102', TRUE, 160),
                                ('GARANTİ BBVA (Lowbed)', 'TL', '595 - 6295813 SİLİVRİ', 'TR780006200059500006295813', TRUE, 170),
                                ('GARANTİ BBVA (Lowbed)', 'EUR', '595 - 9087110 SİLİVRİ', 'TR820006200059500009087110', TRUE, 180),
                                ('TÜRKİYE HALK BANKASI A.Ş. (Lowbed)', 'TL', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR730001200137800010100459', TRUE, 190),
                                ('TÜRKİYE HALK BANKASI A.Ş. (Lowbed)', 'EUR', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR500001200137800058100159', TRUE, 200),
                                ('TÜRKİYE VAKIFLAR BANKASI T.A.O. (Lowbed)', 'TL', '158007311915411 Hurrıyet Tekırdag Subesı-S01047', 'TR170001500158007311915411', TRUE, 210),
                                ('TÜRKİYE VAKIFLAR BANKASI T.A.O. (Lowbed)', 'EUR', '158048018866429 Hurrıyet Tekırdag Subesı-S01047', 'TR340001500158048018866429', TRUE, 220);
                            """;
                        using var seedCmd = new NpgsqlCommand(seedSql, conn);
                        await seedCmd.ExecuteNonQueryAsync();
                    }
                }
                catch { /* seed hatasi: kullanici UI'dan elle ekleyebilir */ }
            }
            finally { _schemaLock.Release(); }
        }

        // ─── FIRMA BILGILERI (singleton) ─────────────────────────

        public async Task<FirmaBilgileriModel> GetFirmaAsync()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                SELECT "Id", "TamUnvan", "KisaUnvan", "VergiDairesi", "VergiNo",
                       "MersisNo", "TicaretSicilNo",
                       "AdresSatir1", "AdresSatir2", "Sehir", "Ulke", "PostaKodu",
                       "Telefon", "Faks", "Email", "Web",
                       "LogoBytes", "LogoMime", "KapakFotoBytes", "KapakFotoMime",
                       "GuncellenmeTarihi", "GuncelleyenKullanici"
                FROM "YLFirmaBilgileri" WHERE "Id" = 1
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return new FirmaBilgileriModel { Id = 1 };

            return new FirmaBilgileriModel
            {
                Id = r.GetInt32(0),
                TamUnvan = r.IsDBNull(1) ? "" : r.GetString(1),
                KisaUnvan = r.IsDBNull(2) ? "" : r.GetString(2),
                VergiDairesi = r.IsDBNull(3) ? "" : r.GetString(3),
                VergiNo = r.IsDBNull(4) ? "" : r.GetString(4),
                MersisNo = r.IsDBNull(5) ? "" : r.GetString(5),
                TicaretSicilNo = r.IsDBNull(6) ? "" : r.GetString(6),
                AdresSatir1 = r.IsDBNull(7) ? "" : r.GetString(7),
                AdresSatir2 = r.IsDBNull(8) ? "" : r.GetString(8),
                Sehir = r.IsDBNull(9) ? "" : r.GetString(9),
                Ulke = r.IsDBNull(10) ? "" : r.GetString(10),
                PostaKodu = r.IsDBNull(11) ? "" : r.GetString(11),
                Telefon = r.IsDBNull(12) ? "" : r.GetString(12),
                Faks = r.IsDBNull(13) ? "" : r.GetString(13),
                Email = r.IsDBNull(14) ? "" : r.GetString(14),
                Web = r.IsDBNull(15) ? "" : r.GetString(15),
                LogoBytes = r.IsDBNull(16) ? null : (byte[])r["LogoBytes"],
                LogoMime = r.IsDBNull(17) ? null : r.GetString(17),
                KapakFotoBytes = r.IsDBNull(18) ? null : (byte[])r["KapakFotoBytes"],
                KapakFotoMime = r.IsDBNull(19) ? null : r.GetString(19),
                GuncellenmeTarihi = r.IsDBNull(20) ? null : r.GetDateTime(20),
                GuncelleyenKullanici = r.IsDBNull(21) ? "" : r.GetString(21),
            };
        }

        public async Task SaveFirmaAsync(FirmaBilgileriModel f)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLFirmaBilgileri" SET
                    "TamUnvan" = @TamUnvan, "KisaUnvan" = @KisaUnvan,
                    "VergiDairesi" = @VergiDairesi, "VergiNo" = @VergiNo,
                    "MersisNo" = @MersisNo, "TicaretSicilNo" = @TicaretSicilNo,
                    "AdresSatir1" = @AdresSatir1, "AdresSatir2" = @AdresSatir2,
                    "Sehir" = @Sehir, "Ulke" = @Ulke, "PostaKodu" = @PostaKodu,
                    "Telefon" = @Telefon, "Faks" = @Faks, "Email" = @Email, "Web" = @Web,
                    "GuncellenmeTarihi" = NOW(), "GuncelleyenKullanici" = @user
                WHERE "Id" = 1
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("TamUnvan", f.TamUnvan ?? "");
            cmd.Parameters.AddWithValue("KisaUnvan", f.KisaUnvan ?? "");
            cmd.Parameters.AddWithValue("VergiDairesi", f.VergiDairesi ?? "");
            cmd.Parameters.AddWithValue("VergiNo", f.VergiNo ?? "");
            cmd.Parameters.AddWithValue("MersisNo", f.MersisNo ?? "");
            cmd.Parameters.AddWithValue("TicaretSicilNo", f.TicaretSicilNo ?? "");
            cmd.Parameters.AddWithValue("AdresSatir1", f.AdresSatir1 ?? "");
            cmd.Parameters.AddWithValue("AdresSatir2", f.AdresSatir2 ?? "");
            cmd.Parameters.AddWithValue("Sehir", f.Sehir ?? "");
            cmd.Parameters.AddWithValue("Ulke", f.Ulke ?? "");
            cmd.Parameters.AddWithValue("PostaKodu", f.PostaKodu ?? "");
            cmd.Parameters.AddWithValue("Telefon", f.Telefon ?? "");
            cmd.Parameters.AddWithValue("Faks", f.Faks ?? "");
            cmd.Parameters.AddWithValue("Email", f.Email ?? "");
            cmd.Parameters.AddWithValue("Web", f.Web ?? "");
            cmd.Parameters.AddWithValue("user", _auth.FullName ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveLogoAsync(byte[]? bytes, string? mime)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLFirmaBilgileri"
                SET "LogoBytes" = @bytes, "LogoMime" = @mime,
                    "GuncellenmeTarihi" = NOW(), "GuncelleyenKullanici" = @user
                WHERE "Id" = 1
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("bytes", (object?)bytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mime", (object?)mime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("user", _auth.FullName ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveKapakFotoAsync(byte[]? bytes, string? mime)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLFirmaBilgileri"
                SET "KapakFotoBytes" = @bytes, "KapakFotoMime" = @mime,
                    "GuncellenmeTarihi" = NOW(), "GuncelleyenKullanici" = @user
                WHERE "Id" = 1
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("bytes", (object?)bytes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("mime", (object?)mime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("user", _auth.FullName ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── BANKA HESAPLARI (1:N) ────────────────────────────────

        public async Task<List<FirmaHesabiModel>> GetHesaplarAsync(bool onlyActive = false)
        {
            var list = new List<FirmaHesabiModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            var sql = """
                SELECT "Id", "BankaAdi", "ParaBirimi", "Sube", "IBAN", "HesapNo",
                       "SwiftKodu", "AktifMi", "SortOrder"
                FROM "YLFirmaHesaplari"
                """;
            if (onlyActive) sql += """ WHERE "AktifMi" = TRUE""";
            sql += """ ORDER BY "SortOrder", "Id" """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new FirmaHesabiModel
                {
                    Id = r.GetInt32(0),
                    BankaAdi = r.GetString(1),
                    ParaBirimi = r.GetString(2),
                    Sube = r.GetString(3),
                    IBAN = r.GetString(4),
                    HesapNo = r.GetString(5),
                    SwiftKodu = r.GetString(6),
                    AktifMi = r.GetBoolean(7),
                    SortOrder = r.GetInt32(8),
                });
            }
            return list;
        }

        public async Task SaveHesapAsync(FirmaHesabiModel h)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            if (h.Id > 0)
            {
                const string sql = """
                    UPDATE "YLFirmaHesaplari" SET
                        "BankaAdi" = @BankaAdi, "ParaBirimi" = @ParaBirimi,
                        "Sube" = @Sube, "IBAN" = @IBAN, "HesapNo" = @HesapNo,
                        "SwiftKodu" = @SwiftKodu, "AktifMi" = @AktifMi, "SortOrder" = @SortOrder
                    WHERE "Id" = @Id
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("Id", h.Id);
                AddHesapParams(cmd, h);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                const string sql = """
                    INSERT INTO "YLFirmaHesaplari"
                        ("BankaAdi","ParaBirimi","Sube","IBAN","HesapNo","SwiftKodu","AktifMi","SortOrder")
                    VALUES (@BankaAdi,@ParaBirimi,@Sube,@IBAN,@HesapNo,@SwiftKodu,@AktifMi,@SortOrder)
                    RETURNING "Id"
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                AddHesapParams(cmd, h);
                var result = await cmd.ExecuteScalarAsync();
                h.Id = Convert.ToInt32(result);
            }
        }

        private static void AddHesapParams(NpgsqlCommand cmd, FirmaHesabiModel h)
        {
            cmd.Parameters.AddWithValue("BankaAdi", h.BankaAdi ?? "");
            cmd.Parameters.AddWithValue("ParaBirimi", h.ParaBirimi ?? "TL");
            cmd.Parameters.AddWithValue("Sube", h.Sube ?? "");
            cmd.Parameters.AddWithValue("IBAN", h.IBAN ?? "");
            cmd.Parameters.AddWithValue("HesapNo", h.HesapNo ?? "");
            cmd.Parameters.AddWithValue("SwiftKodu", h.SwiftKodu ?? "");
            cmd.Parameters.AddWithValue("AktifMi", h.AktifMi);
            cmd.Parameters.AddWithValue("SortOrder", h.SortOrder);
        }

        public async Task DeleteHesapAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("""DELETE FROM "YLFirmaHesaplari" WHERE "Id" = @id""", conn);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Birden fazla hesabin SortOrder'larini toplu gunceller (drag-reorder sonrasi).</summary>
        public async Task UpdateSortOrdersAsync(IEnumerable<(int Id, int Sort)> rows)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                foreach (var (id, sort) in rows)
                {
                    using var cmd = new NpgsqlCommand("""UPDATE "YLFirmaHesaplari" SET "SortOrder" = @s WHERE "Id" = @id""", conn, tx);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("s", sort);
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        }
    }
}
