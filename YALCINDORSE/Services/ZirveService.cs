using Npgsql;
using Microsoft.Data.SqlClient;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    /* ═══════════════════════════════════════════════════════
       MODELLER
       ═══════════════════════════════════════════════════════ */

    public class ResmiCariModel
    {
        public int Id { get; set; }              // PG id
        public int ZirveId { get; set; }          // Arti (Zirve PK)
        public string HesapKodu { get; set; } = "";
        public string CariAdi { get; set; } = "";
        public string? Unvan { get; set; }
        public string? VergiNo { get; set; }
        public string? TCKimlikNo { get; set; }
        public string? VergiDairesi { get; set; }
        public string? VergiDairesiKodu { get; set; }
        public string? Il { get; set; }
        public string? Ilce { get; set; }
        public string? Ulke { get; set; }
        public string? Adres1 { get; set; }
        public string? Adres2 { get; set; }
        public string? Telefon1 { get; set; }
        public string? Cep { get; set; }
        public string? Email { get; set; }
        public string? Web { get; set; }
        public string? Yetkili { get; set; }
        public DateTime SonSenkronTarihi { get; set; }

        // Eslestirme
        public int? EslesmisCariId { get; set; }
        public string? EslesmisCariAdi { get; set; }  // JOIN ile dolu gelir
    }

    public class ResmiCariListItemModel
    {
        public int Id { get; set; }
        public string HesapKodu { get; set; } = "";
        public string CariAdi { get; set; } = "";
        public string? VergiNo { get; set; }
        public string? Il { get; set; }
        public int? EslesmisCariId { get; set; }
        public string? EslesmisCariAdi { get; set; }
    }

    public class SenkronSonucModel
    {
        public int Yeni { get; set; }
        public int Guncellenen { get; set; }
        public int Toplam { get; set; }
        public string? Hata { get; set; }
    }

    /* ═══════════════════════════════════════════════════════
       SERVİS
       ═══════════════════════════════════════════════════════ */

    public class ZirveService
    {
        private readonly DatabaseHelper _db;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);
        private bool _schemaEnsured;

        private const string SchemaSql = """
            CREATE TABLE IF NOT EXISTS "YLResmiCariler"
            (
                "Id"                 SERIAL PRIMARY KEY,
                "ZirveId"            INT NOT NULL,
                "HesapKodu"          VARCHAR(50) NOT NULL,
                "CariAdi"            VARCHAR(300),
                "Unvan"              VARCHAR(500),
                "VergiNo"            VARCHAR(50),
                "TCKimlikNo"         VARCHAR(20),
                "VergiDairesi"       VARCHAR(150),
                "VergiDairesiKodu"   VARCHAR(20),
                "Il"                 VARCHAR(80),
                "Ilce"               VARCHAR(80),
                "Ulke"               VARCHAR(80),
                "Adres1"             VARCHAR(500),
                "Adres2"             VARCHAR(500),
                "Telefon1"           VARCHAR(50),
                "Cep"                VARCHAR(50),
                "Email"              VARCHAR(120),
                "Web"                VARCHAR(200),
                "Yetkili"            VARCHAR(200),
                "SonSenkronTarihi"   TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "EslesmisCariId"     INT NULL,
                CONSTRAINT "FK_ResmiCari_Customer"
                    FOREIGN KEY ("EslesmisCariId")
                    REFERENCES "YLCustomers"("Id")
                    ON DELETE SET NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_YLResmiCariler_ZirveId"
                ON "YLResmiCariler" ("ZirveId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_YLResmiCariler_HesapKodu"
                ON "YLResmiCariler" ("HesapKodu");

            CREATE INDEX IF NOT EXISTS "IX_YLResmiCariler_Eslesmis"
                ON "YLResmiCariler" ("EslesmisCariId");
            """;

        public ZirveService(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task EnsureSchemaAsync()
        {
            if (_schemaEnsured) return;
            await _schemaLock.WaitAsync();
            try
            {
                if (_schemaEnsured) return;
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                try
                {
                    using var cmd = new NpgsqlCommand(SchemaSql, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { /* Tablo zaten varsa veya yetki yoksa devam */ }
                _schemaEnsured = true;
            }
            finally { _schemaLock.Release(); }
        }

        /* ═══════════════════════════════════════════════
           ZİRVE MSSQL'DEN ÇEK → POSTGRESQL'E SENKRONIZE
           ═══════════════════════════════════════════════ */

        /// <summary>
        /// Zirve MSSQL'den 120xxx carileri çeker, PostgreSQL YLResmiCariler'e UPSERT yapar.
        /// </summary>
        public async Task<SenkronSonucModel> SenkronizeEtAsync()
        {
            await EnsureSchemaAsync();
            var sonuc = new SenkronSonucModel();

            // 1) Zirve MSSQL'e baglan
            using var mssql = await _db.GetMssqlConnectionAsync();
            if (mssql == null)
            {
                sonuc.Hata = "Zirve MSSQL veritabanina baglanilamadi. Baglanti bilgilerini kontrol edin.";
                return sonuc;
            }

            // 2) 120xxx carileri çek
            var zirveCariler = new List<ResmiCariModel>();
            try
            {
                const string sql = """
                    SELECT
                        Arti,
                        Kod,
                        Cariadi,
                        Unvan,
                        Vergikimlikno,
                        Tckimlikno,
                        Vergidairesi,
                        VergiDairesiKodu,
                        Il,
                        Ilce,
                        Ulke,
                        Adres1,
                        Adres2,
                        Tel1,
                        Cep1,
                        Email,
                        Webadresi,
                        Yetkili
                    FROM dbo.hesplancariler
                    WHERE Kod LIKE '120%'
                    ORDER BY Cariadi
                    """;

                using var cmd = new SqlCommand(sql, mssql);
                cmd.CommandTimeout = 30;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    zirveCariler.Add(new ResmiCariModel
                    {
                        ZirveId       = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0)),
                        HesapKodu     = SafeStr(reader, 1),
                        CariAdi       = SafeStr(reader, 2),
                        Unvan         = SafeStrNull(reader, 3),
                        VergiNo       = SafeStrNull(reader, 4),
                        TCKimlikNo    = SafeStrNull(reader, 5),
                        VergiDairesi  = SafeStrNull(reader, 6),
                        VergiDairesiKodu = SafeStrNull(reader, 7),
                        Il            = SafeStrNull(reader, 8),
                        Ilce          = SafeStrNull(reader, 9),
                        Ulke          = SafeStrNull(reader, 10),
                        Adres1        = SafeStrNull(reader, 11),
                        Adres2        = SafeStrNull(reader, 12),
                        Telefon1      = SafeStrNull(reader, 13),
                        Cep           = SafeStrNull(reader, 14),
                        Email         = SafeStrNull(reader, 15),
                        Web           = SafeStrNull(reader, 16),
                        Yetkili       = SafeStrNull(reader, 17)
                    });
                }
            }
            catch (Exception ex)
            {
                sonuc.Hata = $"Zirve'den veri cekilemedi: {ex.Message}";
                return sonuc;
            }

            // 3) PostgreSQL'e UPSERT
            try
            {
                using var pg = _db.GetConnection();
                await pg.OpenAsync();

                // Mevcut ZirveId'leri al (yeni/güncellenen ayırt etmek icin)
                var mevcutIdler = new HashSet<int>();
                using (var selCmd = new NpgsqlCommand(
                    """SELECT "ZirveId" FROM "YLResmiCariler" """, pg))
                {
                    using var r = await selCmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                        mevcutIdler.Add(r.GetInt32(0));
                }

                const string upsertSql = """
                    INSERT INTO "YLResmiCariler"
                    ("ZirveId","HesapKodu","CariAdi","Unvan","VergiNo","TCKimlikNo",
                     "VergiDairesi","VergiDairesiKodu","Il","Ilce","Ulke",
                     "Adres1","Adres2","Telefon1","Cep","Email","Web","Yetkili",
                     "SonSenkronTarihi")
                    VALUES
                    (@zid,@kod,@cadi,@unvan,@vno,@tc,
                     @vd,@vdk,@il,@ilce,@ulke,
                     @a1,@a2,@tel,@cep,@email,@web,@yet,
                     CURRENT_TIMESTAMP)
                    ON CONFLICT ("HesapKodu") DO UPDATE SET
                        "ZirveId"          = EXCLUDED."ZirveId",
                        "CariAdi"          = EXCLUDED."CariAdi",
                        "Unvan"            = EXCLUDED."Unvan",
                        "VergiNo"          = EXCLUDED."VergiNo",
                        "TCKimlikNo"       = EXCLUDED."TCKimlikNo",
                        "VergiDairesi"     = EXCLUDED."VergiDairesi",
                        "VergiDairesiKodu" = EXCLUDED."VergiDairesiKodu",
                        "Il"               = EXCLUDED."Il",
                        "Ilce"             = EXCLUDED."Ilce",
                        "Ulke"             = EXCLUDED."Ulke",
                        "Adres1"           = EXCLUDED."Adres1",
                        "Adres2"           = EXCLUDED."Adres2",
                        "Telefon1"         = EXCLUDED."Telefon1",
                        "Cep"              = EXCLUDED."Cep",
                        "Email"            = EXCLUDED."Email",
                        "Web"              = EXCLUDED."Web",
                        "Yetkili"          = EXCLUDED."Yetkili",
                        "SonSenkronTarihi" = CURRENT_TIMESTAMP;
                    """;

                foreach (var c in zirveCariler)
                {
                    using var cmd = new NpgsqlCommand(upsertSql, pg);
                    cmd.Parameters.AddWithValue("zid",   c.ZirveId);
                    cmd.Parameters.AddWithValue("kod",   c.HesapKodu);
                    cmd.Parameters.AddWithValue("cadi",  (object?)c.CariAdi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("unvan", (object?)c.Unvan ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("vno",   (object?)c.VergiNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("tc",    (object?)c.TCKimlikNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("vd",    (object?)c.VergiDairesi ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("vdk",   (object?)c.VergiDairesiKodu ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("il",    (object?)c.Il ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("ilce",  (object?)c.Ilce ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("ulke",  (object?)c.Ulke ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("a1",    (object?)c.Adres1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("a2",    (object?)c.Adres2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("tel",   (object?)c.Telefon1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("cep",   (object?)c.Cep ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("email", (object?)c.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("web",   (object?)c.Web ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("yet",   (object?)c.Yetkili ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();

                    if (mevcutIdler.Contains(c.ZirveId))
                        sonuc.Guncellenen++;
                    else
                        sonuc.Yeni++;
                }

                sonuc.Toplam = zirveCariler.Count;
            }
            catch (Exception ex)
            {
                sonuc.Hata = $"PostgreSQL'e yazma hatasi: {ex.Message}";
            }

            return sonuc;
        }

        /* ═══════════════════════════════════════════════
           POSTGRESQL'DEN LİSTE / DETAY
           ═══════════════════════════════════════════════ */

        public async Task<List<ResmiCariListItemModel>> GetResmiCarilerAsync()
        {
            await EnsureSchemaAsync();
            var items = new List<ResmiCariListItemModel>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT r."Id",
                       r."HesapKodu",
                       r."CariAdi",
                       r."VergiNo",
                       r."Il",
                       r."EslesmisCariId",
                       c."Title"
                FROM "YLResmiCariler" r
                LEFT JOIN "YLCustomers" c ON c."Id" = r."EslesmisCariId"
                ORDER BY r."CariAdi";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ResmiCariListItemModel
                {
                    Id             = reader.GetInt32(0),
                    HesapKodu      = reader.GetString(1),
                    CariAdi        = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    VergiNo        = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Il             = reader.IsDBNull(4) ? null : reader.GetString(4),
                    EslesmisCariId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    EslesmisCariAdi = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return items;
        }

        public async Task<ResmiCariModel?> GetResmiCariByIdAsync(int id)
        {
            await EnsureSchemaAsync();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT r."Id", r."ZirveId", r."HesapKodu", r."CariAdi", r."Unvan",
                       r."VergiNo", r."TCKimlikNo", r."VergiDairesi", r."VergiDairesiKodu",
                       r."Il", r."Ilce", r."Ulke", r."Adres1", r."Adres2",
                       r."Telefon1", r."Cep", r."Email", r."Web", r."Yetkili",
                       r."SonSenkronTarihi", r."EslesmisCariId",
                       c."Title"
                FROM "YLResmiCariler" r
                LEFT JOIN "YLCustomers" c ON c."Id" = r."EslesmisCariId"
                WHERE r."Id" = @id;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new ResmiCariModel
            {
                Id                = reader.GetInt32(0),
                ZirveId           = reader.GetInt32(1),
                HesapKodu         = reader.GetString(2),
                CariAdi           = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Unvan             = reader.IsDBNull(4) ? null : reader.GetString(4),
                VergiNo           = reader.IsDBNull(5) ? null : reader.GetString(5),
                TCKimlikNo        = reader.IsDBNull(6) ? null : reader.GetString(6),
                VergiDairesi      = reader.IsDBNull(7) ? null : reader.GetString(7),
                VergiDairesiKodu  = reader.IsDBNull(8) ? null : reader.GetString(8),
                Il                = reader.IsDBNull(9) ? null : reader.GetString(9),
                Ilce              = reader.IsDBNull(10) ? null : reader.GetString(10),
                Ulke              = reader.IsDBNull(11) ? null : reader.GetString(11),
                Adres1            = reader.IsDBNull(12) ? null : reader.GetString(12),
                Adres2            = reader.IsDBNull(13) ? null : reader.GetString(13),
                Telefon1          = reader.IsDBNull(14) ? null : reader.GetString(14),
                Cep               = reader.IsDBNull(15) ? null : reader.GetString(15),
                Email             = reader.IsDBNull(16) ? null : reader.GetString(16),
                Web               = reader.IsDBNull(17) ? null : reader.GetString(17),
                Yetkili           = reader.IsDBNull(18) ? null : reader.GetString(18),
                SonSenkronTarihi  = reader.GetDateTime(19),
                EslesmisCariId    = reader.IsDBNull(20) ? null : reader.GetInt32(20),
                EslesmisCariAdi   = reader.IsDBNull(21) ? null : reader.GetString(21)
            };
        }

        /* ═══════════════════════════════════════════════
           ESLESTIRME (Resmi ↔ Dahili Cari)
           ═══════════════════════════════════════════════ */

        /// <summary>Resmi cariyi dahili cari kartla eslestirir.</summary>
        public async Task<(bool ok, string msg)> EslestirAsync(int resmiCariId, int dahiliCariId)
        {
            await EnsureSchemaAsync();
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();

                // Ayni dahili cari baska bir resmiye eslesmis mi?
                using var chk = new NpgsqlCommand(
                    """SELECT COUNT(*) FROM "YLResmiCariler" WHERE "EslesmisCariId" = @did AND "Id" <> @rid""", conn);
                chk.Parameters.AddWithValue("did", dahiliCariId);
                chk.Parameters.AddWithValue("rid", resmiCariId);
                var cnt = Convert.ToInt32(await chk.ExecuteScalarAsync());
                if (cnt > 0)
                    return (false, "Bu dahili cari zaten baska bir resmi cariyle eslesmis.");

                using var cmd = new NpgsqlCommand(
                    """UPDATE "YLResmiCariler" SET "EslesmisCariId" = @did WHERE "Id" = @rid""", conn);
                cmd.Parameters.AddWithValue("did", dahiliCariId);
                cmd.Parameters.AddWithValue("rid", resmiCariId);
                await cmd.ExecuteNonQueryAsync();

                // Dahili carinin VergiNo ve VergiDairesi'ni resmi kayittan guncelle
                var resmi = await GetResmiCariByIdAsync(resmiCariId);
                if (resmi != null)
                {
                    using var conn2 = _db.GetConnection();
                    await conn2.OpenAsync();
                    using var upd = new NpgsqlCommand("""
                        UPDATE "YLCustomers"
                        SET "TaxNumber" = @vno,
                            "TaxOffice" = @vd,
                            "ModifiedDate" = CURRENT_TIMESTAMP,
                            "ModifiedBy" = 'zirve-sync'
                        WHERE "Id" = @cid
                        """, conn2);
                    upd.Parameters.AddWithValue("vno", (object?)resmi.VergiNo ?? DBNull.Value);
                    upd.Parameters.AddWithValue("vd",  (object?)resmi.VergiDairesi ?? DBNull.Value);
                    upd.Parameters.AddWithValue("cid", dahiliCariId);
                    await upd.ExecuteNonQueryAsync();
                }

                return (true, "Eslestirme basarili. Vergi bilgileri dahili cariye aktarildi.");
            }
            catch (Exception ex)
            {
                return (false, $"Eslestirme hatasi: {ex.Message}");
            }
        }

        /// <summary>Eslestirmeyi kaldirir.</summary>
        public async Task<(bool ok, string msg)> EslestirmeKaldirAsync(int resmiCariId)
        {
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand(
                    """UPDATE "YLResmiCariler" SET "EslesmisCariId" = NULL WHERE "Id" = @rid""", conn);
                cmd.Parameters.AddWithValue("rid", resmiCariId);
                await cmd.ExecuteNonQueryAsync();
                return (true, "Eslestirme kaldirildi.");
            }
            catch (Exception ex)
            {
                return (false, $"Hata: {ex.Message}");
            }
        }

        /// <summary>Son senkronizasyon tarihini getirir.</summary>
        public async Task<DateTime?> GetSonSenkronTarihiAsync()
        {
            await EnsureSchemaAsync();
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand(
                    """SELECT MAX("SonSenkronTarihi") FROM "YLResmiCariler" """, conn);
                var val = await cmd.ExecuteScalarAsync();
                return val is DateTime dt ? dt : null;
            }
            catch { return null; }
        }

        /// <summary>Istatistikleri getirir.</summary>
        public async Task<(int toplam, int eslesmis, int eslesmemis)> GetIstatistiklerAsync()
        {
            await EnsureSchemaAsync();
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand("""
                    SELECT
                        COUNT(*),
                        COUNT("EslesmisCariId"),
                        COUNT(*) - COUNT("EslesmisCariId")
                    FROM "YLResmiCariler"
                    """, conn);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                    return (r.GetInt32(0), r.GetInt32(1), r.GetInt32(2));
            }
            catch { }
            return (0, 0, 0);
        }

        /* ═══════════════════════════════════════════════
           YARDIMCILAR
           ═══════════════════════════════════════════════ */

        private static string SafeStr(SqlDataReader r, int i)
            => r.IsDBNull(i) ? "" : r.GetValue(i)?.ToString()?.Trim() ?? "";

        private static string? SafeStrNull(SqlDataReader r, int i)
            => r.IsDBNull(i) ? null : r.GetValue(i)?.ToString()?.Trim();
    }
}
