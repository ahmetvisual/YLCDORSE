using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class QuoteSpecModel
    {
        public int Id { get; set; }
        public int TeklifId { get; set; }
        public string Grup { get; set; } = "";
        public int GrupSiraNo { get; set; }
        public string Ozellik { get; set; } = "";
        public string Deger { get; set; } = "";
        public int SiraNo { get; set; }
    }

    public class QuoteSpecService
    {
        private readonly DatabaseHelper _db;
        private bool _schemaEnsured;

        public QuoteSpecService(DatabaseHelper db)
        {
            _db = db;
        }

        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            const string sql = """
                CREATE TABLE IF NOT EXISTS "YLTeklifOzellikleri" (
                    "Id"         SERIAL PRIMARY KEY,
                    "TeklifId"   INTEGER NOT NULL,
                    "Grup"       VARCHAR(100) NOT NULL DEFAULT '',
                    "GrupSiraNo" INTEGER NOT NULL DEFAULT 0,
                    "Ozellik"    VARCHAR(300) NOT NULL DEFAULT '',
                    "Deger"      VARCHAR(500),
                    "SiraNo"     INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX IF NOT EXISTS idx_yltekozel_teklif
                    ON "YLTeklifOzellikleri"("TeklifId");
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            _schemaEnsured = true;
        }

        public async Task<List<QuoteSpecModel>> GetSpecsAsync(int quoteId)
        {
            var list = new List<QuoteSpecModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                SELECT "Id","TeklifId","Grup","GrupSiraNo","Ozellik","Deger","SiraNo"
                FROM "YLTeklifOzellikleri"
                WHERE "TeklifId" = @tid
                ORDER BY "GrupSiraNo", "SiraNo";
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("tid", quoteId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new QuoteSpecModel
                {
                    Id = r.GetInt32(0),
                    TeklifId = r.GetInt32(1),
                    Grup = r.IsDBNull(2) ? "" : r.GetString(2),
                    GrupSiraNo = r.GetInt32(3),
                    Ozellik = r.IsDBNull(4) ? "" : r.GetString(4),
                    Deger = r.IsDBNull(5) ? "" : r.GetString(5),
                    SiraNo = r.GetInt32(6)
                });
            }
            return list;
        }

        /// <summary>
        /// Mevcut tum ozelliklerini siler, yenilerini ekler (replace-all yaklasimi).
        /// </summary>
        public async Task SaveSpecsAsync(int quoteId, List<QuoteSpecModel> specs)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            using var tx = await conn.BeginTransactionAsync();

            // Eski kayitlari sil
            using (var del = new NpgsqlCommand(
                "DELETE FROM \"YLTeklifOzellikleri\" WHERE \"TeklifId\" = @tid", conn, tx))
            {
                del.Parameters.AddWithValue("tid", quoteId);
                await del.ExecuteNonQueryAsync();
            }

            // Yenilerini ekle
            for (int i = 0; i < specs.Count; i++)
            {
                var s = specs[i];
                s.TeklifId = quoteId;
                s.SiraNo = i;

                const string ins = """
                    INSERT INTO "YLTeklifOzellikleri"
                        ("TeklifId","Grup","GrupSiraNo","Ozellik","Deger","SiraNo")
                    VALUES (@tid,@grup,@gs,@ozellik,@deger,@sira)
                    RETURNING "Id";
                    """;
                using var cmd = new NpgsqlCommand(ins, conn, tx);
                cmd.Parameters.AddWithValue("tid", quoteId);
                cmd.Parameters.AddWithValue("grup", s.Grup);
                cmd.Parameters.AddWithValue("gs", s.GrupSiraNo);
                cmd.Parameters.AddWithValue("ozellik", s.Ozellik);
                cmd.Parameters.AddWithValue("deger", (object?)s.Deger ?? DBNull.Value);
                cmd.Parameters.AddWithValue("sira", s.SiraNo);
                s.Id = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            }

            await tx.CommitAsync();
        }

        public async Task DeleteSpecsByQuoteAsync(int quoteId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);
            using var cmd = new NpgsqlCommand(
                "DELETE FROM \"YLTeklifOzellikleri\" WHERE \"TeklifId\" = @tid", conn);
            cmd.Parameters.AddWithValue("tid", quoteId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
