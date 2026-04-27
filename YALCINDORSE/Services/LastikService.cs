using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    /// <summary>Lastik katalogu satiri — Marka/Model/Ebat + Yuk Indeksi + Hiz simgesi.</summary>
    public class LastikKatalogModel
    {
        public int    Id         { get; set; }
        public string Marka      { get; set; } = "";   // BRAND  (PIRELLI, CONTINENTAL...)
        public string Model      { get; set; } = "";   // NAME   (FG85, ITINERIS T, ...)
        public string Ebat       { get; set; } = "";   // TYRE+RIM  ("385/65 R 22.5")
        public string YukIndeksi { get; set; } = "";   // LI SINGLE/DOUBLE  ("164/161")
        public string HizSimgesi { get; set; } = "";   // SPEED ("K")
        public bool   AktifMi    { get; set; } = true;
        public int    SortOrder  { get; set; }

        /// <summary>Dropdown'da ve quote.Lastik string'inde gosterilen format.</summary>
        public string DisplayText
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Marka))      parts.Add(Marka);
                if (!string.IsNullOrWhiteSpace(Model))      parts.Add(Model);
                if (!string.IsNullOrWhiteSpace(Ebat))       parts.Add(Ebat);
                var liSpd = string.Join(" ",
                    new[] { YukIndeksi, HizSimgesi }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(liSpd))      parts.Add(liSpd);
                return string.Join("  ·  ", parts);
            }
        }
    }

    /// <summary>
    /// Lastik katalogu CRUD servisi. Tablo bossa barandrive/LASTIK TABLOSU.xlsx'ten
    /// elde edilen 204 satir (PIRELLI + CONTINENTAL) otomatik seed edilir.
    /// </summary>
    public class LastikService
    {
        private readonly DatabaseHelper _db;
        private bool _schemaEnsured;
        private static readonly SemaphoreSlim _schemaLock = new(1, 1);

        public LastikService(DatabaseHelper db) { _db = db; }

        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            await _schemaLock.WaitAsync();
            try
            {
                if (_schemaEnsured) return;
                _schemaEnsured = true;

                const string ddl = """
                    CREATE TABLE IF NOT EXISTS "YLLastikKatalogu" (
                        "Id"         SERIAL PRIMARY KEY,
                        "Marka"      TEXT NOT NULL DEFAULT '',
                        "Model"      TEXT NOT NULL DEFAULT '',
                        "Ebat"       TEXT NOT NULL DEFAULT '',
                        "YukIndeksi" TEXT NOT NULL DEFAULT '',
                        "HizSimgesi" TEXT NOT NULL DEFAULT '',
                        "AktifMi"    BOOLEAN NOT NULL DEFAULT TRUE,
                        "SortOrder"  INTEGER NOT NULL DEFAULT 0
                    )
                    """;
                using (var cmd = new NpgsqlCommand(ddl, conn))
                    await cmd.ExecuteNonQueryAsync();

                using (var idxCmd = new NpgsqlCommand(
                    @"CREATE INDEX IF NOT EXISTS idx_yllastikkatalogu_aktif ON ""YLLastikKatalogu""(""AktifMi"", ""SortOrder"")",
                    conn))
                    await idxCmd.ExecuteNonQueryAsync();

                // Seed: yalniz tablo bossa. Kullanici UI'dan ekledi/duzenlediyse atlanir.
                try
                {
                    using var checkCmd = new NpgsqlCommand(
                        @"SELECT COUNT(*) FROM ""YLLastikKatalogu""", conn);
                    var rowCount = Convert.ToInt64(
                        await checkCmd.ExecuteScalarAsync() ?? 0L);
                    if (rowCount == 0)
                    {
                        using var seedCmd = new NpgsqlCommand(SEED_SQL, conn);
                        await seedCmd.ExecuteNonQueryAsync();
                    }
                }
                catch { /* seed hatasi: kullanici UI'dan elle ekleyebilir */ }
            }
            finally { _schemaLock.Release(); }
        }

        public async Task<List<LastikKatalogModel>> GetAllAsync(bool onlyActive = true)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            var sql = @"SELECT ""Id"",""Marka"",""Model"",""Ebat"",""YukIndeksi"",""HizSimgesi"",""AktifMi"",""SortOrder"" FROM ""YLLastikKatalogu""";
            if (onlyActive) sql += @" WHERE ""AktifMi"" = TRUE";
            sql += @" ORDER BY ""SortOrder"", ""Id""";

            var list = new List<LastikKatalogModel>();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new LastikKatalogModel
                {
                    Id         = r.GetInt32(0),
                    Marka      = r.IsDBNull(1) ? "" : r.GetString(1),
                    Model      = r.IsDBNull(2) ? "" : r.GetString(2),
                    Ebat       = r.IsDBNull(3) ? "" : r.GetString(3),
                    YukIndeksi = r.IsDBNull(4) ? "" : r.GetString(4),
                    HizSimgesi = r.IsDBNull(5) ? "" : r.GetString(5),
                    AktifMi    = r.GetBoolean(6),
                    SortOrder  = r.GetInt32(7),
                });
            }
            return list;
        }

        public async Task<int> AddAsync(LastikKatalogModel m)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                INSERT INTO "YLLastikKatalogu"
                    ("Marka","Model","Ebat","YukIndeksi","HizSimgesi","AktifMi","SortOrder")
                VALUES (@Marka,@Model,@Ebat,@YukIndeksi,@HizSimgesi,@AktifMi,@SortOrder)
                RETURNING "Id"
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("Marka",      m.Marka      ?? "");
            cmd.Parameters.AddWithValue("Model",      m.Model      ?? "");
            cmd.Parameters.AddWithValue("Ebat",       m.Ebat       ?? "");
            cmd.Parameters.AddWithValue("YukIndeksi", m.YukIndeksi ?? "");
            cmd.Parameters.AddWithValue("HizSimgesi", m.HizSimgesi ?? "");
            cmd.Parameters.AddWithValue("AktifMi",    m.AktifMi);
            cmd.Parameters.AddWithValue("SortOrder",  m.SortOrder);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
        }

        public async Task UpdateAsync(LastikKatalogModel m)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLLastikKatalogu" SET
                    "Marka" = @Marka, "Model" = @Model, "Ebat" = @Ebat,
                    "YukIndeksi" = @YukIndeksi, "HizSimgesi" = @HizSimgesi,
                    "AktifMi" = @AktifMi, "SortOrder" = @SortOrder
                WHERE "Id" = @Id
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("Id",         m.Id);
            cmd.Parameters.AddWithValue("Marka",      m.Marka      ?? "");
            cmd.Parameters.AddWithValue("Model",      m.Model      ?? "");
            cmd.Parameters.AddWithValue("Ebat",       m.Ebat       ?? "");
            cmd.Parameters.AddWithValue("YukIndeksi", m.YukIndeksi ?? "");
            cmd.Parameters.AddWithValue("HizSimgesi", m.HizSimgesi ?? "");
            cmd.Parameters.AddWithValue("AktifMi",    m.AktifMi);
            cmd.Parameters.AddWithValue("SortOrder",  m.SortOrder);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            using var cmd = new NpgsqlCommand(
                @"DELETE FROM ""YLLastikKatalogu"" WHERE ""Id"" = @Id", conn);
            cmd.Parameters.AddWithValue("Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── 204 satir tek bulk INSERT — barandrive/LASTIK TABLOSU.xlsx (TYRE & RIM) ──
        private const string SEED_SQL = """
            INSERT INTO "YLLastikKatalogu" ("Marka","Model","Ebat","YukIndeksi","HizSimgesi","AktifMi","SortOrder") VALUES
                ('PIRELLI','FG85','11 R 22.5','148/145','K',TRUE,10),
                ('PIRELLI','FR25','11 R 22.5','148/145','L',TRUE,20),
                ('PIRELLI','FG85','12 R 22.5','152/148','L',TRUE,30),
                ('PIRELLI','FR25','12 R 22.5','152/148','M',TRUE,40),
                ('PIRELLI','TG85','12 R 22.5','152/148','L',TRUE,50),
                ('PIRELLI','TR25','12 R 22.5','152/148','M',TRUE,60),
                ('CONTINENTAL','Conti Hybrid HS3+ / Steer axle','12 R 22.5','152/148','L',TRUE,70),
                ('PIRELLI','FG:01™ II','13 R 22.5','156/156','K',TRUE,80),
                ('PIRELLI','FG88™','13 R 22.5','156/156','K',TRUE,90),
                ('PIRELLI','G02 ECO PRO DRIVE','13 R 22.5','158/156','K',TRUE,100),
                ('PIRELLI','G02 ECO PRO MULTIAXLE','13 R 22.5','158/156','K',TRUE,110),
                ('PIRELLI','TG:01™ II','13 R 22.5','156/156','K',TRUE,120),
                ('PIRELLI','TG88™','13 R 22.5','156/156','K',TRUE,130),
                ('PIRELLI','TQ:01™ ROCK','13 R 22.5','158/156','G',TRUE,140),
                ('PIRELLI','FG85','12.00 R 20','154/150','K',TRUE,150),
                ('PIRELLI','FG85','12.00 R 24','160/156','K',TRUE,160),
                ('PIRELLI','TG85','12.00 R 20','154/150','K',TRUE,170),
                ('PIRELLI','TG85','12.00 R 20','154/150','K',TRUE,180),
                ('PIRELLI','TG85','12.00 R 24','160/156','K',TRUE,190),
                ('PIRELLI','S02 PISTA','14.00 R 20','164/160','G',TRUE,200),
                ('PIRELLI','S02 PISTA','14.00 R 20','164/160','K',TRUE,210),
                ('PIRELLI','H02 PROFUEL™ STEER','185/55 R 22.5','160','K',TRUE,220),
                ('PIRELLI','ST:01™','205/65 R 17.5','129/127','J',TRUE,230),
                ('PIRELLI','FR:01™ TRIATHLON™ 17.5”','205/75 R 17.5','124/122','M',TRUE,240),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','205/75 R 17.5','124/122','M',TRUE,250),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','205/75 R 17.5','124/122','M',TRUE,260),
                ('PIRELLI','TR:01™ TRIATHLON™ 17.5”','205/75 R 17.5','124/122','M',TRUE,270),
                ('CONTINENTAL','Conti Hybrid LS3 / Steer axle','205/75 R 17.5','124/122','M',TRUE,280),
                ('CONTINENTAL','Conti Hybrid LD3 / Drive axle','205/75 R 17.5','124/122','M',TRUE,290),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','215/75 R 17.5','126/124','M',TRUE,300),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','215/75 R 17.5','128/126','M',TRUE,310),
                ('PIRELLI','ST:01™','215/75 R 17.5','135/133','J',TRUE,320),
                ('CONTINENTAL','Conti Hybrid LS3 / Steer axle','215/75 R 17.5','126/124','M',TRUE,330),
                ('CONTINENTAL','Conti Hybrid LD3 / Drive axle','215/75 R 17.5','126/124','M',TRUE,340),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','225/75 R 17.5','129/127','M',TRUE,350),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','225/75 R 17.5','129/127','M',TRUE,360),
                ('CONTINENTAL','Conti Hybrid LS3 / Steer axle','225/75 R 17.5','129/127','M',TRUE,370),
                ('CONTINENTAL','Conti Hybrid LD3 / Drive axle','225/75 R 17.5','129/127','M',TRUE,380),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','235/75 R 17.5','132/130','M',TRUE,390),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','235/75 R 17.5','132/130','M',TRUE,400),
                ('PIRELLI','ST:01™','235/75 R 17.5','143/141','J',TRUE,410),
                ('CONTINENTAL','Conti Hybrid LS3 / Steer axle','235/75 R 17.5','132/130','M',TRUE,420),
                ('CONTINENTAL','Conti Hybrid LD3 / Drive axle','235/75 R 17.5','132/130','M',TRUE,430),
                ('PIRELLI','FR:01™ TRIATHLON™ 17.5”','245/70 R 17.5','136/134','M',TRUE,440),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','245/70 R 17.5','136/134','M',TRUE,450),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','245/70 R 19.5','136/134','M',TRUE,460),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','245/70 R 17.5','136/134','M',TRUE,470),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','245/70 R 19.5','136/134','M',TRUE,480),
                ('PIRELLI','ST:01™','245/70 R 19.5','141/140','J',TRUE,490),
                ('PIRELLI','ST:01™','245/70 R 17.5','143/141','J',TRUE,500),
                ('PIRELLI','TR:01™ TRIATHLON™ 17.5”','245/70 R 17.5','136/134','M',TRUE,510),
                ('PIRELLI','R02 PRO TRAILER','245/70 R 17.5','143/141','L',TRUE,520),
                ('CONTINENTAL','Conti Hybrid HS3 19.5 / Steer axle','245/70 R 19.5','136/134','M',TRUE,530),
                ('CONTINENTAL','Conti Hybrid HD3 19.5 / Drive axle','245/70 R 19.5','136/134','M',TRUE,540),
                ('CONTINENTAL','Conti Hybrid HT3+ 19.5 / Trailer axle','245/70 R 19.5','141/140','K',TRUE,550),
                ('CONTINENTAL','Conti Hybrid LS3 / Steer axle','245/70 R 17.5','136/134','M',TRUE,560),
                ('CONTINENTAL','Conti Hybrid LD3 / Drive axle','245/70 R 17.5','136/134','M',TRUE,570),
                ('PIRELLI','FH15','255/70 R 22.5','140/137','M',TRUE,580),
                ('PIRELLI','MG:01™ II','265/70 R 19.5','143/141','J',TRUE,590),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','265/70 R 17.5','140/138','M',TRUE,600),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','265/70 R 19.5','140/138','M',TRUE,610),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','265/70 R 17.5','140/138','M',TRUE,620),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','265/70 R 19.5','140/138','M',TRUE,630),
                ('PIRELLI','ST:01™','265/70 R 19.5','143/141','J',TRUE,640),
                ('CONTINENTAL','Conti Hybrid HS3 19.5 / Steer axle','265/70 R 19.5','140/138','M',TRUE,650),
                ('CONTINENTAL','Conti Hybrid HD3 19.5 / Drive axle','265/70 R 19.5','140/138','M',TRUE,660),
                ('CONTINENTAL','Conti Hybrid HT3+ 19.5 / Trailer axle','265/70 R 19.5','143/141','K',TRUE,670),
                ('CONTINENTAL','Conti Hybrid LS3 / Steer axle','265/70 R 17.5','139/136','M',TRUE,680),
                ('CONTINENTAL','Conti Hybrid LD3 / Drive axle','265/70 R 17.5','139/136','M',TRUE,690),
                ('PIRELLI','FH:01™ ENERGY™','275/70 R 22.5','148/145','M',TRUE,700),
                ('PIRELLI','MC88 III M+S AMARANTO','275/70 R 22.5','150/148','J',TRUE,710),
                ('PIRELLI','TH:01™ ENERGY™','275/70 R 22.5','148/145','M',TRUE,720),
                ('PIRELLI','U02 URBAN-e PRO MULTIAXLE','275/70 R 22.5','152/148','J',TRUE,730),
                ('CONTINENTAL','Conti Hybrid HS3+ / Steer axle','275/70 R 22.5','148/145','M',TRUE,740),
                ('CONTINENTAL','Conti Hybrid HD3 / Drive axle','275/70 R 22.5','148/145','M',TRUE,750),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','285/70 R 19.5','146/144','L',TRUE,760),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','285/70 R 19.5','146/144','L',TRUE,770),
                ('PIRELLI','ST:01™','285/70 R 19.5','150/148','J',TRUE,780),
                ('CONTINENTAL','Conti Hybrid HS3 19.5 / Steer axle','285/70 R 19.5','146/144','M',TRUE,790),
                ('CONTINENTAL','Conti Hybrid HD3 19.5 / Drive axle','285/70 R 19.5','146/144','M',TRUE,800),
                ('CONTINENTAL','Conti Hybrid HT3+ 19.5 / Trailer axle','285/70 R 19.5','150/148','K',TRUE,810),
                ('CONTINENTAL','Conti EcoPlus HD3+ / Drive axle','295/55 R 22.5','147/145','K',TRUE,820),
                ('CONTINENTAL','Conti Hybrid HD3 / Drive axle','295/55 R 22.5','147/145','K',TRUE,830),
                ('PIRELLI','FH:01™ ENERGY™','295/60 R 22.5','150/147','L',TRUE,840),
                ('PIRELLI','TH:01™ ENERGY™','295/60 R 22.5','150/147','L',TRUE,850),
                ('PIRELLI','TR:01™ TRIATHLON™ 22.5”','295/60 R 22.5','150/147','K',TRUE,860),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','295/60 R 22.5','150/147','L',TRUE,870),
                ('CONTINENTAL','Conti EcoPlus HD3+ / Drive axle','295/60 R 22.5','150/147','L',TRUE,880),
                ('CONTINENTAL','Conti Hybrid HD3 / Drive axle','295/60 R 22.5','150/147','L',TRUE,890),
                ('PIRELLI','FG:01™ II','295/80 R 22.5','152/148','L',TRUE,900),
                ('PIRELLI','FH:01™ COACH','295/80 R 22.5','156/149','M',TRUE,910),
                ('PIRELLI','FR:01™ TRIATHLON™ 22.5”','295/80 R 22.5','154/149','M',TRUE,920),
                ('PIRELLI','FW:01™','295/80 R 22.5','154/149','M',TRUE,930),
                ('PIRELLI','G02 ECO PRO DRIVE','295/80 R 22.5','152/149','L',TRUE,940),
                ('PIRELLI','G02 ECO PRO MULTIAXLE','295/80 R 22.5','154/149','L',TRUE,950),
                ('PIRELLI','ITINERIS™ D','295/80 R 22.5','152/148','M',TRUE,960),
                ('PIRELLI','ITINERIS™ S','295/80 R 22.5','154/149','M',TRUE,970),
                ('PIRELLI','TG:01™ II','295/80 R 22.5','152/148','L',TRUE,980),
                ('PIRELLI','TH:01™ COACH','295/80 R 22.5','152/148','M',TRUE,990),
                ('PIRELLI','TR:01™ TRIATHLON™ 22.5”','295/80 R 22.5','152/148','M',TRUE,1000),
                ('PIRELLI','TW:01™','295/80 R 22.5','152/148','M',TRUE,1010),
                ('CONTINENTAL','Conti Hybrid HS5 / Steer axle','295/80 R 22.5','154/149','M',TRUE,1020),
                ('CONTINENTAL','Conti Hybrid HS5 / Drive axle','295/80 R 22.5','152/148','M',TRUE,1030),
                ('CONTINENTAL','Conti Hybrid HD3 / Drive axle','295/80 R 22.5','152/148','M',TRUE,1040),
                ('PIRELLI','FH:01™ ENERGY™','305/70 R 22.5','152/150','L',TRUE,1050),
                ('PIRELLI','FR:01™','305/70 R 19.5','148/145','M',TRUE,1060),
                ('PIRELLI','R02 PROFUEL™ DRIVE 17.5” & 19.5”','305/70 R 19.5','148/145','M',TRUE,1070),
                ('PIRELLI','R02 PROFUEL™ STEER 17.5” & 19.5”','305/70 R 19.5','148/145','M',TRUE,1080),
                ('PIRELLI','TR:01™','305/70 R 19.5','148/145','M',TRUE,1090),
                ('CONTINENTAL','Conti Hybrid HS3 19.5 / Steer axle','305/70 R 19.5','148/145','M',TRUE,1100),
                ('CONTINENTAL','Conti Hybrid HD3 19.5 / Drive axle','305/70 R 19.5','148/145','M',TRUE,1110),
                ('PIRELLI','TH:01™ ENERGY™','305/70 R 22.5','152/150','L',TRUE,1120),
                ('CONTINENTAL','Conti EcoPlus HD3+ / Drive axle','315/45 R 22.5','147/145','L',TRUE,1130),
                ('PIRELLI','FR:01™ TRIATHLON™ 22.5”','315/60 R 22.5','154/148','L',TRUE,1140),
                ('PIRELLI','TH:01™ PROWAY™','315/60 R 22.5','152/148','L',TRUE,1150),
                ('PIRELLI','TR:01™ TRIATHLON™ 22.5”','315/60 R 22.5','152/148','L',TRUE,1160),
                ('PIRELLI','U02 URBAN-e PRO MULTIAXLE','315/60 R 22.5','156/150','J',TRUE,1170),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','315/60 R 22.5','154/150','L',TRUE,1180),
                ('CONTINENTAL','Conti EcoPlus HD3+ / Drive axle','315/60 R 22.5','152/148','L',TRUE,1190),
                ('CONTINENTAL','Conti Hybrid HD3 / Drive axle','315/60 R 22.5','152/148','L',TRUE,1200),
                ('PIRELLI','FH:01™ PROWAY™','315/60 R 22.5','154/148','L',TRUE,1210),
                ('PIRELLI','FH:01™ PROWAY™','315/70 R 22.5','156/156','L',TRUE,1220),
                ('PIRELLI','FR:01™ TRIATHLON™ 22.5”','315/70 R 22.5','156/156','L',TRUE,1230),
                ('PIRELLI','FW:01™','315/70 R 22.5','156/156','L',TRUE,1240),
                ('PIRELLI','H02 PROFUEL™ DRIVE','315/70 R 22.5','154/150','L',TRUE,1250),
                ('PIRELLI','H02 PROFUEL™ STEER','315/70 R 22.5','156/156','L',TRUE,1260),
                ('PIRELLI','ITINERIS™ D','315/70 R 22.5','154/150','L',TRUE,1270),
                ('PIRELLI','ITINERIS™ S','315/70 R 22.5','156/156','L',TRUE,1280),
                ('PIRELLI','R02 PROFUEL™ STEER 22.5”','315/70 R 22.5','156/156','L',TRUE,1290),
                ('PIRELLI','TH:01™ PROWAY™','315/70 R 22.5','154/150','L',TRUE,1300),
                ('PIRELLI','TR:01™ TRIATHLON™ 22.5”','315/70 R 22.5','154/150','L',TRUE,1310),
                ('PIRELLI','TW:01™','315/70 R 22.5','154/150','L',TRUE,1320),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','315/70 R 22.5','156/150','L',TRUE,1330),
                ('CONTINENTAL','Conti EcoPlus HD3+ / Drive axle','315/70 R 22.5','154/150','L',TRUE,1340),
                ('CONTINENTAL','Conti EfficientPro S+ / Steer axle','315/70 R 22.5','156/150','L',TRUE,1350),
                ('CONTINENTAL','Conti EfficientPro S+ / Drive axle','315/70 R 22.5','154/150','L',TRUE,1360),
                ('CONTINENTAL','Conti Hybrid HS5 / Steer axle','315/70 R 22.5','156/150','L',TRUE,1370),
                ('CONTINENTAL','Conti Hybrid HS5 / Drive axle','315/70 R 22.5','154/150','L',TRUE,1380),
                ('PIRELLI','FG:01™ II','315/80 R 22.5','156/156','K',TRUE,1390),
                ('PIRELLI','FG88™','315/80 R 22.5','156/156','K',TRUE,1400),
                ('PIRELLI','FH:01™ COACH','315/80 R 22.5','158/150','L',TRUE,1410),
                ('PIRELLI','FH:01™ PROWAY™','315/80 R 22.5','158/150','L',TRUE,1420),
                ('PIRELLI','FR:01™ TRIATHLON™ 22.5”','315/80 R 22.5','156/156','L',TRUE,1430),
                ('PIRELLI','FW:01™','315/80 R 22.5','156/156','L',TRUE,1440),
                ('PIRELLI','G02 ECO PRO DRIVE','315/80 R 22.5','158/156','K',TRUE,1450),
                ('PIRELLI','G02 ECO PRO MULTIAXLE','315/80 R 22.5','158/156','K',TRUE,1460),
                ('PIRELLI','H02 PROFUEL™ DRIVE','315/80 R 22.5','158/150','L',TRUE,1470),
                ('PIRELLI','H02 PROFUEL™ STEER','315/80 R 22.5','158/150','L',TRUE,1480),
                ('PIRELLI','ITINERIS™ D','315/80 R 22.5','156/156','L',TRUE,1490),
                ('PIRELLI','ITINERIS™ S','315/80 R 22.5','156/156','L',TRUE,1500),
                ('PIRELLI','TG:01™ II','315/80 R 22.5','156/156','K',TRUE,1510),
                ('PIRELLI','TG88™','315/80 R 22.5','156/156','K',TRUE,1520),
                ('PIRELLI','TH:01™ PROWAY™','315/80 R 22.5','156/156','L',TRUE,1530),
                ('PIRELLI','TR:01™ TRIATHLON™ 22.5”','315/80 R 22.5','156/156','L',TRUE,1540),
                ('PIRELLI','TW:01™','315/80 R 22.5','156/156','L',TRUE,1550),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','315/80 R 22.5','156/150','L',TRUE,1560),
                ('CONTINENTAL','Conti EcoPlus HD3+ / Drive axle','315/80 R 22.5','156/150','L',TRUE,1570),
                ('CONTINENTAL','Conti Hybrid HS5 / Steer axle','315/80 R 22.5','156/150','L',TRUE,1580),
                ('CONTINENTAL','Conti Hybrid HS5 / Drive axle','315/80 R 22.5','156/150','M',TRUE,1590),
                ('CONTINENTAL','Conti Hybrid HD3 / Drive axle','315/80 R 22.5','156/150','L',TRUE,1600),
                ('PIRELLI','G02 ON-OFF PRO DRIVE','325/95 R 24','162/160','K',TRUE,1610),
                ('PIRELLI','G02 ON-OFF PRO MULTIAXLE','325/95 R 24','162/160','K',TRUE,1620),
                ('PIRELLI','S02 PISTA','335/80 R 20','149','K',TRUE,1630),
                ('PIRELLI','R02 PROFUEL™ STEER 22.5”','355/50 R 22.5','156','L',TRUE,1640),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','355/60 R 22.5','156','K',TRUE,1650),
                ('PIRELLI','S02 PISTA','365/80 R 20','152','K',TRUE,1660),
                ('PIRELLI','S02 PISTA','365/85 R 20','164/G','',TRUE,1670),
                ('PIRELLI','FH:01™ ENERGY™','385/55 R 22.5','158','L',TRUE,1680),
                ('PIRELLI','FR:01™ TRIATHLON™ 22.5”','385/55 R 22.5','160','K',TRUE,1690),
                ('PIRELLI','FW:01™','385/55 R 22.5','158','L',TRUE,1700),
                ('PIRELLI','H02 PRO TRAILER','385/55 R 22.5','164','K',TRUE,1710),
                ('PIRELLI','ITINERIS™ T','385/55 R 22.5','160','K',TRUE,1720),
                ('PIRELLI','R02 PRO TRAILER','385/55 R 22.5','164','K',TRUE,1730),
                ('PIRELLI','R02 PROFUEL™ STEER 22.5”','385/55 R 22.5','162','K',TRUE,1740),
                ('PIRELLI','ST:01™ NEVERENDING™ ENERGY','385/55 R 22.5','160','K',TRUE,1750),
                ('PIRELLI','ST:01™ TRIATHLON™','385/55 R 22.5','160','K',TRUE,1760),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','385/55 R 22.5','160','K',TRUE,1770),
                ('CONTINENTAL','Conti EcoPlus HT3+ / Trailer axle','385/55 R 22.5','160','K',TRUE,1780),
                ('CONTINENTAL','Conti EfficientPro S+ / Steer axle','385/55 R 22.5','160','K',TRUE,1790),
                ('CONTINENTAL','Conti Hybrid HS5 / Steer axle','385/55 R 22.5','160','K',TRUE,1800),
                ('CONTINENTAL','Conti Hybrid HT3+ / Trailer axle','385/55 R 22.5','160','K',TRUE,1810),
                ('CONTINENTAL','Conti Hybrid HT3 SR / Trailer axle','385/55 R 22.5','146/143','K',TRUE,1820),
                ('CONTINENTAL','Conti Hybrid HT3+ 19.5 / Trailer axle','385/55 R 19.5','156','J',TRUE,1830),
                ('PIRELLI','FH:01™ ENERGY™','385/65 R 22.5','160','K',TRUE,1840),
                ('PIRELLI','FR:01™ TRIATHLON™ 22.5”','385/65 R 22.5','164','K',TRUE,1850),
                ('PIRELLI','FW:01™','385/65 R 22.5','158','L',TRUE,1860),
                ('PIRELLI','G02 PRO MULTIAXLE','385/65 R 22.5','164','K',TRUE,1870),
                ('PIRELLI','H02 PRO TRAILER','385/65 R 22.5','164','K',TRUE,1880),
                ('PIRELLI','H02 PROFUEL™ STEER','385/65 R 22.5','164','K',TRUE,1890),
                ('PIRELLI','ITINERIS™ T','385/65 R 22.5','160','K',TRUE,1900),
                ('PIRELLI','R02 PRO TRAILER','385/65 R 22.5','164','K',TRUE,1910),
                ('PIRELLI','R02 PROFUEL™ STEER 22.5”','385/65 R 22.5','164','K',TRUE,1920),
                ('PIRELLI','ST:01™ NEVERENDING™ ENERGY','385/65 R 22.5','160','K',TRUE,1930),
                ('CONTINENTAL','Conti EcoPlus HS3+ / Steer axle','385/65 R 22.5','160','K',TRUE,1940),
                ('CONTINENTAL','Conti EcoPlus HT3+ / Trailer axle','385/65 R 22.5','160','K',TRUE,1950),
                ('CONTINENTAL','Conti Hybrid HS5 / Steer axle','385/65 R 22.5','164','K',TRUE,1960),
                ('CONTINENTAL','Conti Hybrid HT3+ / Trailer axle','385/65 R 22.5','164','K',TRUE,1970),
                ('CONTINENTAL','Conti Hybrid HT3 ED / Trailer axle','385/65 R 22.5','164','K',TRUE,1980),
                ('CONTINENTAL','Conti Hybrid HT3 SR / Trailer axle','385/65 R 22.5','160','K',TRUE,1990),
                ('PIRELLI','S02 PISTA','395/85 R 20','168','G',TRUE,2000),
                ('PIRELLI','H02 PRO TRAILER','435/50 R 19.5','164','J',TRUE,2010),
                ('CONTINENTAL','Conti Hybrid HT3+ 19.5 / Trailer axle','435/50 R 19.5','160','J',TRUE,2020),
                ('PIRELLI','H02 PRO TRAILER','445/45 R 19.5','164','J',TRUE,2030),
                ('CONTINENTAL','Conti Hybrid HT3+ 19.5 / Trailer axle','445/45 R 19.5','160','J',TRUE,2040)
            """;
    }
}
