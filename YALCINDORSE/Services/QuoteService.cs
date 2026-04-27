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
        public string? TeslimatNotlari { get; set; }   // Cok satirli teslimat sartlari/notlari
        public string? SiparisNo { get; set; }

        // Ödeme planı (yeni)
        public decimal OnOdemeYuzdesi { get; set; }    // %
        public decimal OnOdemeTutari  { get; set; }    // hesaplanmis
        public decimal BakiyeTutari   { get; set; }    // Net - OnOdemeTutari
        public int? VadeGun           { get; set; }    // Bakiye odeme vadesi (gun)
        // Ikinci el alanları
        public string? SasiNo { get; set; }
        public int? ModelYili { get; set; }

        // Urun detay alanlari
        public string? TipAdi        { get; set; }
        public string? Lastik        { get; set; }
        public string? Suspansiyon   { get; set; }
        public string? Extension     { get; set; }
        public string? Gooseneck     { get; set; }
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

    public class QuoteRevisionModel
    {
        public int Id { get; set; }
        public int TeklifId { get; set; }
        public int RevizyonNo { get; set; }
        public string DegisiklikDetayi { get; set; } = "";
        public DateTime Tarih { get; set; }
        public string Yapan { get; set; } = "";
    }

    public class QuoteService
    {
        private readonly DatabaseHelper _db;
        private readonly AuthService _auth;
        private readonly SemaphoreSlim _schemaLock = new(1, 1);
        private static bool _schemaEnsured;   // static: Transient servis olsa da bir kez calisir

        public QuoteService(DatabaseHelper db, AuthService auth)
        {
            _db = db;
            _auth = auth;
        }

        /// <summary>YLTeklifler tablosuna eksik kolonlari ekler (her kolonu ayri komutla).</summary>
        private async Task EnsureSchemaAsync(NpgsqlConnection conn)
        {
            if (_schemaEnsured) return;
            await _schemaLock.WaitAsync();
            try
            {
                if (_schemaEnsured) return;
                _schemaEnsured = true;

                var migrations = new[]
                {
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "TipAdi"      TEXT""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Lastik"      TEXT""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Suspansiyon" TEXT""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Extension"   TEXT""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "Gooseneck"   TEXT""",
                    // Step 3 yeni alanlari — odeme plani + teslimat notlari
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "TeslimatNotlari" TEXT""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "OnOdemeYuzdesi"  NUMERIC(10,4) NOT NULL DEFAULT 0""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "OnOdemeTutari"   NUMERIC(18,4) NOT NULL DEFAULT 0""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "BakiyeTutari"    NUMERIC(18,4) NOT NULL DEFAULT 0""",
                    """ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "VadeGun"         INTEGER""",
                };

                foreach (var sql in migrations)
                {
                    try
                    {
                        using var cmd = new NpgsqlCommand(sql, conn);
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch { /* kolon zaten varsa atla */ }
                }
            }
            finally { _schemaLock.Release(); }
        }

        public async Task<List<QuoteListItemModel>> GetQuotesAsync(string search = "", string durum = "", int? saticiId = null, string puan = "", IReadOnlyList<string>? durumList = null)
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
            if (durumList != null && durumList.Count > 0)
                sql += " AND q.\"Durum\" = ANY(@durumList)";
            else if (!string.IsNullOrWhiteSpace(durum))
                sql += " AND q.\"Durum\" = @durum";
            if (saticiId.HasValue && saticiId > 0)
                sql += " AND q.\"SaticiId\" = @saticiId";
            if (!string.IsNullOrWhiteSpace(puan))
                sql += " AND q.\"Puan\" = @puan";

            sql += " ORDER BY q.\"OlusturmaTarihi\" DESC;";

            using var cmd = new NpgsqlCommand(sql, conn);

            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("search", $"%{search}%");
            if (durumList != null && durumList.Count > 0)
                cmd.Parameters.AddWithValue("durumList", durumList.ToArray());
            else if (!string.IsNullOrWhiteSpace(durum))
                cmd.Parameters.AddWithValue("durum", durum);
            if (saticiId.HasValue && saticiId > 0)
                cmd.Parameters.AddWithValue("saticiId", saticiId.Value);
            if (!string.IsNullOrWhiteSpace(puan))
                cmd.Parameters.AddWithValue("puan", puan);

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
                SELECT "Id", "TeklifId", "BaslikMi", "Aciklama", "SiraNo",
                       "UstKalemId", "UrunKodu", "Miktar", "Birim", "BirimFiyat", "Tutar", "OpsiyonMu", "KalemTipi"
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
                    SiraNo = reader.GetInt32(4),
                    UstKalemId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    UrunKodu = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Miktar = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    Birim = reader.IsDBNull(8) ? null : reader.GetString(8),
                    BirimFiyat = reader.IsDBNull(9) ? null : reader.GetDecimal(9),
                    Tutar = reader.IsDBNull(10) ? null : reader.GetDecimal(10),
                    OpsiyonMu = !reader.IsDBNull(11) && reader.GetBoolean(11),
                    KalemTipi = reader.IsDBNull(12) ? "ITEM" : reader.GetString(12)
                });
            }
            return items;
        }

        public async Task<int> CreateDraftQuoteAsync(QuoteModel quote)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                INSERT INTO "YLTeklifler"
                (
                    "TeklifNo", "MusteriId", "IlgiliKisiId", "SatisTipi",
                    "Kaynak", "Dil", "ParaBirimi", "Durum", "Puan",
                    "TalepTarihi", "GecerlilikTarihi", "SaticiId", "Notlar",
                    "ToplamTutar", "IndirimYuzde", "IndirimTutar", "NetTutar",
                    "RevizyonNo", "OnayGerektirir", "Olusturan",
                    "TeklifKanali", "TeklifTipi", "AksSayisi", "OdemeSistemi",
                    "IskontoAciklama", "KdvDahilMi", "IhracatMi", "IhracKayitliMi",
                    "TeslimatHaftasi", "TeslimatTipiKodu", "TeslimatYeri", "TeslimatNotlari",
                    "OnOdemeYuzdesi", "OnOdemeTutari", "BakiyeTutari", "VadeGun",
                    "SasiNo", "ModelYili",
                    "TipAdi", "Lastik", "Suspansiyon", "Extension", "Gooseneck"
                )
                VALUES
                (
                    @TeklifNo, @MusteriId, @IlgiliKisiId, @SatisTipi,
                    @Kaynak, @Dil, @ParaBirimi, @Durum, @Puan,
                    @TalepTarihi, @GecerlilikTarihi, @SaticiId, @Notlar,
                    @ToplamTutar, @IndirimYuzde, @IndirimTutar, @NetTutar,
                    0, false, @Olusturan,
                    @TeklifKanali, @TeklifTipi, @AksSayisi, @OdemeSistemi,
                    @IskontoAciklama, @KdvDahilMi, @IhracatMi, @IhracKayitliMi,
                    @TeslimatHaftasi, @TeslimatTipiKodu, @TeslimatYeri, @TeslimatNotlari,
                    @OnOdemeYuzdesi, @OnOdemeTutari, @BakiyeTutari, @VadeGun,
                    @SasiNo, @ModelYili,
                    @TipAdi, @Lastik, @Suspansiyon, @Extension, @Gooseneck
                )
                RETURNING "Id";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("TeklifNo", "");
            cmd.Parameters.AddWithValue("MusteriId", quote.MusteriId);
            cmd.Parameters.AddWithValue("IlgiliKisiId", (object?)quote.IlgiliKisiId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SatisTipi", quote.SatisTipi);
            cmd.Parameters.AddWithValue("Kaynak", quote.Kaynak);
            cmd.Parameters.AddWithValue("Dil", quote.Dil);
            cmd.Parameters.AddWithValue("ParaBirimi", quote.ParaBirimi);
            cmd.Parameters.AddWithValue("Durum", quote.Durum);
            cmd.Parameters.AddWithValue("Puan", quote.Puan);
            cmd.Parameters.AddWithValue("TalepTarihi", quote.TalepTarihi);
            cmd.Parameters.AddWithValue("GecerlilikTarihi", quote.GecerlilikTarihi);
            cmd.Parameters.AddWithValue("SaticiId", (object?)quote.SaticiId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Notlar", (object?)quote.Notlar ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ToplamTutar", quote.ToplamTutar);
            cmd.Parameters.AddWithValue("IndirimYuzde", quote.IndirimYuzde);
            cmd.Parameters.AddWithValue("IndirimTutar", quote.IndirimTutar);
            cmd.Parameters.AddWithValue("NetTutar", quote.NetTutar);
            cmd.Parameters.AddWithValue("Olusturan", _auth.CurrentUser);
            cmd.Parameters.AddWithValue("TeklifKanali", (object?)quote.TeklifKanali ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeklifTipi", (object?)quote.TeklifTipi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("AksSayisi", (object?)quote.AksSayisi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("OdemeSistemi", (object?)quote.OdemeSistemi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("IskontoAciklama", (object?)quote.IskontoAciklama ?? DBNull.Value);
            cmd.Parameters.AddWithValue("KdvDahilMi", quote.KdvDahilMi);
            cmd.Parameters.AddWithValue("IhracatMi", quote.IhracatMi);
            cmd.Parameters.AddWithValue("IhracKayitliMi", quote.IhracKayitliMi);
            cmd.Parameters.AddWithValue("TeslimatHaftasi", (object?)quote.TeslimatHaftasi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeslimatTipiKodu", (object?)quote.TeslimatTipiKodu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeslimatYeri", (object?)quote.TeslimatYeri ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeslimatNotlari", (object?)quote.TeslimatNotlari ?? DBNull.Value);
            cmd.Parameters.AddWithValue("OnOdemeYuzdesi", quote.OnOdemeYuzdesi);
            cmd.Parameters.AddWithValue("OnOdemeTutari", quote.OnOdemeTutari);
            cmd.Parameters.AddWithValue("BakiyeTutari", quote.BakiyeTutari);
            cmd.Parameters.AddWithValue("VadeGun", (object?)quote.VadeGun ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SasiNo", (object?)quote.SasiNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ModelYili", (object?)quote.ModelYili ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TipAdi", (object?)quote.TipAdi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Lastik", (object?)quote.Lastik ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Suspansiyon", (object?)quote.Suspansiyon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Extension", (object?)quote.Extension ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Gooseneck", (object?)quote.Gooseneck ?? DBNull.Value);

            var idResult = await cmd.ExecuteScalarAsync();
            var newId = Convert.ToInt32(idResult);

            // Rev 0 kaydi olustur
            await CreateRevisionEntryAsync(newId, 0, "Ilk teklif olusturuldu");

            return newId;
        }

        public async Task SaveQuoteItemAsync(QuoteItemModel item)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                INSERT INTO "YLTeklifKalemleri"
                ("TeklifId", "BaslikMi", "Aciklama", "SiraNo", "UstKalemId", "UrunKodu",
                 "Miktar", "Birim", "BirimFiyat", "Tutar", "OpsiyonMu", "KalemTipi")
                VALUES
                (@TeklifId, @BaslikMi, @Aciklama, @SiraNo, @UstKalemId, @UrunKodu,
                 @Miktar, @Birim, @BirimFiyat, @Tutar, @OpsiyonMu, @KalemTipi)
                RETURNING "Id";
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("TeklifId", item.TeklifId);
            cmd.Parameters.AddWithValue("BaslikMi", item.BaslikMi);
            cmd.Parameters.AddWithValue("Aciklama", item.Aciklama);
            cmd.Parameters.AddWithValue("SiraNo", item.SiraNo);
            cmd.Parameters.AddWithValue("UstKalemId", (object?)item.UstKalemId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("UrunKodu", (object?)item.UrunKodu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Miktar", (object?)item.Miktar ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Birim", (object?)item.Birim ?? DBNull.Value);
            cmd.Parameters.AddWithValue("BirimFiyat", (object?)item.BirimFiyat ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Tutar", (object?)item.Tutar ?? DBNull.Value);
            cmd.Parameters.AddWithValue("OpsiyonMu", item.OpsiyonMu);
            cmd.Parameters.AddWithValue("KalemTipi", item.KalemTipi);

            var idResult = await cmd.ExecuteScalarAsync();
            item.Id = Convert.ToInt32(idResult);
        }

        public async Task UpdateQuoteAsync(QuoteModel quote)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                UPDATE "YLTeklifler" SET
                    "MusteriId" = @MusteriId, "IlgiliKisiId" = @IlgiliKisiId, "SatisTipi" = @SatisTipi,
                    "Kaynak" = @Kaynak, "Dil" = @Dil, "ParaBirimi" = @ParaBirimi, "Durum" = @Durum, "Puan" = @Puan,
                    "TalepTarihi" = @TalepTarihi, "GecerlilikTarihi" = @GecerlilikTarihi,
                    "SaticiId" = @SaticiId, "Notlar" = @Notlar,
                    "ToplamTutar" = @ToplamTutar, "IndirimYuzde" = @IndirimYuzde,
                    "IndirimTutar" = @IndirimTutar, "NetTutar" = @NetTutar,
                    "TeklifKanali" = @TeklifKanali, "TeklifTipi" = @TeklifTipi, "AksSayisi" = @AksSayisi,
                    "OdemeSistemi" = @OdemeSistemi, "IskontoAciklama" = @IskontoAciklama,
                    "KdvDahilMi" = @KdvDahilMi, "IhracatMi" = @IhracatMi, "IhracKayitliMi" = @IhracKayitliMi,
                    "TeslimatHaftasi" = @TeslimatHaftasi, "TeslimatTipiKodu" = @TeslimatTipiKodu, "TeslimatYeri" = @TeslimatYeri,
                    "TeslimatNotlari" = @TeslimatNotlari,
                    "OnOdemeYuzdesi" = @OnOdemeYuzdesi, "OnOdemeTutari" = @OnOdemeTutari,
                    "BakiyeTutari" = @BakiyeTutari, "VadeGun" = @VadeGun,
                    "SasiNo" = @SasiNo, "ModelYili" = @ModelYili,
                    "TipAdi" = @TipAdi, "Lastik" = @Lastik, "Suspansiyon" = @Suspansiyon, "Extension" = @Extension, "Gooseneck" = @Gooseneck
                WHERE "Id" = @Id;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("Id", quote.Id);
            cmd.Parameters.AddWithValue("MusteriId", quote.MusteriId);
            cmd.Parameters.AddWithValue("IlgiliKisiId", (object?)quote.IlgiliKisiId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SatisTipi", quote.SatisTipi);
            cmd.Parameters.AddWithValue("Kaynak", quote.Kaynak);
            cmd.Parameters.AddWithValue("Dil", quote.Dil);
            cmd.Parameters.AddWithValue("ParaBirimi", quote.ParaBirimi);
            cmd.Parameters.AddWithValue("Durum", quote.Durum);
            cmd.Parameters.AddWithValue("Puan", quote.Puan);
            cmd.Parameters.AddWithValue("TalepTarihi", quote.TalepTarihi);
            cmd.Parameters.AddWithValue("GecerlilikTarihi", quote.GecerlilikTarihi);
            cmd.Parameters.AddWithValue("SaticiId", (object?)quote.SaticiId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Notlar", (object?)quote.Notlar ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ToplamTutar", quote.ToplamTutar);
            cmd.Parameters.AddWithValue("IndirimYuzde", quote.IndirimYuzde);
            cmd.Parameters.AddWithValue("IndirimTutar", quote.IndirimTutar);
            cmd.Parameters.AddWithValue("NetTutar", quote.NetTutar);
            cmd.Parameters.AddWithValue("TeklifKanali", (object?)quote.TeklifKanali ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeklifTipi", (object?)quote.TeklifTipi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("AksSayisi", (object?)quote.AksSayisi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("OdemeSistemi", (object?)quote.OdemeSistemi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("IskontoAciklama", (object?)quote.IskontoAciklama ?? DBNull.Value);
            cmd.Parameters.AddWithValue("KdvDahilMi", quote.KdvDahilMi);
            cmd.Parameters.AddWithValue("IhracatMi", quote.IhracatMi);
            cmd.Parameters.AddWithValue("IhracKayitliMi", quote.IhracKayitliMi);
            cmd.Parameters.AddWithValue("TeslimatHaftasi", (object?)quote.TeslimatHaftasi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeslimatTipiKodu", (object?)quote.TeslimatTipiKodu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeslimatYeri", (object?)quote.TeslimatYeri ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TeslimatNotlari", (object?)quote.TeslimatNotlari ?? DBNull.Value);
            cmd.Parameters.AddWithValue("OnOdemeYuzdesi", quote.OnOdemeYuzdesi);
            cmd.Parameters.AddWithValue("OnOdemeTutari", quote.OnOdemeTutari);
            cmd.Parameters.AddWithValue("BakiyeTutari", quote.BakiyeTutari);
            cmd.Parameters.AddWithValue("VadeGun", (object?)quote.VadeGun ?? DBNull.Value);
            cmd.Parameters.AddWithValue("SasiNo", (object?)quote.SasiNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ModelYili", (object?)quote.ModelYili ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TipAdi", (object?)quote.TipAdi ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Lastik", (object?)quote.Lastik ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Suspansiyon", (object?)quote.Suspansiyon ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Extension", (object?)quote.Extension ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Gooseneck", (object?)quote.Gooseneck ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteQuoteItemsByQuoteIdAsync(int quoteId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            const string sql = """DELETE FROM "YLTeklifKalemleri" WHERE "TeklifId" = @id;""";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", quoteId);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Bir teklifi tüm ilişkili kayıtlarıyla birlikte siler
        /// (kalemleri, revizyon logu, özellikler, ekler, ana kayıt).
        /// </summary>
        public async Task DeleteQuoteAsync(int quoteId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var tables = new[]
                {
                    """DELETE FROM "YLTeklifKalemleri"    WHERE "TeklifId" = @id""",
                    """DELETE FROM "YLTeklifRevLog"       WHERE "TeklifId" = @id""",
                    """DELETE FROM "YLTeklifOzellikleri"  WHERE "TeklifId" = @id""",
                    """DELETE FROM "YLTeklifEkleri"       WHERE "TeklifId" = @id""",
                    """DELETE FROM "YLTeklifler"          WHERE "Id"       = @id""",
                };
                foreach (var sql in tables)
                {
                    using var cmd = new NpgsqlCommand(sql, conn, tx);
                    cmd.Parameters.AddWithValue("id", quoteId);
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<QuoteModel?> GetQuoteByIdAsync(int quoteId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            await EnsureSchemaAsync(conn);

            const string sql = """
                SELECT "Id", "TeklifNo", "MusteriId", "IlgiliKisiId", "SatisTipi", "Kaynak",
                       "Dil", "ParaBirimi", "Durum", "Puan", "TalepTarihi", "GecerlilikTarihi",
                       "SaticiId", "Notlar", "ToplamTutar", "IndirimYuzde", "IndirimTutar", "NetTutar",
                       "RevizyonNo", "OnayGerektirir", "OlusturmaTarihi", "Olusturan",
                       "TeklifKanali", "TeklifTipi", "AksSayisi", "OdemeSistemi", "IskontoAciklama",
                       "KdvDahilMi", "IhracatMi", "IhracKayitliMi", "TeslimatHaftasi",
                       "TeslimatTipiKodu", "TeslimatYeri", "SiparisNo",
                       "SasiNo", "ModelYili",
                       "TipAdi", "Lastik", "Suspansiyon", "Extension", "Gooseneck",
                       "TeslimatNotlari", "OnOdemeYuzdesi", "OnOdemeTutari", "BakiyeTutari", "VadeGun"
                FROM "YLTeklifler"
                WHERE "Id" = @id;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", quoteId);

            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return new QuoteModel
            {
                Id = r.GetInt32(0),
                TeklifNo = r.GetString(1),
                MusteriId = r.GetInt32(2),
                IlgiliKisiId = r.IsDBNull(3) ? null : r.GetInt32(3),
                SatisTipi = r.IsDBNull(4) ? "" : r.GetString(4),
                Kaynak = r.IsDBNull(5) ? "" : r.GetString(5),
                Dil = r.IsDBNull(6) ? "" : r.GetString(6),
                ParaBirimi = r.IsDBNull(7) ? "" : r.GetString(7),
                Durum = r.IsDBNull(8) ? "" : r.GetString(8),
                Puan = r.IsDBNull(9) ? "" : r.GetString(9),
                TalepTarihi = r.GetDateTime(10),
                GecerlilikTarihi = r.GetDateTime(11),
                SaticiId = r.IsDBNull(12) ? null : r.GetInt32(12),
                Notlar = r.IsDBNull(13) ? null : r.GetString(13),
                ToplamTutar = r.IsDBNull(14) ? 0 : r.GetDecimal(14),
                IndirimYuzde = r.IsDBNull(15) ? 0 : r.GetDecimal(15),
                IndirimTutar = r.IsDBNull(16) ? 0 : r.GetDecimal(16),
                NetTutar = r.IsDBNull(17) ? 0 : r.GetDecimal(17),
                RevizyonNo = r.IsDBNull(18) ? 0 : r.GetInt32(18),
                OnayGerektirir = !r.IsDBNull(19) && r.GetBoolean(19),
                OlusturmaTarihi = r.IsDBNull(20) ? DateTime.Now : r.GetDateTime(20),
                Olusturan = r.IsDBNull(21) ? "" : r.GetString(21),
                TeklifKanali = r.IsDBNull(22) ? null : r.GetString(22),
                TeklifTipi = r.IsDBNull(23) ? null : r.GetString(23),
                AksSayisi = r.IsDBNull(24) ? null : r.GetInt32(24),
                OdemeSistemi = r.IsDBNull(25) ? null : r.GetString(25),
                IskontoAciklama = r.IsDBNull(26) ? null : r.GetString(26),
                KdvDahilMi = !r.IsDBNull(27) && r.GetBoolean(27),
                IhracatMi = !r.IsDBNull(28) && r.GetBoolean(28),
                IhracKayitliMi = !r.IsDBNull(29) && r.GetBoolean(29),
                TeslimatHaftasi = r.IsDBNull(30) ? null : r.GetString(30),
                TeslimatTipiKodu = r.IsDBNull(31) ? null : r.GetString(31),
                TeslimatYeri = r.IsDBNull(32) ? null : r.GetString(32),
                SiparisNo = r.IsDBNull(33) ? null : r.GetString(33),
                SasiNo = r.IsDBNull(34) ? null : r.GetString(34),
                ModelYili = r.IsDBNull(35) ? null : r.GetInt32(35),
                TipAdi      = r.IsDBNull(36) ? null : r.GetString(36),
                Lastik      = r.IsDBNull(37) ? null : r.GetString(37),
                Suspansiyon = r.IsDBNull(38) ? null : r.GetString(38),
                Extension   = r.IsDBNull(39) ? null : r.GetString(39),
                Gooseneck   = r.IsDBNull(40) ? null : r.GetString(40),
                TeslimatNotlari = r.IsDBNull(41) ? null : r.GetString(41),
                OnOdemeYuzdesi  = r.IsDBNull(42) ? 0 : r.GetDecimal(42),
                OnOdemeTutari   = r.IsDBNull(43) ? 0 : r.GetDecimal(43),
                BakiyeTutari    = r.IsDBNull(44) ? 0 : r.GetDecimal(44),
                VadeGun         = r.IsDBNull(45) ? null : r.GetInt32(45),
            };
        }

        public async Task<int> CopyQuoteAsync(int sourceQuoteId)
        {
            var source = await GetQuoteByIdAsync(sourceQuoteId);
            if (source == null) throw new Exception("Kaynak teklif bulunamadi.");

            source.Id = 0;
            source.TeklifNo = "";
            source.Durum = "Draft";
            source.RevizyonNo = 0;
            source.SiparisNo = null;
            source.TalepTarihi = DateTime.Today;
            source.OlusturmaTarihi = DateTime.Now;

            var newId = await CreateDraftQuoteAsync(source);

            // Kalemleri kopyala
            var items = await GetQuoteItemsAsync(sourceQuoteId);
            var idMap = new Dictionary<int, int>(); // eski Id -> yeni Id

            foreach (var item in items)
            {
                var oldId = item.Id;
                item.Id = 0;
                item.TeklifId = newId;
                // UstKalemId'yi sonra guncelleyecegiz
                var origParent = item.UstKalemId;
                if (origParent.HasValue && idMap.ContainsKey(origParent.Value))
                    item.UstKalemId = idMap[origParent.Value];
                else
                    item.UstKalemId = null;

                await SaveQuoteItemAsync(item);
                idMap[oldId] = item.Id;
            }

            return newId;
        }

        public async Task<List<QuoteRevisionModel>> GetRevisionsAsync(int quoteId)
        {
            var items = new List<QuoteRevisionModel>();
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                SELECT r."Id", r."TeklifId", r."RevizyonNo", r."DegisiklikDetayi", r."Tarih",
                       COALESCE(u."FullName", r."Yapan") AS "Yapan"
                FROM "YLTeklifRevLog" r
                LEFT JOIN "YLUsers" u ON u."Username" = r."Yapan"
                WHERE r."TeklifId" = @quoteId
                ORDER BY r."RevizyonNo" ASC;
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("quoteId", quoteId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new QuoteRevisionModel
                {
                    Id = reader.GetInt32(0),
                    TeklifId = reader.GetInt32(1),
                    RevizyonNo = reader.GetInt32(2),
                    DegisiklikDetayi = reader.GetString(3),
                    Tarih = reader.GetDateTime(4),
                    Yapan = reader.IsDBNull(5) ? "" : reader.GetString(5)
                });
            }
            return items;
        }

        public async Task CreateRevisionEntryAsync(int quoteId, int revNo, string detail)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            const string sql = """
                INSERT INTO "YLTeklifRevLog" ("TeklifId", "RevizyonNo", "DegisiklikDetayi", "Tarih", "Yapan")
                VALUES (@quoteId, @revNo, @detail, NOW(), @yapan);
                """;
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("quoteId", quoteId);
            cmd.Parameters.AddWithValue("revNo", revNo);
            cmd.Parameters.AddWithValue("detail", detail);
            cmd.Parameters.AddWithValue("yapan", _auth.CurrentUser ?? "");
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateQuoteWithRevisionAsync(QuoteModel newQuote)
        {
            var oldQuote = await GetQuoteByIdAsync(newQuote.Id);
            if (oldQuote == null) throw new Exception("Teklif bulunamadi.");

            // Degisiklikleri tespit et
            var changes = new List<string>();
            CompareField(changes, "Musteri", oldQuote.MusteriId, newQuote.MusteriId);
            CompareField(changes, "Ilgili Kisi", oldQuote.IlgiliKisiId, newQuote.IlgiliKisiId);
            CompareField(changes, "Satis Tipi", oldQuote.SatisTipi, newQuote.SatisTipi);
            CompareField(changes, "Kaynak", oldQuote.Kaynak, newQuote.Kaynak);
            CompareField(changes, "Dil", oldQuote.Dil, newQuote.Dil);
            CompareField(changes, "Para Birimi", oldQuote.ParaBirimi, newQuote.ParaBirimi);
            CompareField(changes, "Puan", oldQuote.Puan, newQuote.Puan);
            CompareField(changes, "Talep Tarihi", oldQuote.TalepTarihi.ToString("dd.MM.yyyy"), newQuote.TalepTarihi.ToString("dd.MM.yyyy"));
            CompareField(changes, "Gecerlilik Tarihi", oldQuote.GecerlilikTarihi.ToString("dd.MM.yyyy"), newQuote.GecerlilikTarihi.ToString("dd.MM.yyyy"));
            CompareField(changes, "Satici", oldQuote.SaticiId, newQuote.SaticiId);
            CompareField(changes, "Notlar", oldQuote.Notlar ?? "", newQuote.Notlar ?? "");
            CompareField(changes, "Net Tutar", oldQuote.NetTutar, newQuote.NetTutar);
            CompareField(changes, "Toplam Tutar", oldQuote.ToplamTutar, newQuote.ToplamTutar);
            CompareField(changes, "Indirim %", oldQuote.IndirimYuzde, newQuote.IndirimYuzde);
            CompareField(changes, "Teklif Kanali", oldQuote.TeklifKanali ?? "", newQuote.TeklifKanali ?? "");
            CompareField(changes, "Teklif Tipi", oldQuote.TeklifTipi ?? "", newQuote.TeklifTipi ?? "");
            CompareField(changes, "Aks Sayisi", oldQuote.AksSayisi, newQuote.AksSayisi);
            CompareField(changes, "Odeme Sistemi", oldQuote.OdemeSistemi ?? "", newQuote.OdemeSistemi ?? "");
            CompareField(changes, "Iskonto Aciklama", oldQuote.IskontoAciklama ?? "", newQuote.IskontoAciklama ?? "");
            CompareField(changes, "KDV Dahil", oldQuote.KdvDahilMi, newQuote.KdvDahilMi);
            CompareField(changes, "Ihracat", oldQuote.IhracatMi, newQuote.IhracatMi);
            CompareField(changes, "Teslimat Haftasi", oldQuote.TeslimatHaftasi ?? "", newQuote.TeslimatHaftasi ?? "");
            CompareField(changes, "Teslimat Yeri", oldQuote.TeslimatYeri ?? "", newQuote.TeslimatYeri ?? "");

            if (changes.Count > 0)
            {
                // RevizyonNo artir
                newQuote.RevizyonNo = oldQuote.RevizyonNo + 1;

                // Guncelle (RevizyonNo ile birlikte)
                await UpdateQuoteAsync(newQuote);
                await IncrementRevisionNoAsync(newQuote.Id, newQuote.RevizyonNo);

                // Revizyon kaydi olustur
                var detail = string.Join("; ", changes);
                await CreateRevisionEntryAsync(newQuote.Id, newQuote.RevizyonNo, detail);
            }
            else
            {
                // Degisiklik yok, sadece guncelle
                await UpdateQuoteAsync(newQuote);
            }
        }

        private async Task IncrementRevisionNoAsync(int quoteId, int newRevNo)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            const string sql = """UPDATE "YLTeklifler" SET "RevizyonNo" = @rev WHERE "Id" = @id;""";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("rev", newRevNo);
            cmd.Parameters.AddWithValue("id", quoteId);
            await cmd.ExecuteNonQueryAsync();
        }

        private static void CompareField<T>(List<string> changes, string fieldName, T? oldVal, T? newVal)
        {
            var oldStr = oldVal?.ToString() ?? "";
            var newStr = newVal?.ToString() ?? "";
            if (oldStr != newStr)
            {
                if (string.IsNullOrEmpty(oldStr))
                    changes.Add($"{fieldName} eklendi: {newStr}");
                else if (string.IsNullOrEmpty(newStr))
                    changes.Add($"{fieldName} kaldirildi");
                else
                    changes.Add($"{fieldName}: {oldStr} -> {newStr}");
            }
        }

        public async Task<string> TransitionStatusAsync(int quoteId, string newStatus)
        {
            // Onceki durumu al
            var oldQuote = await GetQuoteByIdAsync(quoteId);
            var oldStatus = oldQuote?.Durum ?? "";
            var currentRev = oldQuote?.RevizyonNo ?? 0;

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string? siparisNo = null;
            if (newStatus == "ORDER")
            {
                const string seqSql = """SELECT nextval('"YLSiparisNoSeq"');""";
                using var seqCmd = new NpgsqlCommand(seqSql, conn);
                var seqVal = await seqCmd.ExecuteScalarAsync();
                siparisNo = $"ACF{Convert.ToInt64(seqVal):D4}";

                const string updateSql = """
                    UPDATE "YLTeklifler"
                    SET "Durum" = @durum, "SiparisNo" = @siparisNo, "RevizyonNo" = "RevizyonNo" + 1
                    WHERE "Id" = @id;
                    """;
                using var cmd = new NpgsqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("durum", newStatus);
                cmd.Parameters.AddWithValue("siparisNo", siparisNo);
                cmd.Parameters.AddWithValue("id", quoteId);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                const string updateSql = """
                    UPDATE "YLTeklifler"
                    SET "Durum" = @durum, "RevizyonNo" = "RevizyonNo" + 1
                    WHERE "Id" = @id;
                    """;
                using var cmd = new NpgsqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("durum", newStatus);
                cmd.Parameters.AddWithValue("id", quoteId);
                await cmd.ExecuteNonQueryAsync();
            }

            // Durum gecisi revizyon kaydi
            var detail = $"Durum degisti: {oldStatus} -> {newStatus}";
            if (siparisNo != null) detail += $" (Siparis No: {siparisNo})";
            await CreateRevisionEntryAsync(quoteId, currentRev + 1, detail);

            return siparisNo ?? "";
        }
    }
}
