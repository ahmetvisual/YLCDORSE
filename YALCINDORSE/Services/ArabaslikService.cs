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
        /// Iki kolon tablo (TablTipi=1) icin evrensel deger alani. Orn: "9.500 mm", "8 adet"
        /// </summary>
        public string Deger { get; set; } = "";
    }

    // === SERVICE ===
    public class ArabaslikService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private static bool _schemaMigrated = false;

        public ArabaslikService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        /// <summary>
        /// Yeni "Deger" kolonunu ekler (IF NOT EXISTS — idempotent, hata vermez).
        /// Ilk GetGruplarAsync cagrisinda otomatik calisir.
        /// </summary>
        private async Task EnsureDetaySchemaAsync()
        {
            if (_schemaMigrated) return;
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                const string sql = """
                    ALTER TABLE "YLArabaslikDetaylar"
                    ADD COLUMN IF NOT EXISTS "Deger" varchar(500) NOT NULL DEFAULT '';
                    """;
                await new NpgsqlCommand(sql, conn).ExecuteNonQueryAsync();
            }
            catch { /* sutun zaten varsa veya yetki yoksa sessiz devam */ }
            finally { _schemaMigrated = true; }
        }

        // ─── GRUP CRUD ───────────────────────────────────────

        public async Task<List<ArabaslikGrupModel>> GetGruplarAsync(string search = "")
        {
            await EnsureDetaySchemaAsync();

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

        public async Task DeleteGrupAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Soft delete (IsActive = false)
            const string sql = """UPDATE "YLArabaslikGruplar" SET "IsActive" = FALSE WHERE "Id" = @id;""";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── DETAY CRUD ─────────────────────────────────────

        public async Task<List<ArabaslikDetayModel>> GetDetaylarAsync(int grupId)
        {
            var items = new List<ArabaslikDetayModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT "Id", "GrupId", "SatirMetni", "SatirMetni_EN", "SatirMetni_FR",
                       "SatirMetni_DE", "SatirMetni_RO", "SatirMetni_AR", "SatirMetni_RU",
                       "Fiyat", "ParaBirimi", "SortOrder", "Deger"
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
                    SortOrder = reader.GetInt32(11),
                    Deger = reader.IsDBNull(12) ? "" : reader.GetString(12)
                });
            }
            return items;
        }

        /// <summary>
        /// Transaction-safe save: grup header + tum detaylar (delete & re-insert)
        /// </summary>
        public async Task SaveGrupWithDetaylarAsync(ArabaslikGrupModel grup, List<ArabaslikDetayModel> detaylar)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
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
                             "Fiyat", "ParaBirimi", "SortOrder", "Deger")
                        VALUES (@grupId, @tr, @en, @fr, @de, @ro, @ar, @ru, @fiyat, @para, @sort, @deger);
                        """;
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
                    detayCmd.Parameters.AddWithValue("deger", d.Deger ?? "");
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
