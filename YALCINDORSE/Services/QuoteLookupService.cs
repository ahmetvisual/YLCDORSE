using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    /// <summary>
    /// Teklif formundaki dinamik dropdown seceneklerini yoneten servis.
    /// TEKLIF_TIPI, SUSPANSIYON, EXTENSION, GOOSENECK lookup tipleri.
    /// </summary>
    public class QuoteLookupService
    {
        private readonly DatabaseHelper _db;
        private bool _schemaEnsured;

        public QuoteLookupService(DatabaseHelper db) { _db = db; }

        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            _schemaEnsured = true;

            // Her DDL/DML ayri komut olarak calistir — multi-statement batch sorunu yok
            var steps = new[]
            {
                // 1. Tablo olustur
                """
                CREATE TABLE IF NOT EXISTS "YLLookupValues" (
                    "Id"         SERIAL PRIMARY KEY,
                    "LookupType" VARCHAR(50) NOT NULL,
                    "Value"      TEXT        NOT NULL,
                    "SortOrder"  INT         NOT NULL DEFAULT 0,
                    UNIQUE("LookupType", "Value")
                )
                """,

                // 2. Varsayilan Teklif Tipi degerleri
                """
                INSERT INTO "YLLookupValues" ("LookupType", "Value", "SortOrder")
                VALUES
                    ('TEKLIF_TIPI', 'LB',  10),
                    ('TEKLIF_TIPI', 'KB',  20),
                    ('TEKLIF_TIPI', 'SHC', 30),
                    ('TEKLIF_TIPI', 'PLT', 40)
                ON CONFLICT ("LookupType", "Value") DO NOTHING
                """,

                // 3-7. YLTeklifler yeni kolonlar
                """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "TipAdi"      TEXT""",
                """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Lastik"      TEXT""",
                """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Suspansiyon" TEXT""",
                """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Extension"   TEXT""",
                """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Gooseneck"   TEXT""",
            };

            foreach (var sql in steps)
            {
                try
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                catch { /* mevcut tablo/kolon vs. — adim atlayarak devam */ }
            }
        }

        public async Task<List<string>> GetValuesAsync(string lookupType)
        {
            var result = new List<string>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                SELECT "Value" FROM "YLLookupValues"
                WHERE "LookupType" = @type
                ORDER BY "SortOrder", "Value"
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("type", lookupType);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result.Add(r.GetString(0));
            return result;
        }

        public async Task AddValueAsync(string lookupType, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            value = value.Trim().ToUpperInvariant();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                INSERT INTO "YLLookupValues" ("LookupType", "Value")
                VALUES (@type, @value)
                ON CONFLICT ("LookupType", "Value") DO NOTHING
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("type", lookupType);
            cmd.Parameters.AddWithValue("value", value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteValueAsync(string lookupType, string value)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                DELETE FROM "YLLookupValues"
                WHERE "LookupType" = @type AND "Value" = @value
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("type", lookupType);
            cmd.Parameters.AddWithValue("value", value);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
