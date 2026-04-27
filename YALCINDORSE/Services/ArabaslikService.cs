using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    // === MODELS ===
    public class ArabaslikGrupModel
    {
        public int Id { get; set; }
        public string GrupAdi { get; set; } = "";
        public string GrupAdi_EN { get; set; } = "";
        public string GrupAdi_FR { get; set; } = "";
        public string GrupAdi_DE { get; set; } = "";
        public string GrupAdi_RO { get; set; } = "";
        public string GrupAdi_AR { get; set; } = "";
        public string GrupAdi_RU { get; set; } = "";
        public short TablTipi { get; set; } = 1;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = "";
    }

    public class ArabaslikDetayModel
    {
        public int Id { get; set; }
        public int GrupId { get; set; }
        public string SatirMetni { get; set; } = "";
        public string SatirMetni_EN { get; set; } = "";
        public string SatirMetni_FR { get; set; } = "";
        public string SatirMetni_DE { get; set; } = "";
        public string SatirMetni_RO { get; set; } = "";
        public string SatirMetni_AR { get; set; } = "";
        public string SatirMetni_RU { get; set; } = "";
        public decimal? Fiyat { get; set; }
        public string? ParaBirimi { get; set; }
        public int SortOrder { get; set; }

        /// <summary>
        /// TablTipi=1 (iki kolon tablo) icin evrensel deger alani — "9.500 mm" gibi.
        /// Yeni DB kolonu gerektirmez: ParaBirimi alani uzerinde calisir
        /// (TablTipi=1 satirlarinda Fiyat/ParaBirimi zaten kullanilmaz).
        /// </summary>
        public string Deger
        {
            get => ParaBirimi ?? "";
            set => ParaBirimi = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    // === SERVICE ===
    public class ArabaslikService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private bool _schemaEnsured = false;

        public ArabaslikService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        /// <summary>
        /// YLArabaslikDetaylar tablosunun ParaBirimi kolonunu TEXT'e genisletir.
        /// VARCHAR(10) olarak yaratilmis olabilir; Deger alani icin yeterli degil.
        /// </summary>
        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            _schemaEnsured = true;
            try
            {
                const string sql = """
                    DO $$
                    BEGIN
                        -- ParaBirimi kolonunu TEXT'e genislet (eger VARCHAR(n) ise)
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'YLArabaslikDetaylar'
                              AND column_name = 'ParaBirimi'
                              AND data_type = 'character varying'
                        ) THEN
                            ALTER TABLE "YLArabaslikDetaylar"
                                ALTER COLUMN "ParaBirimi" TYPE TEXT;
                        END IF;

                        -- YLTeklifKalemleri.Birim kolonunu TEXT'e genislet
                        -- SPEC satirlarda "12 ton x 6 aks = 96 ton**" gibi degerler VARCHAR(20) ye sigmayabiliyor
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'YLTeklifKalemleri'
                              AND column_name = 'Birim'
                              AND data_type = 'character varying'
                        ) THEN
                            ALTER TABLE "YLTeklifKalemleri"
                                ALTER COLUMN "Birim" TYPE TEXT;
                        END IF;
                    END$$;
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* tablo yoksa veya yetki yoksa sessiz devam */ }

            // OM (Offer Manual) sablonlarini idempotent seed et — barandrive
            // OM_TR/OM_EN.xlsx verilerinden 3 grup (BPW/SAF, TURK MALI, OPSIYONEL)
            // YLArabaslikGruplar/Detaylar tablolarina yansir; quote items editor'da
            // sablon olarak gorunur. Mevcut grup varsa atlar (kullanici silmis olabilir).
            try { await SeedOmSablonAsync(conn); }
            catch { /* seed hatasi: kullanici manuel SQL calistirabilir */ }
        }

        /// <summary>
        /// OmFormVerileri'ndeki TR/EN listelerini YLArabaslikGruplar+Detaylar'a
        /// idempotent insert eder. SECTION satirlari yeni grup acar; ITEM satirlari
        /// o gruba TablTipi=2 detayi olarak eklenir. Grup ayni isimde varsa atlar.
        /// </summary>
        private static async Task SeedOmSablonAsync(NpgsqlConnection conn)
        {
            var tr = OmFormVerileri.TR;
            var en = OmFormVerileri.EN;
            if (tr.Count == 0 || tr.Count != en.Count) return;

            int? curGrupId = null;
            int  detaySort = 0;
            int  grupSort  = 900;  // Diger gruplarin sonuna konumlanir

            for (int i = 0; i < tr.Count; i++)
            {
                var rTr = tr[i];
                var rEn = en[i];

                if (rTr.Tip == "SECTION")
                {
                    string grupAdi    = rTr.Aciklama;
                    string grupAdiEn  = rEn.Aciklama;

                    // Idempotent: ayni isimde grup varsa atla. Kullanici silmis olabilir,
                    // tekrar otomatik olusturma — bilerek silmis olabilir.
                    using (var checkCmd = new NpgsqlCommand(
                        @"SELECT ""Id"" FROM ""YLArabaslikGruplar"" WHERE ""GrupAdi"" = @ad LIMIT 1",
                        conn))
                    {
                        checkCmd.Parameters.AddWithValue("ad", grupAdi);
                        var existing = await checkCmd.ExecuteScalarAsync();
                        if (existing != null && existing != DBNull.Value)
                        {
                            curGrupId = null;  // Bu grubu atla; detaylar da eklenmez
                            grupSort += 10;
                            continue;
                        }
                    }

                    using var insGrup = new NpgsqlCommand("""
                        INSERT INTO "YLArabaslikGruplar"
                            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
                             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
                        VALUES (@ad,@adEn,'','','','','',2,@sort,TRUE,NOW(),'OM Auto-Seed')
                        RETURNING "Id"
                        """, conn);
                    insGrup.Parameters.AddWithValue("ad",   grupAdi);
                    insGrup.Parameters.AddWithValue("adEn", grupAdiEn);
                    insGrup.Parameters.AddWithValue("sort", grupSort);
                    grupSort += 10;
                    curGrupId = Convert.ToInt32(await insGrup.ExecuteScalarAsync() ?? 0);
                    detaySort = 0;
                }
                else if (rTr.Tip == "ITEM" && curGrupId.HasValue)
                {
                    detaySort += 10;

                    // SatirMetni: "{Kod} - {Aciklama}" + (varsa) " — {Detay}"
                    string metniTr = string.IsNullOrEmpty(rTr.Kod)
                        ? rTr.Aciklama
                        : $"{rTr.Kod} - {rTr.Aciklama}";
                    if (!string.IsNullOrWhiteSpace(rTr.Detay))
                        metniTr += $" — {rTr.Detay}";

                    string metniEn = string.IsNullOrEmpty(rEn.Kod)
                        ? rEn.Aciklama
                        : $"{rEn.Kod} - {rEn.Aciklama}";
                    if (!string.IsNullOrWhiteSpace(rEn.Detay))
                        metniEn += $" — {rEn.Detay}";

                    using var insDet = new NpgsqlCommand("""
                        INSERT INTO "YLArabaslikDetaylar"
                            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE",
                             "SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
                        VALUES (@gid,@metni,@metniEn,'','','','','',@fiyat,'EUR',@sort)
                        """, conn);
                    insDet.Parameters.AddWithValue("gid",     curGrupId.Value);
                    insDet.Parameters.AddWithValue("metni",   metniTr);
                    insDet.Parameters.AddWithValue("metniEn", metniEn);
                    insDet.Parameters.AddWithValue("fiyat",
                        rTr.BirimFiyat.HasValue ? (object)rTr.BirimFiyat.Value : DBNull.Value);
                    insDet.Parameters.AddWithValue("sort",    detaySort);
                    await insDet.ExecuteNonQueryAsync();
                }
            }
        }

        // ─── GRUP CRUD ───────────────────────────────────────

        public async Task<List<ArabaslikGrupModel>> GetGruplarAsync(string search = "")
        {
            var items = new List<ArabaslikGrupModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = """
                SELECT "Id", "GrupAdi", "GrupAdi_EN", "GrupAdi_FR", "GrupAdi_DE",
                       "GrupAdi_RO", "GrupAdi_AR", "GrupAdi_RU",
                       "TablTipi", "SortOrder", "IsActive", "CreatedDate", "CreatedBy"
                FROM "YLArabaslikGruplar"
                WHERE "IsActive" = TRUE
                """;

            if (!string.IsNullOrWhiteSpace(search))
                sql += """ AND (LOWER("GrupAdi") LIKE @search OR LOWER("GrupAdi_EN") LIKE @search)""";

            sql += """ ORDER BY "SortOrder", "GrupAdi" """;

            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("search", $"%{search.Trim().ToLower()}%");

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ArabaslikGrupModel
                {
                    Id = reader.GetInt32(0),
                    GrupAdi = reader.GetString(1),
                    GrupAdi_EN = reader.GetString(2),
                    GrupAdi_FR = reader.GetString(3),
                    GrupAdi_DE = reader.GetString(4),
                    GrupAdi_RO = reader.GetString(5),
                    GrupAdi_AR = reader.GetString(6),
                    GrupAdi_RU = reader.GetString(7),
                    TablTipi = reader.GetInt16(8),
                    SortOrder = reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10),
                    CreatedDate = reader.GetDateTime(11),
                    CreatedBy = reader.GetString(12)
                });
            }
            return items;
        }

        public async Task<ArabaslikGrupModel?> GetGrupByIdAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT "Id", "GrupAdi", "GrupAdi_EN", "GrupAdi_FR", "GrupAdi_DE",
                       "GrupAdi_RO", "GrupAdi_AR", "GrupAdi_RU",
                       "TablTipi", "SortOrder", "IsActive", "CreatedDate", "CreatedBy"
                FROM "YLArabaslikGruplar"
                WHERE "Id" = @id
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ArabaslikGrupModel
                {
                    Id = reader.GetInt32(0),
                    GrupAdi = reader.GetString(1),
                    GrupAdi_EN = reader.GetString(2),
                    GrupAdi_FR = reader.GetString(3),
                    GrupAdi_DE = reader.GetString(4),
                    GrupAdi_RO = reader.GetString(5),
                    GrupAdi_AR = reader.GetString(6),
                    GrupAdi_RU = reader.GetString(7),
                    TablTipi = reader.GetInt16(8),
                    SortOrder = reader.GetInt32(9),
                    IsActive = reader.GetBoolean(10),
                    CreatedDate = reader.GetDateTime(11),
                    CreatedBy = reader.GetString(12)
                };
            }
            return null;
        }

        public async Task<int> CreateGrupAsync(ArabaslikGrupModel grup)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                INSERT INTO "YLArabaslikGruplar"
                    ("GrupAdi", "GrupAdi_EN", "GrupAdi_FR", "GrupAdi_DE",
                     "GrupAdi_RO", "GrupAdi_AR", "GrupAdi_RU",
                     "TablTipi", "SortOrder", "IsActive", "CreatedDate", "CreatedBy")
                VALUES
                    (@grupAdi, @en, @fr, @de, @ro, @ar, @ru,
                     @tablTipi, @sortOrder, TRUE, NOW(), @createdBy)
                RETURNING "Id";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("grupAdi", grup.GrupAdi);
            cmd.Parameters.AddWithValue("en", grup.GrupAdi_EN);
            cmd.Parameters.AddWithValue("fr", grup.GrupAdi_FR);
            cmd.Parameters.AddWithValue("de", grup.GrupAdi_DE);
            cmd.Parameters.AddWithValue("ro", grup.GrupAdi_RO);
            cmd.Parameters.AddWithValue("ar", grup.GrupAdi_AR);
            cmd.Parameters.AddWithValue("ru", grup.GrupAdi_RU);
            cmd.Parameters.AddWithValue("tablTipi", grup.TablTipi);
            cmd.Parameters.AddWithValue("sortOrder", grup.SortOrder);
            cmd.Parameters.AddWithValue("createdBy", _auth.FullName ?? "");

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task UpdateGrupAsync(ArabaslikGrupModel grup)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                UPDATE "YLArabaslikGruplar" SET
                    "GrupAdi" = @grupAdi,
                    "GrupAdi_EN" = @en,
                    "GrupAdi_FR" = @fr,
                    "GrupAdi_DE" = @de,
                    "GrupAdi_RO" = @ro,
                    "GrupAdi_AR" = @ar,
                    "GrupAdi_RU" = @ru,
                    "TablTipi" = @tablTipi,
                    "SortOrder" = @sortOrder
                WHERE "Id" = @id;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", grup.Id);
            cmd.Parameters.AddWithValue("grupAdi", grup.GrupAdi);
            cmd.Parameters.AddWithValue("en", grup.GrupAdi_EN);
            cmd.Parameters.AddWithValue("fr", grup.GrupAdi_FR);
            cmd.Parameters.AddWithValue("de", grup.GrupAdi_DE);
            cmd.Parameters.AddWithValue("ro", grup.GrupAdi_RO);
            cmd.Parameters.AddWithValue("ar", grup.GrupAdi_AR);
            cmd.Parameters.AddWithValue("ru", grup.GrupAdi_RU);
            cmd.Parameters.AddWithValue("tablTipi", grup.TablTipi);
            cmd.Parameters.AddWithValue("sortOrder", grup.SortOrder);

            await cmd.ExecuteNonQueryAsync();
        }

        // ─── DETAY CRUD ─────────────────────────────────────

        public async Task<List<ArabaslikDetayModel>> GetDetaylarAsync(int grupId)
        {
            var items = new List<ArabaslikDetayModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                SELECT "Id", "GrupId", "SatirMetni", "SatirMetni_EN", "SatirMetni_FR",
                       "SatirMetni_DE", "SatirMetni_RO", "SatirMetni_AR", "SatirMetni_RU",
                       "Fiyat", "ParaBirimi", "SortOrder"
                FROM "YLArabaslikDetaylar"
                WHERE "GrupId" = @grupId
                ORDER BY "SortOrder";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("grupId", grupId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ArabaslikDetayModel
                {
                    Id = reader.GetInt32(0),
                    GrupId = reader.GetInt32(1),
                    SatirMetni = reader.GetString(2),
                    SatirMetni_EN = reader.GetString(3),
                    SatirMetni_FR = reader.GetString(4),
                    SatirMetni_DE = reader.GetString(5),
                    SatirMetni_RO = reader.GetString(6),
                    SatirMetni_AR = reader.GetString(7),
                    SatirMetni_RU = reader.GetString(8),
                    Fiyat = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                    ParaBirimi = reader.IsDBNull(10) ? null : reader.GetString(10),
                    SortOrder = reader.GetInt32(11)
                    // Deger: ParaBirimi uzerinden computed property — ek alan gerektirmez
                });
            }
            return items;
        }

        // ─── SINGLE-ROW CRUD (Quote editor sag panelinden inline yonetim icin) ─

        /// <summary>Tek bir grup header'ini olusturur veya gunceller (detaylara dokunmaz).</summary>
        public async Task SaveGrupAsync(ArabaslikGrupModel grup)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            if (grup.Id > 0)
            {
                const string sql = """
                    UPDATE "YLArabaslikGruplar" SET
                        "GrupAdi" = @grupAdi, "GrupAdi_EN" = @en, "GrupAdi_FR" = @fr,
                        "GrupAdi_DE" = @de, "GrupAdi_RO" = @ro, "GrupAdi_AR" = @ar, "GrupAdi_RU" = @ru,
                        "TablTipi" = @tablTipi, "SortOrder" = @sortOrder
                    WHERE "Id" = @id;
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", grup.Id);
                cmd.Parameters.AddWithValue("grupAdi", grup.GrupAdi);
                cmd.Parameters.AddWithValue("en", grup.GrupAdi_EN ?? "");
                cmd.Parameters.AddWithValue("fr", grup.GrupAdi_FR ?? "");
                cmd.Parameters.AddWithValue("de", grup.GrupAdi_DE ?? "");
                cmd.Parameters.AddWithValue("ro", grup.GrupAdi_RO ?? "");
                cmd.Parameters.AddWithValue("ar", grup.GrupAdi_AR ?? "");
                cmd.Parameters.AddWithValue("ru", grup.GrupAdi_RU ?? "");
                cmd.Parameters.AddWithValue("tablTipi", grup.TablTipi);
                cmd.Parameters.AddWithValue("sortOrder", grup.SortOrder);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                const string sql = """
                    INSERT INTO "YLArabaslikGruplar"
                        ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
                         "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
                    VALUES (@grupAdi, @en, @fr, @de, @ro, @ar, @ru, @tablTipi, @sortOrder, TRUE, NOW(), @createdBy)
                    RETURNING "Id";
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("grupAdi", grup.GrupAdi);
                cmd.Parameters.AddWithValue("en", grup.GrupAdi_EN ?? "");
                cmd.Parameters.AddWithValue("fr", grup.GrupAdi_FR ?? "");
                cmd.Parameters.AddWithValue("de", grup.GrupAdi_DE ?? "");
                cmd.Parameters.AddWithValue("ro", grup.GrupAdi_RO ?? "");
                cmd.Parameters.AddWithValue("ar", grup.GrupAdi_AR ?? "");
                cmd.Parameters.AddWithValue("ru", grup.GrupAdi_RU ?? "");
                cmd.Parameters.AddWithValue("tablTipi", grup.TablTipi);
                cmd.Parameters.AddWithValue("sortOrder", grup.SortOrder);
                cmd.Parameters.AddWithValue("createdBy", _auth.FullName ?? "");
                var result = await cmd.ExecuteScalarAsync();
                grup.Id = Convert.ToInt32(result);
            }
        }

        /// <summary>Bir grubu ve tum detaylarini kalici olarak siler.</summary>
        public async Task DeleteGrupAsync(int grupId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                using (var cmd1 = new NpgsqlCommand("""DELETE FROM "YLArabaslikDetaylar" WHERE "GrupId" = @id;""", conn, tx))
                {
                    cmd1.Parameters.AddWithValue("id", grupId);
                    await cmd1.ExecuteNonQueryAsync();
                }
                using (var cmd2 = new NpgsqlCommand("""DELETE FROM "YLArabaslikGruplar" WHERE "Id" = @id;""", conn, tx))
                {
                    cmd2.Parameters.AddWithValue("id", grupId);
                    await cmd2.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        /// <summary>Yeni bir detay satiri ekler ve uretilen Id'yi doner.</summary>
        public async Task<int> AddDetayAsync(ArabaslikDetayModel d)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);
            const string sql = """
                INSERT INTO "YLArabaslikDetaylar"
                    ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE",
                     "SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
                VALUES (@grupId, @tr, @en, @fr, @de, @ro, @ar, @ru, @fiyat, @para, @sort)
                RETURNING "Id";
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("grupId", d.GrupId);
            cmd.Parameters.AddWithValue("tr", d.SatirMetni ?? "");
            cmd.Parameters.AddWithValue("en", d.SatirMetni_EN ?? "");
            cmd.Parameters.AddWithValue("fr", d.SatirMetni_FR ?? "");
            cmd.Parameters.AddWithValue("de", d.SatirMetni_DE ?? "");
            cmd.Parameters.AddWithValue("ro", d.SatirMetni_RO ?? "");
            cmd.Parameters.AddWithValue("ar", d.SatirMetni_AR ?? "");
            cmd.Parameters.AddWithValue("ru", d.SatirMetni_RU ?? "");
            cmd.Parameters.AddWithValue("fiyat", (object?)d.Fiyat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("para", (object?)d.ParaBirimi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sort", d.SortOrder);
            var result = await cmd.ExecuteScalarAsync();
            d.Id = Convert.ToInt32(result);
            return d.Id;
        }

        /// <summary>Mevcut bir detay satirinin tum alanlarini gunceller (multi-lang dahil).</summary>
        public async Task UpdateDetayAsync(ArabaslikDetayModel d)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);
            const string sql = """
                UPDATE "YLArabaslikDetaylar" SET
                    "SatirMetni" = @tr, "SatirMetni_EN" = @en, "SatirMetni_FR" = @fr,
                    "SatirMetni_DE" = @de, "SatirMetni_RO" = @ro, "SatirMetni_AR" = @ar, "SatirMetni_RU" = @ru,
                    "Fiyat" = @fiyat, "ParaBirimi" = @para, "SortOrder" = @sort
                WHERE "Id" = @id;
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", d.Id);
            cmd.Parameters.AddWithValue("tr", d.SatirMetni ?? "");
            cmd.Parameters.AddWithValue("en", d.SatirMetni_EN ?? "");
            cmd.Parameters.AddWithValue("fr", d.SatirMetni_FR ?? "");
            cmd.Parameters.AddWithValue("de", d.SatirMetni_DE ?? "");
            cmd.Parameters.AddWithValue("ro", d.SatirMetni_RO ?? "");
            cmd.Parameters.AddWithValue("ar", d.SatirMetni_AR ?? "");
            cmd.Parameters.AddWithValue("ru", d.SatirMetni_RU ?? "");
            cmd.Parameters.AddWithValue("fiyat", (object?)d.Fiyat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("para", (object?)d.ParaBirimi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sort", d.SortOrder);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>Tek bir detay satirini siler.</summary>
        public async Task DeleteDetayAsync(int detayId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("""DELETE FROM "YLArabaslikDetaylar" WHERE "Id" = @id;""", conn);
            cmd.Parameters.AddWithValue("id", detayId);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Tek bir detay satirinin Fiyat (ve opsiyonel ParaBirimi) alanini gunceller.
        /// Quote editor icindeki sablon panelinden hizli fiyat duzenleme icin kullanilir.
        /// </summary>
        public async Task UpdateDetayFiyatAsync(int detayId, decimal? fiyat, string? paraBirimi = null)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLArabaslikDetaylar"
                SET "Fiyat" = @fiyat,
                    "ParaBirimi" = COALESCE(@para, "ParaBirimi")
                WHERE "Id" = @id;
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", detayId);
            cmd.Parameters.AddWithValue("fiyat", (object?)fiyat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("para", (object?)paraBirimi ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Tek bir detay satirinin Deger alanini (TablTipi=1 icin "9.500 mm" gibi)
        /// gunceller. Deger property'si ParaBirimi kolonu uzerinde duruyor.
        /// </summary>
        public async Task UpdateDetayDegerAsync(int detayId, string? deger)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLArabaslikDetaylar"
                SET "ParaBirimi" = @deger
                WHERE "Id" = @id;
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", detayId);
            cmd.Parameters.AddWithValue("deger", (object?)deger ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Transaction-safe save: grup header + tum detaylar (delete & re-insert)
        /// </summary>
        public async Task SaveGrupWithDetaylarAsync(ArabaslikGrupModel grup, List<ArabaslikDetayModel> detaylar)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                if (grup.Id > 0)
                {
                    // Update grup header
                    const string updateSql = """
                        UPDATE "YLArabaslikGruplar" SET
                            "GrupAdi" = @grupAdi, "GrupAdi_EN" = @en, "GrupAdi_FR" = @fr,
                            "GrupAdi_DE" = @de, "GrupAdi_RO" = @ro, "GrupAdi_AR" = @ar, "GrupAdi_RU" = @ru,
                            "TablTipi" = @tablTipi, "SortOrder" = @sortOrder
                        WHERE "Id" = @id;
                        """;
                    using var updateCmd = new NpgsqlCommand(updateSql, conn, tx);
                    updateCmd.Parameters.AddWithValue("id", grup.Id);
                    updateCmd.Parameters.AddWithValue("grupAdi", grup.GrupAdi);
                    updateCmd.Parameters.AddWithValue("en", grup.GrupAdi_EN);
                    updateCmd.Parameters.AddWithValue("fr", grup.GrupAdi_FR);
                    updateCmd.Parameters.AddWithValue("de", grup.GrupAdi_DE);
                    updateCmd.Parameters.AddWithValue("ro", grup.GrupAdi_RO);
                    updateCmd.Parameters.AddWithValue("ar", grup.GrupAdi_AR);
                    updateCmd.Parameters.AddWithValue("ru", grup.GrupAdi_RU);
                    updateCmd.Parameters.AddWithValue("tablTipi", grup.TablTipi);
                    updateCmd.Parameters.AddWithValue("sortOrder", grup.SortOrder);
                    await updateCmd.ExecuteNonQueryAsync();

                    // Delete existing detail rows
                    const string deleteSql = """DELETE FROM "YLArabaslikDetaylar" WHERE "GrupId" = @grupId;""";
                    using var deleteCmd = new NpgsqlCommand(deleteSql, conn, tx);
                    deleteCmd.Parameters.AddWithValue("grupId", grup.Id);
                    await deleteCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new grup
                    const string insertSql = """
                        INSERT INTO "YLArabaslikGruplar"
                            ("GrupAdi", "GrupAdi_EN", "GrupAdi_FR", "GrupAdi_DE",
                             "GrupAdi_RO", "GrupAdi_AR", "GrupAdi_RU",
                             "TablTipi", "SortOrder", "IsActive", "CreatedDate", "CreatedBy")
                        VALUES (@grupAdi, @en, @fr, @de, @ro, @ar, @ru, @tablTipi, @sortOrder, TRUE, NOW(), @createdBy)
                        RETURNING "Id";
                        """;
                    using var insertCmd = new NpgsqlCommand(insertSql, conn, tx);
                    insertCmd.Parameters.AddWithValue("grupAdi", grup.GrupAdi);
                    insertCmd.Parameters.AddWithValue("en", grup.GrupAdi_EN);
                    insertCmd.Parameters.AddWithValue("fr", grup.GrupAdi_FR);
                    insertCmd.Parameters.AddWithValue("de", grup.GrupAdi_DE);
                    insertCmd.Parameters.AddWithValue("ro", grup.GrupAdi_RO);
                    insertCmd.Parameters.AddWithValue("ar", grup.GrupAdi_AR);
                    insertCmd.Parameters.AddWithValue("ru", grup.GrupAdi_RU);
                    insertCmd.Parameters.AddWithValue("tablTipi", grup.TablTipi);
                    insertCmd.Parameters.AddWithValue("sortOrder", grup.SortOrder);
                    insertCmd.Parameters.AddWithValue("createdBy", _auth.FullName ?? "");
                    var result = await insertCmd.ExecuteScalarAsync();
                    grup.Id = Convert.ToInt32(result);
                }

                // Re-insert detail rows with correct SortOrder
                for (int i = 0; i < detaylar.Count; i++)
                {
                    var d = detaylar[i];
                    const string detaySql = """
                        INSERT INTO "YLArabaslikDetaylar"
                            ("GrupId", "SatirMetni", "SatirMetni_EN", "SatirMetni_FR",
                             "SatirMetni_DE", "SatirMetni_RO", "SatirMetni_AR", "SatirMetni_RU",
                             "Fiyat", "ParaBirimi", "SortOrder")
                        VALUES (@grupId, @tr, @en, @fr, @de, @ro, @ar, @ru, @fiyat, @para, @sort);
                        """;
                    // Not: TablTipi=1 satirlarinda d.Deger setter'i d.ParaBirimi'yi doldurur,
                    // @para parametresi hem Deger hem de klasik para birimi degerini tasir.
                    using var detayCmd = new NpgsqlCommand(detaySql, conn, tx);
                    detayCmd.Parameters.AddWithValue("grupId", grup.Id);
                    detayCmd.Parameters.AddWithValue("tr", d.SatirMetni);
                    detayCmd.Parameters.AddWithValue("en", d.SatirMetni_EN);
                    detayCmd.Parameters.AddWithValue("fr", d.SatirMetni_FR);
                    detayCmd.Parameters.AddWithValue("de", d.SatirMetni_DE);
                    detayCmd.Parameters.AddWithValue("ro", d.SatirMetni_RO);
                    detayCmd.Parameters.AddWithValue("ar", d.SatirMetni_AR);
                    detayCmd.Parameters.AddWithValue("ru", d.SatirMetni_RU);
                    detayCmd.Parameters.AddWithValue("fiyat", (object?)d.Fiyat ?? DBNull.Value);
                    detayCmd.Parameters.AddWithValue("para", (object?)d.ParaBirimi ?? DBNull.Value);
                    detayCmd.Parameters.AddWithValue("sort", i);
                    await detayCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
