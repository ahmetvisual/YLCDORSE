using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class QuoteAttachmentModel
    {
        public int Id { get; set; }
        public int TeklifId { get; set; }
        public string DosyaAdi { get; set; } = "";
        public string Tip { get; set; } = "";   // "foto" | "teknikresim"
        public string DosyaYolu { get; set; } = "";
        public int SiraNo { get; set; }
        public bool PdfeDahilEt { get; set; } = true;
        public DateTime OlusturmaTarihi { get; set; }
    }

    public class QuoteAttachmentService
    {
        private readonly DatabaseHelper _db;
        private bool _schemaEnsured;

        public QuoteAttachmentService(DatabaseHelper db)
        {
            _db = db;
        }

        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            const string sql = """
                CREATE TABLE IF NOT EXISTS "YLTeklifEkleri" (
                    "Id"              SERIAL PRIMARY KEY,
                    "TeklifId"        INTEGER NOT NULL,
                    "DosyaAdi"        VARCHAR(255) NOT NULL DEFAULT '',
                    "Tip"             VARCHAR(50) NOT NULL DEFAULT 'foto',
                    "DosyaYolu"       VARCHAR(1000),
                    "SiraNo"          INTEGER NOT NULL DEFAULT 0,
                    "PdfeDahilEt"     BOOLEAN NOT NULL DEFAULT TRUE,
                    "OlusturmaTarihi" TIMESTAMP NOT NULL DEFAULT NOW()
                );
                CREATE INDEX IF NOT EXISTS idx_ylteklifekleri_teklif
                    ON "YLTeklifEkleri"("TeklifId");
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
            _schemaEnsured = true;
        }

        /// <summary>Dosya dizini: {AppDataDirectory}/teklif_ekleri/{quoteId}/</summary>
        public static string GetStorageDir(int quoteId)
        {
            return Path.Combine(FileSystem.AppDataDirectory, "teklif_ekleri", quoteId.ToString());
        }

        public async Task<List<QuoteAttachmentModel>> GetAttachmentsAsync(int quoteId)
        {
            var list = new List<QuoteAttachmentModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                SELECT "Id","TeklifId","DosyaAdi","Tip","DosyaYolu","SiraNo","PdfeDahilEt","OlusturmaTarihi"
                FROM "YLTeklifEkleri"
                WHERE "TeklifId" = @tid
                ORDER BY "Tip", "SiraNo";
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("tid", quoteId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new QuoteAttachmentModel
                {
                    Id = r.GetInt32(0),
                    TeklifId = r.GetInt32(1),
                    DosyaAdi = r.IsDBNull(2) ? "" : r.GetString(2),
                    Tip = r.IsDBNull(3) ? "" : r.GetString(3),
                    DosyaYolu = r.IsDBNull(4) ? "" : r.GetString(4),
                    SiraNo = r.GetInt32(5),
                    PdfeDahilEt = r.GetBoolean(6),
                    OlusturmaTarihi = r.GetDateTime(7)
                });
            }
            return list;
        }

        /// <summary>Dosyayi diske yazar, DB'ye kaydeder. Ek Id'sini doner.</summary>
        public async Task<int> SaveAttachmentAsync(int quoteId, string tip, Stream stream, string fileName)
        {
            // Klasoru olustur
            var dir = GetStorageDir(quoteId);
            Directory.CreateDirectory(dir);

            // Gecici Id ile kaydet (DB'ye ekledikten sonra yeniden adlandirma yapabiliriz)
            var tempPath = Path.Combine(dir, $"_tmp_{Guid.NewGuid():N}_{SanitizeFileName(fileName)}");
            using (var fs = File.Create(tempPath))
                await stream.CopyToAsync(fs);

            // DB'ye kaydet
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            // Mevcut sira no
            int siraNo;
            using (var cntCmd = new NpgsqlCommand(
                "SELECT COALESCE(MAX(\"SiraNo\")+1,0) FROM \"YLTeklifEkleri\" WHERE \"TeklifId\"=@tid AND \"Tip\"=@tip", conn))
            {
                cntCmd.Parameters.AddWithValue("tid", quoteId);
                cntCmd.Parameters.AddWithValue("tip", tip);
                siraNo = (int)(await cntCmd.ExecuteScalarAsync() ?? 0);
            }

            const string ins = """
                INSERT INTO "YLTeklifEkleri" ("TeklifId","DosyaAdi","Tip","DosyaYolu","SiraNo","PdfeDahilEt")
                VALUES (@tid,@ad,@tip,@yol,@sira,TRUE)
                RETURNING "Id";
                """;
            using var cmd = new NpgsqlCommand(ins, conn);
            cmd.Parameters.AddWithValue("tid", quoteId);
            cmd.Parameters.AddWithValue("ad", fileName);
            cmd.Parameters.AddWithValue("tip", tip);
            cmd.Parameters.AddWithValue("yol", tempPath);
            cmd.Parameters.AddWithValue("sira", siraNo);
            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);

            // Dosyayi Id ile yeniden adlandir
            var finalPath = Path.Combine(dir, $"{newId}_{SanitizeFileName(fileName)}");
            if (File.Exists(tempPath))
            {
                File.Move(tempPath, finalPath, overwrite: true);
                // DB'deki yolu guncelle
                using var upd = new NpgsqlCommand(
                    "UPDATE \"YLTeklifEkleri\" SET \"DosyaYolu\"=@yol WHERE \"Id\"=@id", conn);
                upd.Parameters.AddWithValue("yol", finalPath);
                upd.Parameters.AddWithValue("id", newId);
                await upd.ExecuteNonQueryAsync();
            }

            return newId;
        }

        public async Task UpdatePdfeDahilAsync(int id, bool dahil)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE \"YLTeklifEkleri\" SET \"PdfeDahilEt\"=@d WHERE \"Id\"=@id", conn);
            cmd.Parameters.AddWithValue("d", dahil);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAttachmentAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            // Dosya yolunu bul
            string? path = null;
            using (var sel = new NpgsqlCommand(
                "SELECT \"DosyaYolu\" FROM \"YLTeklifEkleri\" WHERE \"Id\"=@id", conn))
            {
                sel.Parameters.AddWithValue("id", id);
                var result = await sel.ExecuteScalarAsync();
                path = result as string;
            }

            // DB'den sil
            using var del = new NpgsqlCommand(
                "DELETE FROM \"YLTeklifEkleri\" WHERE \"Id\"=@id", conn);
            del.Parameters.AddWithValue("id", id);
            await del.ExecuteNonQueryAsync();

            // Fiziksel dosyayi sil
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try { File.Delete(path); } catch { /* sessiz kal */ }
            }
        }

        public async Task<byte[]> GetAttachmentBytesAsync(int id)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT \"DosyaYolu\" FROM \"YLTeklifEkleri\" WHERE \"Id\"=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            var path = (string?)await cmd.ExecuteScalarAsync();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return [];
            return await File.ReadAllBytesAsync(path);
        }

        /// <summary>
        /// Birden fazla teklif icin "hazir_teklif" durumunu tek SQL sorgusunda ceker.
        /// Donus: quoteId → attachment bilgisi (yoksa null).
        /// </summary>
        public async Task<Dictionary<int, QuoteAttachmentModel?>> GetHazirTeklifBulkAsync(IEnumerable<int> quoteIds)
        {
            var result = new Dictionary<int, QuoteAttachmentModel?>();
            var idList = quoteIds.ToList();
            if (!idList.Any()) return result;
            foreach (var id in idList) result[id] = null;

            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            var idParams = string.Join(",", idList.Select((_, i) => $"@id{i}"));
            var sql = $"""
                SELECT "Id","TeklifId","DosyaAdi","DosyaYolu"
                FROM "YLTeklifEkleri"
                WHERE "TeklifId" IN ({idParams}) AND "Tip" = 'hazir_teklif'
                ORDER BY "OlusturmaTarihi" DESC;
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            for (int i = 0; i < idList.Count; i++)
                cmd.Parameters.AddWithValue($"id{i}", idList[i]);

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var tid = r.GetInt32(1);
                if (result.TryGetValue(tid, out var existing) && existing == null)
                {
                    result[tid] = new QuoteAttachmentModel
                    {
                        Id        = r.GetInt32(0),
                        TeklifId  = tid,
                        DosyaAdi  = r.IsDBNull(2) ? "" : r.GetString(2),
                        DosyaYolu = r.IsDBNull(3) ? "" : r.GetString(3),
                        Tip       = "hazir_teklif"
                    };
                }
            }
            return result;
        }

        /// <summary>
        /// Bir teklife ait tum "hazir_teklif" eklerini (DB + disk) siler.
        /// Yeni upload oncesinde cagirilir.
        /// </summary>
        public async Task DeleteHazirTeklifAsync(int quoteId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            // Dosya yollarini bul
            var paths = new List<string>();
            using (var sel = new NpgsqlCommand(
                "SELECT \"DosyaYolu\" FROM \"YLTeklifEkleri\" WHERE \"TeklifId\"=@tid AND \"Tip\"='hazir_teklif'", conn))
            {
                sel.Parameters.AddWithValue("tid", quoteId);
                using var r = await sel.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var p = r.IsDBNull(0) ? null : r.GetString(0);
                    if (!string.IsNullOrEmpty(p)) paths.Add(p);
                }
            }

            // DB kayitlarini sil
            using var del = new NpgsqlCommand(
                "DELETE FROM \"YLTeklifEkleri\" WHERE \"TeklifId\"=@tid AND \"Tip\"='hazir_teklif'", conn);
            del.Parameters.AddWithValue("tid", quoteId);
            await del.ExecuteNonQueryAsync();

            // Fiziksel dosyalari sil
            foreach (var p in paths)
                if (File.Exists(p)) try { File.Delete(p); } catch { /* sessiz kal */ }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
        }
    }
}
