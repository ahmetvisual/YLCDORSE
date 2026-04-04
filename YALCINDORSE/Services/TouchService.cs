using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    // ── Models ──────────────────────────────────────────────────────

    public class TouchModel
    {
        public int Id { get; set; }
        public int TeklifId { get; set; }
        public int RevizyonNo { get; set; }
        public DateTime TemasTarihi { get; set; } = DateTime.Now;
        public int? TemasEden { get; set; }
        public string? TemasEdenAdi { get; set; }
        public string TemasTipi { get; set; } = "NOTE"; // CALL, MAIL, VISIT, NOTE, MEETING
        public string? Not { get; set; }
        public DateTime? SonrakiTemasTarihi { get; set; }
        public bool YonetimDahilMi { get; set; }
        public string? DurumGuncelleme { get; set; }  // Durum degisikligi (ORDER, CLOSED, OPEN vs.)
        public string? PuanGuncelleme { get; set; }   // Puan degisikligi (HOT, WARM, COLD)
        public DateTime OlusturmaTarihi { get; set; }
        public string Olusturan { get; set; } = "";
    }

    public class RevisionModel
    {
        public int Id { get; set; }
        public int TeklifId { get; set; }
        public int RevizyonNo { get; set; }
        public DateTime RevizyonTarihi { get; set; } = DateTime.Now;
        public decimal? Fiyat { get; set; }
        public string? ModelGuncelleme { get; set; }
        public string? Neden { get; set; }
        public string? Not { get; set; }
        public string Olusturan { get; set; } = "";
    }

    public class TimelineEntryModel
    {
        public string EntryType { get; set; } = ""; // "TOUCH" or "REVISION"
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string? Aciklama { get; set; }
        public string? KisiAdi { get; set; }
        public string? Tipi { get; set; } // Touch: CALL/MAIL/VISIT/NOTE | Revision: R0/R1/R2
        public decimal? Fiyat { get; set; }
        public DateTime? SonrakiTemas { get; set; }
        public bool YonetimDahil { get; set; }
        public int RevizyonNo { get; set; }
    }

    public class TouchReminderModel
    {
        public int TeklifId { get; set; }
        public string TeklifNo { get; set; } = "";
        public string Musteri { get; set; } = "";
        public string? MusteriKodu { get; set; }
        public string? Satici { get; set; }
        public string Durum { get; set; } = "";
        public string Puan { get; set; } = "";
        public DateTime PlanlananTarih { get; set; }
        public int GecenGun { get; set; }
        public decimal NetTutar { get; set; }
        public string? ParaBirimi { get; set; }
        public string? SonTemasNotu { get; set; }
    }

    public class DashboardKpiModel
    {
        public int ToplamTeklif { get; set; }
        public int HotSayisi { get; set; }
        public int WarmSayisi { get; set; }
        public int ColdSayisi { get; set; }
        public int OpenSayisi { get; set; }
        public int OrderSayisi { get; set; }
        public int ClosedSayisi { get; set; }
        public int GecikenTemasSayisi { get; set; }
        public int HareketsizSayisi { get; set; }
        public int ToplamTemas { get; set; }
    }

    public class PersonPerformanceModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public int TeklifSayisi { get; set; }
        public int TemasSayisi { get; set; }
        public int SiparisSayisi { get; set; }
    }

    public class CalendarEntryModel
    {
        public DateTime Tarih { get; set; }
        public string Tip { get; set; } = ""; // TOUCH, REVISION, PLANNED, OVERDUE, OFFER
        public string Baslik { get; set; } = "";
        public int TeklifId { get; set; }
        public string? TeklifNo { get; set; }
        public string? Musteri { get; set; }
        public string? Durum { get; set; }
    }

    // ── Service ─────────────────────────────────────────────────────

    public class TouchService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);
        private bool _schemaEnsured;  // static degil: her restart'ta yeniden calisir (IF NOT EXISTS ile guvenli)

        private const string SchemaSql = """
            CREATE TABLE IF NOT EXISTS "YLTemaslar" (
                "Id" SERIAL PRIMARY KEY,
                "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
                "RevizyonNo" INT NOT NULL DEFAULT 0,
                "TemasTarihi" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "TemasEden" INT REFERENCES "YLUsers"("Id"),
                "TemasTipi" VARCHAR(20) NOT NULL DEFAULT 'NOTE',
                "Not" TEXT,
                "SonrakiTemasTarihi" DATE,
                "YonetimDahilMi" BOOLEAN DEFAULT FALSE,
                "DurumGuncelleme" VARCHAR(50),
                "OlusturmaTarihi" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                "Olusturan" VARCHAR(100) DEFAULT 'system'
            );
            CREATE INDEX IF NOT EXISTS "IX_YLTemaslar_TeklifId" ON "YLTemaslar"("TeklifId");
            CREATE INDEX IF NOT EXISTS "IX_YLTemaslar_SonrakiTemas" ON "YLTemaslar"("SonrakiTemasTarihi");
            CREATE INDEX IF NOT EXISTS "IX_YLTemaslar_TemasEden" ON "YLTemaslar"("TemasEden");

            CREATE TABLE IF NOT EXISTS "YLTeklifRevizyonlari" (
                "Id" SERIAL PRIMARY KEY,
                "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
                "RevizyonNo" INT NOT NULL,
                "RevizyonTarihi" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                "Fiyat" NUMERIC(18,2),
                "ModelGuncelleme" VARCHAR(200),
                "Neden" TEXT,
                "Not" TEXT,
                "Olusturan" VARCHAR(100) DEFAULT 'system',
                UNIQUE("TeklifId", "RevizyonNo")
            );
            CREATE INDEX IF NOT EXISTS "IX_YLTeklifRevizyonlari_TeklifId" ON "YLTeklifRevizyonlari"("TeklifId");

            CREATE TABLE IF NOT EXISTS "YLTeklifIlgiliKisileri" (
                "Id" SERIAL PRIMARY KEY,
                "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
                "IlgiliKisiId" INT NOT NULL REFERENCES "YLCustomerContacts"("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_YLTeklifIlgiliKisileri_TeklifId" ON "YLTeklifIlgiliKisileri"("TeklifId");

            ALTER TABLE "YLTemaslar" ADD COLUMN IF NOT EXISTS "PuanGuncelleme" VARCHAR(20);
            """;

        public TouchService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
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
                using var cmd = new NpgsqlCommand(SchemaSql, conn);
                await cmd.ExecuteNonQueryAsync();
                _schemaEnsured = true;
            }
            catch { /* Tablo zaten varsa veya yetki yoksa sessizce gecilir */ }
            finally { _schemaLock.Release(); }
        }

        // ── TOUCH CRUD ──────────────────────────────────────────────

        public async Task<List<TouchModel>> GetTouchesForQuoteAsync(int teklifId)
        {
            await EnsureSchemaAsync();
            var items = new List<TouchModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT t."Id", t."TeklifId", t."RevizyonNo", t."TemasTarihi",
                       t."TemasEden", u."FullName", t."TemasTipi", t."Not",
                       t."SonrakiTemasTarihi", t."YonetimDahilMi", t."DurumGuncelleme",
                       t."OlusturmaTarihi", t."Olusturan"
                FROM "YLTemaslar" t
                LEFT JOIN "YLUsers" u ON u."Id" = t."TemasEden"
                WHERE t."TeklifId" = @teklifId
                ORDER BY t."TemasTarihi" ASC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("teklifId", teklifId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TouchModel
                {
                    Id = reader.GetInt32(0),
                    TeklifId = reader.GetInt32(1),
                    RevizyonNo = reader.GetInt32(2),
                    TemasTarihi = reader.GetDateTime(3),
                    TemasEden = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    TemasEdenAdi = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TemasTipi = reader.GetString(6),
                    Not = reader.IsDBNull(7) ? null : reader.GetString(7),
                    SonrakiTemasTarihi = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    YonetimDahilMi = reader.IsDBNull(9) ? false : reader.GetBoolean(9),
                    DurumGuncelleme = reader.IsDBNull(10) ? null : reader.GetString(10),
                    OlusturmaTarihi = reader.IsDBNull(11) ? DateTime.Now : reader.GetDateTime(11),
                    Olusturan = reader.IsDBNull(12) ? "" : reader.GetString(12)
                });
            }
            return items;
        }

        public async Task<int> CreateTouchAsync(TouchModel touch)
        {
            await EnsureSchemaAsync();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                INSERT INTO "YLTemaslar"
                ("TeklifId", "RevizyonNo", "TemasTarihi", "TemasEden", "TemasTipi",
                 "Not", "SonrakiTemasTarihi", "YonetimDahilMi", "DurumGuncelleme", "PuanGuncelleme", "Olusturan")
                VALUES
                (@TeklifId, @RevizyonNo, @TemasTarihi, @TemasEden, @TemasTipi,
                 @Not, @SonrakiTemasTarihi, @YonetimDahilMi, @DurumGuncelleme, @PuanGuncelleme, @Olusturan)
                RETURNING "Id";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("TeklifId", touch.TeklifId);
            cmd.Parameters.AddWithValue("RevizyonNo", touch.RevizyonNo);
            cmd.Parameters.AddWithValue("TemasTarihi", touch.TemasTarihi);
            cmd.Parameters.AddWithValue("TemasEden", (object?)touch.TemasEden ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TemasTipi", touch.TemasTipi);
            cmd.Parameters.AddWithValue("Not", (object?)touch.Not ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SonrakiTemasTarihi", (object?)touch.SonrakiTemasTarihi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("YonetimDahilMi", touch.YonetimDahilMi);
            cmd.Parameters.AddWithValue("DurumGuncelleme", (object?)touch.DurumGuncelleme ?? DBNull.Value);
            cmd.Parameters.AddWithValue("PuanGuncelleme", (object?)touch.PuanGuncelleme ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Olusturan", _auth.CurrentUser ?? "system");

            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Puan degisikligi varsa YLTeklifler.Puan guncelle
            if (!string.IsNullOrEmpty(touch.PuanGuncelleme))
            {
                const string puanSql = """
                    UPDATE "YLTeklifler" SET "Puan" = @puan, "DegistirmeTarihi" = CURRENT_TIMESTAMP,
                    "Degistiren" = @degistiren WHERE "Id" = @teklifId;
                    """;
                using var cmdPuan = new NpgsqlCommand(puanSql, conn);
                cmdPuan.Parameters.AddWithValue("puan", touch.PuanGuncelleme);
                cmdPuan.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
                cmdPuan.Parameters.AddWithValue("teklifId", touch.TeklifId);
                await cmdPuan.ExecuteNonQueryAsync();
            }

            // Durum degisikligi varsa YLTeklifler.Durum guncelle
            if (!string.IsNullOrEmpty(touch.DurumGuncelleme))
            {
                const string durumSql = """
                    UPDATE "YLTeklifler" SET "Durum" = @durum, "DegistirmeTarihi" = CURRENT_TIMESTAMP,
                    "Degistiren" = @degistiren WHERE "Id" = @teklifId;
                    """;
                using var cmdDurum = new NpgsqlCommand(durumSql, conn);
                cmdDurum.Parameters.AddWithValue("durum", touch.DurumGuncelleme);
                cmdDurum.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
                cmdDurum.Parameters.AddWithValue("teklifId", touch.TeklifId);
                await cmdDurum.ExecuteNonQueryAsync();
            }

            return id;
        }

        public async Task UpdateTouchAsync(TouchModel touch)
        {
            await EnsureSchemaAsync();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                UPDATE "YLTemaslar"
                SET "TemasTarihi"        = @TemasTarihi,
                    "TemasTipi"          = @TemasTipi,
                    "Not"                = @Not,
                    "SonrakiTemasTarihi" = @SonrakiTemasTarihi,
                    "YonetimDahilMi"     = @YonetimDahilMi,
                    "DurumGuncelleme"    = @DurumGuncelleme,
                    "PuanGuncelleme"     = @PuanGuncelleme
                WHERE "Id" = @Id;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("Id", touch.Id);
            cmd.Parameters.AddWithValue("TemasTarihi", touch.TemasTarihi);
            cmd.Parameters.AddWithValue("TemasTipi", touch.TemasTipi);
            cmd.Parameters.AddWithValue("Not", (object?)touch.Not ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SonrakiTemasTarihi", (object?)touch.SonrakiTemasTarihi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("YonetimDahilMi", touch.YonetimDahilMi);
            cmd.Parameters.AddWithValue("DurumGuncelleme", (object?)touch.DurumGuncelleme ?? DBNull.Value);
            cmd.Parameters.AddWithValue("PuanGuncelleme", (object?)touch.PuanGuncelleme ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();

            // Puan degisikligi guncelle
            if (!string.IsNullOrEmpty(touch.PuanGuncelleme))
            {
                const string puanSql = """
                    UPDATE "YLTeklifler" SET "Puan" = @puan, "DegistirmeTarihi" = CURRENT_TIMESTAMP,
                    "Degistiren" = @degistiren WHERE "Id" = @teklifId;
                    """;
                using var cmdPuan = new NpgsqlCommand(puanSql, conn);
                cmdPuan.Parameters.AddWithValue("puan", touch.PuanGuncelleme);
                cmdPuan.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
                cmdPuan.Parameters.AddWithValue("teklifId", touch.TeklifId);
                await cmdPuan.ExecuteNonQueryAsync();
            }

            // Durum degisikligi guncelle
            if (!string.IsNullOrEmpty(touch.DurumGuncelleme))
            {
                const string durumSql = """
                    UPDATE "YLTeklifler" SET "Durum" = @durum, "DegistirmeTarihi" = CURRENT_TIMESTAMP,
                    "Degistiren" = @degistiren WHERE "Id" = @teklifId;
                    """;
                using var cmdDurum = new NpgsqlCommand(durumSql, conn);
                cmdDurum.Parameters.AddWithValue("durum", touch.DurumGuncelleme);
                cmdDurum.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
                cmdDurum.Parameters.AddWithValue("teklifId", touch.TeklifId);
                await cmdDurum.ExecuteNonQueryAsync();
            }
        }

        // ── REVISION CRUD ───────────────────────────────────────────

        public async Task<List<RevisionModel>> GetRevisionsAsync(int teklifId)
        {
            await EnsureSchemaAsync();
            var items = new List<RevisionModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT "Id", "TeklifId", "RevizyonNo", "RevizyonTarihi",
                       "Fiyat", "ModelGuncelleme", "Neden", "Not", "Olusturan"
                FROM "YLTeklifRevizyonlari"
                WHERE "TeklifId" = @teklifId
                ORDER BY "RevizyonNo" ASC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("teklifId", teklifId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new RevisionModel
                {
                    Id = reader.GetInt32(0),
                    TeklifId = reader.GetInt32(1),
                    RevizyonNo = reader.GetInt32(2),
                    RevizyonTarihi = reader.GetDateTime(3),
                    Fiyat = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    ModelGuncelleme = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Neden = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Not = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Olusturan = reader.IsDBNull(8) ? "" : reader.GetString(8)
                });
            }
            return items;
        }

        public async Task<int> CreateRevisionAsync(RevisionModel revision)
        {
            await EnsureSchemaAsync();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Sonraki revizyon numarasini al
            const string maxRevSql = """
                SELECT COALESCE(MAX("RevizyonNo"), -1) + 1
                FROM "YLTeklifRevizyonlari"
                WHERE "TeklifId" = @teklifId;
                """;

            using var maxCmd = new NpgsqlCommand(maxRevSql, conn);
            maxCmd.Parameters.AddWithValue("teklifId", revision.TeklifId);
            var nextRev = Convert.ToInt32(await maxCmd.ExecuteScalarAsync());
            revision.RevizyonNo = nextRev;

            const string sql = """
                INSERT INTO "YLTeklifRevizyonlari"
                ("TeklifId", "RevizyonNo", "RevizyonTarihi", "Fiyat", "ModelGuncelleme", "Neden", "Not", "Olusturan")
                VALUES
                (@TeklifId, @RevizyonNo, @RevizyonTarihi, @Fiyat, @ModelGuncelleme, @Neden, @Not, @Olusturan)
                RETURNING "Id";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("TeklifId", revision.TeklifId);
            cmd.Parameters.AddWithValue("RevizyonNo", revision.RevizyonNo);
            cmd.Parameters.AddWithValue("RevizyonTarihi", revision.RevizyonTarihi);
            cmd.Parameters.AddWithValue("Fiyat", (object?)revision.Fiyat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ModelGuncelleme", (object?)revision.ModelGuncelleme ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Neden", (object?)revision.Neden ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Not", (object?)revision.Not ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Olusturan", _auth.CurrentUser ?? "system");

            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Teklif tablosundaki RevizyonNo'yu guncelle
            const string updateSql = """
                UPDATE "YLTeklifler"
                SET "RevizyonNo" = @revNo, "DegistirmeTarihi" = CURRENT_TIMESTAMP, "Degistiren" = @degistiren
                WHERE "Id" = @teklifId;
                """;
            using var cmd2 = new NpgsqlCommand(updateSql, conn);
            cmd2.Parameters.AddWithValue("revNo", revision.RevizyonNo);
            cmd2.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
            cmd2.Parameters.AddWithValue("teklifId", revision.TeklifId);
            await cmd2.ExecuteNonQueryAsync();

            // Fiyat varsa NetTutar'i guncelle
            if (revision.Fiyat.HasValue)
            {
                const string priceSql = """
                    UPDATE "YLTeklifler"
                    SET "NetTutar" = @fiyat
                    WHERE "Id" = @teklifId;
                    """;
                using var cmd3 = new NpgsqlCommand(priceSql, conn);
                cmd3.Parameters.AddWithValue("fiyat", revision.Fiyat.Value);
                cmd3.Parameters.AddWithValue("teklifId", revision.TeklifId);
                await cmd3.ExecuteNonQueryAsync();
            }

            return id;
        }

        // ── TIMELINE ────────────────────────────────────────────────

        public async Task<List<TimelineEntryModel>> GetTimelineAsync(int teklifId)
        {
            await EnsureSchemaAsync();
            var items = new List<TimelineEntryModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT 'TOUCH' AS "EntryType", t."Id", t."TemasTarihi" AS "Tarih",
                       t."Not" AS "Aciklama", u."FullName" AS "KisiAdi",
                       t."TemasTipi" AS "Tipi", NULL::NUMERIC AS "Fiyat",
                       t."SonrakiTemasTarihi" AS "SonrakiTemas",
                       t."YonetimDahilMi" AS "YonetimDahil",
                       t."RevizyonNo"
                FROM "YLTemaslar" t
                LEFT JOIN "YLUsers" u ON u."Id" = t."TemasEden"
                WHERE t."TeklifId" = @teklifId
                ORDER BY "TemasTarihi" ASC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("teklifId", teklifId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TimelineEntryModel
                {
                    EntryType = reader.GetString(0),
                    Id = reader.GetInt32(1),
                    Tarih = reader.GetDateTime(2),
                    Aciklama = reader.IsDBNull(3) ? null : reader.GetString(3),
                    KisiAdi = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Tipi = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Fiyat = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    SonrakiTemas = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                    YonetimDahil = reader.IsDBNull(8) ? false : reader.GetBoolean(8),
                    RevizyonNo = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                });
            }
            return items;
        }

        // ── DASHBOARD KPIs ──────────────────────────────────────────

        public async Task<DashboardKpiModel> GetDashboardKpisAsync(int? saticiId = null)
        {
            await EnsureSchemaAsync();
            var kpi = new DashboardKpiModel();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Teklif durum sayilari
            var statusSql = """
                SELECT "Puan", COUNT(*) FROM "YLTeklifler"
                WHERE "Durum" NOT IN ('CLOSED')
                """ + (saticiId.HasValue ? " AND \"SaticiId\" = @saticiId" : "") + """
                GROUP BY "Puan";
                """;

            using var cmd1 = new NpgsqlCommand(statusSql, conn);
            if (saticiId.HasValue)
                cmd1.Parameters.AddWithValue("saticiId", saticiId.Value);

            using var r1 = await cmd1.ExecuteReaderAsync();
            while (await r1.ReadAsync())
            {
                var puan = r1.IsDBNull(0) ? "" : r1.GetString(0);
                var count = r1.GetInt32(1);
                switch (puan)
                {
                    case "HOT": kpi.HotSayisi = count; break;
                    case "WARM": kpi.WarmSayisi = count; break;
                    case "COLD": kpi.ColdSayisi = count; break;
                }
            }
            await r1.CloseAsync();

            // Durum bazli sayilar
            var durumSql = """
                SELECT "Durum", COUNT(*) FROM "YLTeklifler"
                """ + (saticiId.HasValue ? " WHERE \"SaticiId\" = @saticiId2" : "") + """
                GROUP BY "Durum";
                """;

            using var cmd2 = new NpgsqlCommand(durumSql, conn);
            if (saticiId.HasValue)
                cmd2.Parameters.AddWithValue("saticiId2", saticiId.Value);

            using var r2 = await cmd2.ExecuteReaderAsync();
            while (await r2.ReadAsync())
            {
                var durum = r2.IsDBNull(0) ? "" : r2.GetString(0);
                var count = r2.GetInt32(1);
                kpi.ToplamTeklif += count;
                switch (durum)
                {
                    case "ORDER": kpi.OrderSayisi = count; break;
                    case "CLOSED": kpi.ClosedSayisi = count; break;
                }
            }
            await r2.CloseAsync();

            // Toplam temas sayisi
            var temasSql = """
                SELECT COUNT(*) FROM "YLTemaslar" t
                JOIN "YLTeklifler" q ON q."Id" = t."TeklifId"
                WHERE 1=1
                """ + (saticiId.HasValue ? " AND q.\"SaticiId\" = @saticiId3" : "") + ";";

            using var cmd3 = new NpgsqlCommand(temasSql, conn);
            if (saticiId.HasValue)
                cmd3.Parameters.AddWithValue("saticiId3", saticiId.Value);
            kpi.ToplamTemas = Convert.ToInt32(await cmd3.ExecuteScalarAsync());

            // Geciken temas sayisi
            var gecikenSql = """
                SELECT COUNT(DISTINCT t."TeklifId")
                FROM "YLTemaslar" t
                JOIN "YLTeklifler" q ON q."Id" = t."TeklifId"
                WHERE t."SonrakiTemasTarihi" < CURRENT_DATE
                  AND q."Durum" NOT IN ('CLOSED', 'ORDER')
                  AND t."Id" = (SELECT MAX(t2."Id") FROM "YLTemaslar" t2 WHERE t2."TeklifId" = t."TeklifId")
                """ + (saticiId.HasValue ? " AND q.\"SaticiId\" = @saticiId4" : "") + ";";

            using var cmd4 = new NpgsqlCommand(gecikenSql, conn);
            if (saticiId.HasValue)
                cmd4.Parameters.AddWithValue("saticiId4", saticiId.Value);
            kpi.GecikenTemasSayisi = Convert.ToInt32(await cmd4.ExecuteScalarAsync());

            // Hareketsiz (30+ gun sessiz)
            var hareketsizSql = """
                SELECT COUNT(*) FROM "YLTeklifler" q
                WHERE q."Durum" NOT IN ('CLOSED', 'ORDER')
                  AND NOT EXISTS (
                      SELECT 1 FROM "YLTemaslar" t
                      WHERE t."TeklifId" = q."Id"
                        AND t."TemasTarihi" > CURRENT_TIMESTAMP - INTERVAL '30 days'
                  )
                """ + (saticiId.HasValue ? " AND q.\"SaticiId\" = @saticiId5" : "") + ";";

            using var cmd5 = new NpgsqlCommand(hareketsizSql, conn);
            if (saticiId.HasValue)
                cmd5.Parameters.AddWithValue("saticiId5", saticiId.Value);
            kpi.HareketsizSayisi = Convert.ToInt32(await cmd5.ExecuteScalarAsync());

            return kpi;
        }

        // ── PERSON PERFORMANCE ──────────────────────────────────────

        public async Task<List<PersonPerformanceModel>> GetPersonPerformanceAsync()
        {
            await EnsureSchemaAsync();
            var items = new List<PersonPerformanceModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT u."Id", u."FullName",
                       (SELECT COUNT(*) FROM "YLTeklifler" q WHERE q."SaticiId" = u."Id") AS "TeklifSayisi",
                       (SELECT COUNT(*) FROM "YLTemaslar" t WHERE t."TemasEden" = u."Id") AS "TemasSayisi",
                       (SELECT COUNT(*) FROM "YLTeklifler" q WHERE q."SaticiId" = u."Id" AND q."Durum" = 'ORDER') AS "SiparisSayisi"
                FROM "YLUsers" u
                WHERE u."IsActive" = TRUE
                ORDER BY "TeklifSayisi" DESC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new PersonPerformanceModel
                {
                    UserId = reader.GetInt32(0),
                    FullName = reader.GetString(1),
                    TeklifSayisi = reader.GetInt32(2),
                    TemasSayisi = reader.GetInt32(3),
                    SiparisSayisi = reader.GetInt32(4)
                });
            }
            return items;
        }

        // ── REMINDERS ───────────────────────────────────────────────

        public async Task<List<TouchReminderModel>> GetOverdueTouchesAsync()
        {
            return await GetRemindersByDateConditionAsync(
                "t.\"SonrakiTemasTarihi\" < CURRENT_DATE");
        }

        public async Task<List<TouchReminderModel>> GetTodayTouchesAsync()
        {
            return await GetRemindersByDateConditionAsync(
                "t.\"SonrakiTemasTarihi\" = CURRENT_DATE");
        }

        public async Task<List<TouchReminderModel>> GetWeekTouchesAsync()
        {
            return await GetRemindersByDateConditionAsync(
                "t.\"SonrakiTemasTarihi\" > CURRENT_DATE AND t.\"SonrakiTemasTarihi\" <= CURRENT_DATE + INTERVAL '7 days'");
        }

        public async Task<List<TouchReminderModel>> GetUpcomingMeetingsAsync()
        {
            return await GetRemindersByDateConditionAsync(
                "t.\"SonrakiTemasTarihi\" > CURRENT_DATE + INTERVAL '7 days' AND t.\"SonrakiTemasTarihi\" <= CURRENT_DATE + INTERVAL '30 days'");
        }

        public async Task<List<TouchReminderModel>> GetStaleTouchesAsync(int days = 30)
        {
            await EnsureSchemaAsync();
            var items = new List<TouchReminderModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = $"""
                SELECT q."Id", q."TeklifNo", c."Title", c."CustomerCode", u."FullName", q."Durum", q."Puan",
                       COALESCE(
                           (SELECT MAX(t."TemasTarihi") FROM "YLTemaslar" t WHERE t."TeklifId" = q."Id"),
                           q."OlusturmaTarihi"
                       ) AS "SonTemas",
                       EXTRACT(DAY FROM CURRENT_TIMESTAMP -
                           COALESCE(
                               (SELECT MAX(t."TemasTarihi") FROM "YLTemaslar" t WHERE t."TeklifId" = q."Id"),
                               q."OlusturmaTarihi"
                           )
                       )::INT AS "GecenGun"
                FROM "YLTeklifler" q
                LEFT JOIN "YLCustomers" c ON c."Id" = q."MusteriId"
                LEFT JOIN "YLUsers" u ON u."Id" = q."SaticiId"
                WHERE q."Durum" NOT IN ('CLOSED', 'ORDER')
                  AND NOT EXISTS (
                      SELECT 1 FROM "YLTemaslar" t
                      WHERE t."TeklifId" = q."Id"
                        AND t."TemasTarihi" > CURRENT_TIMESTAMP - INTERVAL '{days} days'
                  )
                ORDER BY "GecenGun" DESC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TouchReminderModel
                {
                    TeklifId = reader.GetInt32(0),
                    TeklifNo = reader.GetString(1),
                    Musteri = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    MusteriKodu = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Satici = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Durum = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Puan = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    PlanlananTarih = reader.GetDateTime(7),
                    GecenGun = reader.IsDBNull(8) ? 0 : reader.GetInt32(8)
                });
            }
            return items;
        }

        private async Task<List<TouchReminderModel>> GetRemindersByDateConditionAsync(string dateCondition)
        {
            await EnsureSchemaAsync();
            var items = new List<TouchReminderModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = $"""
                SELECT q."Id", q."TeklifNo", c."Title", c."CustomerCode", u."FullName",
                       q."Durum", q."Puan", t."SonrakiTemasTarihi",
                       EXTRACT(DAY FROM CURRENT_DATE - t."SonrakiTemasTarihi")::INT AS "GecenGun",
                       q."NetTutar", q."ParaBirimi",
                       t."Not" AS "SonTemasNotu"
                FROM "YLTemaslar" t
                JOIN "YLTeklifler" q ON q."Id" = t."TeklifId"
                LEFT JOIN "YLCustomers" c ON c."Id" = q."MusteriId"
                LEFT JOIN "YLUsers" u ON u."Id" = q."SaticiId"
                WHERE {dateCondition}
                  AND q."Durum" NOT IN ('CLOSED', 'ORDER')
                  AND t."Id" = (SELECT MAX(t2."Id") FROM "YLTemaslar" t2 WHERE t2."TeklifId" = t."TeklifId")
                ORDER BY t."SonrakiTemasTarihi" ASC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TouchReminderModel
                {
                    TeklifId = reader.GetInt32(0),
                    TeklifNo = reader.GetString(1),
                    Musteri = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    MusteriKodu = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Satici = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Durum = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Puan = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    PlanlananTarih = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7),
                    GecenGun = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    NetTutar = reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
                    ParaBirimi = reader.IsDBNull(10) ? null : reader.GetString(10),
                    SonTemasNotu = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }
            return items;
        }

        // ── CALENDAR ────────────────────────────────────────────────

        public async Task<List<CalendarEntryModel>> GetCalendarEntriesAsync(int year, int month)
        {
            await EnsureSchemaAsync();
            var items = new List<CalendarEntryModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            const string sql = """
                -- Gorusmeler (temaslar)
                SELECT 'TOUCH' AS "Tip", t."TemasTarihi"::DATE AS "Tarih",
                       q."TeklifNo" || ' - ' ||
                       CASE t."TemasTipi" WHEN 'CALL' THEN 'Arama' WHEN 'MAIL' THEN 'Mail'
                       WHEN 'VISIT' THEN 'Ziyaret' WHEN 'NOTE' THEN 'Not' WHEN 'MEETING' THEN 'Toplanti'
                       ELSE t."TemasTipi" END AS "Baslik",
                       q."Id" AS "TeklifId", q."TeklifNo", c."Title" AS "Musteri", q."Durum"
                FROM "YLTemaslar" t
                JOIN "YLTeklifler" q ON q."Id" = t."TeklifId"
                LEFT JOIN "YLCustomers" c ON c."Id" = q."MusteriId"
                WHERE t."TemasTarihi"::DATE >= @startDate AND t."TemasTarihi"::DATE < @endDate

                UNION ALL

                -- Planli temaslar (sonraki temas tarihi bu ayda olanlar)
                SELECT CASE WHEN t."SonrakiTemasTarihi" < CURRENT_DATE THEN 'OVERDUE' ELSE 'PLANNED' END,
                       t."SonrakiTemasTarihi",
                       COALESCE(c."Title", q."TeklifNo") || ' · Gorusme',
                       q."Id", q."TeklifNo", c."Title", q."Durum"
                FROM "YLTemaslar" t
                JOIN "YLTeklifler" q ON q."Id" = t."TeklifId"
                LEFT JOIN "YLCustomers" c ON c."Id" = q."MusteriId"
                WHERE t."SonrakiTemasTarihi" >= @startDate AND t."SonrakiTemasTarihi" < @endDate
                  AND q."Durum" NOT IN ('CLOSED', 'ORDER')
                  AND t."Id" = (SELECT MAX(t2."Id") FROM "YLTemaslar" t2 WHERE t2."TeklifId" = t."TeklifId")

                ORDER BY "Tarih" ASC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("startDate", startDate);
            cmd.Parameters.AddWithValue("endDate", endDate);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new CalendarEntryModel
                {
                    Tip = reader.GetString(0),
                    Tarih = reader.GetDateTime(1),
                    Baslik = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    TeklifId = reader.GetInt32(3),
                    TeklifNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Musteri = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Durum = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }
            return items;
        }

        // ── STATUS UPDATE ───────────────────────────────────────────

        public async Task UpdateQuoteStatusAsync(int teklifId, string durum)
        {
            await EnsureSchemaAsync();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                UPDATE "YLTeklifler"
                SET "Durum" = @durum, "DegistirmeTarihi" = CURRENT_TIMESTAMP, "Degistiren" = @degistiren
                WHERE "Id" = @teklifId;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("durum", durum);
            cmd.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
            cmd.Parameters.AddWithValue("teklifId", teklifId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateQuotePuanAsync(int teklifId, string puan)
        {
            await EnsureSchemaAsync();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                UPDATE "YLTeklifler"
                SET "Puan" = @puan, "DegistirmeTarihi" = CURRENT_TIMESTAMP, "Degistiren" = @degistiren
                WHERE "Id" = @teklifId;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("puan", puan);
            cmd.Parameters.AddWithValue("degistiren", _auth.CurrentUser ?? "system");
            cmd.Parameters.AddWithValue("teklifId", teklifId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
