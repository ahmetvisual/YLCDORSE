using Npgsql;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    public class QuoteListItemModel
    {
        public int Id { get; set; }
        public string TeklifNo { get; set; } = "";
        public string Musteri { get; set; } = "";
        public string? Ilgili { get; set; }
        public string? Satici { get; set; }
        public string Durum { get; set; } = "";
        public string Puan { get; set; } = "";
        public string Dil { get; set; } = "";
        public string Para { get; set; } = "";
        public string SatisTipi { get; set; } = "";
        public string Kaynak { get; set; } = "";
        public DateTime TalepTarihi { get; set; }
        public DateTime GecerlilikTarihi { get; set; }
        public decimal NetTutar { get; set; }
        public int Rev { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public string? Notlar { get; set; }
        public string? TeklifKanali { get; set; }
        public string? MusteriKodu { get; set; }
    }

    public class QuoteModel
    {
        public int Id { get; set; }
        public string TeklifNo { get; set; } = "";
        public int MusteriId { get; set; }
        public int? IlgiliKisiId { get; set; }
        public string SatisTipi { get; set; } = "NewProduction";
        public string Kaynak { get; set; } = "Email";
        public string Dil { get; set; } = "TR";
        public string ParaBirimi { get; set; } = "EUR";
        public string Durum { get; set; } = "Draft";
        public string Puan { get; set; } = "WARM";
        public DateTime TalepTarihi { get; set; } = DateTime.Today;
        public DateTime GecerlilikTarihi { get; set; } = DateTime.Today.AddDays(15);
        public int? SaticiId { get; set; }
        public string? Notlar { get; set; }
        public decimal ToplamTutar { get; set; }
        public decimal IndirimYuzde { get; set; }
        public decimal IndirimTutar { get; set; }
        public decimal NetTutar { get; set; }
        public int RevizyonNo { get; set; }
        public bool OnayGerektirir { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public string Olusturan { get; set; } = "";

        // Yeni alanlar - Touch CRM & Musteri notlari
        public string? TeklifKanali { get; set; }
        public string? TeklifTipi { get; set; }
        public int? AksSayisi { get; set; }
        public string? OdemeSistemi { get; set; }
        public string? IskontoAciklama { get; set; }
        public bool KdvDahilMi { get; set; }
        public bool IhracatMi { get; set; }
        public bool IhracKayitliMi { get; set; }
        public string? TeslimatHaftasi { get; set; }
        public string? TeslimatTipiKodu { get; set; }
        public string? TeslimatYeri { get; set; }
        public string? SiparisNo { get; set; }
    }

    public class QuoteItemModel
    {
        public int Id { get; set; }
        public int TeklifId { get; set; }
        public bool BaslikMi { get; set; }
        public string Aciklama { get; set; } = "";
        public int SiraNo { get; set; }

        // Yeni alanlar - Hiyerarsik kalem yapisi
        public int? UstKalemId { get; set; }
        public string? UrunKodu { get; set; }
        public decimal? Miktar { get; set; }
        public string? Birim { get; set; }
        public decimal? BirimFiyat { get; set; }
        public decimal? Tutar { get; set; }
        public bool OpsiyonMu { get; set; }
        public string KalemTipi { get; set; } = "ITEM"; // HEADER, ITEM, SUB_ITEM, OPTION
    }

    public class QuoteService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);
        private bool _schemaEnsured;

        public QuoteService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        public async Task<List<QuoteListItemModel>> GetQuotesAsync(string search = "", string durum = "", int? saticiId = null)
        {
            var items = new List<QuoteListItemModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string sql = """
                SELECT
                    q."Id",
                    q."TeklifNo",
                    c."Title"               AS "Musteri",
                    cc."ContactName"        AS "Ilgili",
                    u."FullName"            AS "Satici",
                    q."Durum",
                    q."Puan",
                    q."Dil",
                    q."ParaBirimi"          AS "Para",
                    q."SatisTipi",
                    q."Kaynak",
                    q."TalepTarihi",
                    q."GecerlilikTarihi",
                    q."NetTutar",
                    q."RevizyonNo"          AS "Rev",
                    q."OlusturmaTarihi",
                    q."Notlar"
                FROM "YLTeklifler" q
                LEFT JOIN "YLCustomers" c ON c."Id" = q."MusteriId"
                LEFT JOIN "YLCustomerContacts" cc ON cc."Id" = q."IlgiliKisiId"
                LEFT JOIN "YLUsers" u ON u."Id" = q."SaticiId"
                WHERE 1=1
                """;

            if (!string.IsNullOrWhiteSpace(search))
                sql += " AND (q.\"TeklifNo\" ILIKE @search OR c.\"Title\" ILIKE @search OR u.\"FullName\" ILIKE @search)";
            if (!string.IsNullOrWhiteSpace(durum))
                sql += " AND q.\"Durum\" = @durum";
            if (saticiId.HasValue && saticiId > 0)
                sql += " AND q.\"SaticiId\" = @saticiId";

            sql += " ORDER BY q.\"OlusturmaTarihi\" DESC;";

            using var cmd = new NpgsqlCommand(sql, conn);
            
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("search", $"%{search}%");
            if (!string.IsNullOrWhiteSpace(durum))
                cmd.Parameters.AddWithValue("durum", durum);
            if (saticiId.HasValue && saticiId > 0)
                cmd.Parameters.AddWithValue("saticiId", saticiId.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new QuoteListItemModel
                {
                    Id = reader.GetInt32(0),
                    TeklifNo = reader.GetString(1),
                    Musteri = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Ilgili = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Satici = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Durum = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    Puan = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    Dil = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    Para = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    SatisTipi = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Kaynak = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    TalepTarihi = reader.GetDateTime(11),
                    GecerlilikTarihi = reader.GetDateTime(12),
                    NetTutar = reader.IsDBNull(13) ? 0 : reader.GetDecimal(13),
                    Rev = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                    OlusturmaTarihi = reader.IsDBNull(15) ? DateTime.Now : reader.GetDateTime(15),
                    Notlar = reader.IsDBNull(16) ? null : reader.GetString(16)
                });
            }

            return items;
        }

        public async Task<List<QuoteItemModel>> GetQuoteItemsAsync(int quoteId)
        {
            var items = new List<QuoteItemModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT "Id", "TeklifId", "BaslikMi", "Aciklama", "SiraNo"
                FROM "YLTeklifKalemleri"
                WHERE "TeklifId" = @quoteId
                ORDER BY "SiraNo";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("quoteId", quoteId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new QuoteItemModel
                {
                    Id = reader.GetInt32(0),
                    TeklifId = reader.GetInt32(1),
                    BaslikMi = reader.GetBoolean(2),
                    Aciklama = reader.GetString(3),
                    SiraNo = reader.GetInt32(4)
                });
            }
            return items;
        }

        public async Task<int> CreateDraftQuoteAsync(QuoteModel quote)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                INSERT INTO "YLTeklifler"
                (
                    "TeklifNo", "MusteriId", "IlgiliKisiId", "SatisTipi",
                    "Kaynak", "Dil", "ParaBirimi", "Durum", "Puan",
                    "TalepTarihi", "GecerlilikTarihi", "SaticiId", "Notlar",
                    "ToplamTutar", "IndirimYuzde", "IndirimTutar", "NetTutar",
                    "RevizyonNo", "OnayGerektirir", "Olusturan"
                )
                VALUES
                (
                    @TeklifNo, @MusteriId, @IlgiliKisiId, @SatisTipi,
                    @Kaynak, @Dil, @ParaBirimi, 'Draft', @Puan,
                    @TalepTarihi, @GecerlilikTarihi, @SaticiId, @Notlar,
                    0, 0, 0, 0,
                    0, false, @Olusturan
                )
                RETURNING "Id";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("TeklifNo", ""); // Trigger set eder
            cmd.Parameters.AddWithValue("MusteriId", quote.MusteriId);
            cmd.Parameters.AddWithValue("IlgiliKisiId", (object?)quote.IlgiliKisiId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SatisTipi", quote.SatisTipi);
            cmd.Parameters.AddWithValue("Kaynak", quote.Kaynak);
            cmd.Parameters.AddWithValue("Dil", quote.Dil);
            cmd.Parameters.AddWithValue("ParaBirimi", quote.ParaBirimi);
            cmd.Parameters.AddWithValue("Puan", quote.Puan);
            cmd.Parameters.AddWithValue("TalepTarihi", quote.TalepTarihi);
            cmd.Parameters.AddWithValue("GecerlilikTarihi", quote.GecerlilikTarihi);
            cmd.Parameters.AddWithValue("SaticiId", (object?)quote.SaticiId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Notlar", (object?)quote.Notlar ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Olusturan", _auth.CurrentUser);

            var idResult = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idResult);
        }
    }
}
