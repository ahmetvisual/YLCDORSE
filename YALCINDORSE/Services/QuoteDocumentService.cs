using Npgsql;
using System.Diagnostics;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    /// <summary>
    /// Teklif belgesi uretim servisi.
    /// PDF uretimi icin DevExpress TeklifReport kullanilir.
    /// </summary>
    public class QuoteDocumentService
    {
        private readonly DatabaseHelper _db;
        private readonly QuoteService _quoteSvc;
        private readonly QuoteSpecService _specSvc;
        private readonly QuoteAttachmentService _attachSvc;
        private readonly CustomerService _customerSvc;
        private readonly FirmaService _firmaSvc;

        public QuoteDocumentService(
            DatabaseHelper db,
            QuoteService quoteSvc,
            QuoteSpecService specSvc,
            QuoteAttachmentService attachSvc,
            CustomerService customerSvc,
            FirmaService firmaSvc)
        {
            _db = db;
            _quoteSvc = quoteSvc;
            _specSvc = specSvc;
            _attachSvc = attachSvc;
            _customerSvc = customerSvc;
            _firmaSvc = firmaSvc;
        }

        // ─────────────────────────────────────────────
        //  Ana uretim metodlari
        // ─────────────────────────────────────────────

        /// <summary>DevExpress kaldırıldı — QuestPDF'e yönlendirir.</summary>
        public Task OpenInDefaultAppAsync(int quoteId) => OpenQuestPdfAsync(quoteId);

        /// <summary>QuestPDF ile PDF uretir ve varsayilan uygulamada acar.</summary>
        public async Task OpenQuestPdfAsync(int quoteId)
        {
            var pdfBytes = await GenerateQuestPdfBytesAsync(quoteId);

            var tempDir = Path.Combine(FileSystem.AppDataDirectory, "temp");
            Directory.CreateDirectory(tempDir);

            var quote = await _quoteSvc.GetQuoteByIdAsync(quoteId);
            var safeName = SanitizeFileName(quote?.TeklifNo ?? quoteId.ToString());
            var filePath = Path.Combine(tempDir, $"{safeName}_quest.pdf");

            await File.WriteAllBytesAsync(filePath, pdfBytes);
#if WINDOWS
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
#endif
        }

        /// <summary>QuestPDF rapor nesnesini doldurup PDF byte[] uretir.</summary>
        public async Task<byte[]> GenerateQuestPdfBytesAsync(int quoteId)
        {
            var quote = await _quoteSvc.GetQuoteByIdAsync(quoteId)
                ?? throw new Exception($"Teklif bulunamadi: {quoteId}");

            var items = await _quoteSvc.GetQuoteItemsAsync(quoteId);

            // Musteri / ilgili kisi / satici
            string musteriAdi = "", musteriKodu = "";
            string ilgiliKisi = "", ilgiliEmail = "", ilgiliMobil = "";
            string saticiAdi = "", saticiEmail = "", saticiTelefon = "";
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                const string sql = """
                    SELECT c."Title", c."CustomerCode",
                           cc."ContactName", cc."Email", cc."Mobile",
                           u."FullName", u."Email" as "SaticiEmail", u."Phone" as "SaticiTelefon"
                    FROM "YLTeklifler" q
                    LEFT JOIN "YLCustomers" c ON c."Id" = q."MusteriId"
                    LEFT JOIN "YLCustomerContacts" cc ON cc."Id" = q."IlgiliKisiId"
                    LEFT JOIN "YLUsers" u ON u."Id" = q."SaticiId"
                    WHERE q."Id" = @id;
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", quoteId);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    musteriAdi    = r.IsDBNull(0) ? "" : r.GetString(0);
                    musteriKodu   = r.IsDBNull(1) ? "" : r.GetString(1);
                    ilgiliKisi    = r.IsDBNull(2) ? "" : r.GetString(2);
                    ilgiliEmail   = r.IsDBNull(3) ? "" : r.GetString(3);
                    ilgiliMobil   = r.IsDBNull(4) ? "" : r.GetString(4);
                    saticiAdi     = r.IsDBNull(5) ? "" : r.GetString(5);
                    saticiEmail   = r.IsDBNull(6) ? "" : r.GetString(6);
                    saticiTelefon = r.IsDBNull(7) ? "" : r.GetString(7);
                }
            }
            catch { }

            // Ekler
            var attachments = new List<QuoteAttachmentModel>();
            try { attachments = await _attachSvc.GetAttachmentsAsync(quoteId); }
            catch { }

            // Ürün fotoğrafları
            byte[]? foto1 = null, foto2 = null;
            try
            {
                var fotograflar = attachments
                    .Where(a => a.Tip == "foto" && File.Exists(a.DosyaYolu))
                    .OrderBy(a => a.SiraNo).Take(2).ToList();
                if (fotograflar.Count >= 1) foto1 = await File.ReadAllBytesAsync(fotograflar[0].DosyaYolu);
                if (fotograflar.Count >= 2) foto2 = await File.ReadAllBytesAsync(fotograflar[1].DosyaYolu);
            }
            catch { }

            // Teknik resimler
            var cizimImages = new List<byte[]>();
            try
            {
                foreach (var c in attachments
                    .Where(a => a.Tip == "teknikresim" && File.Exists(a.DosyaYolu))
                    .OrderBy(a => a.SiraNo))
                    cizimImages.Add(await File.ReadAllBytesAsync(c.DosyaYolu));
            }
            catch { }

            // Ürün başlık
            var urunAdi    = items.FirstOrDefault(i => i.KalemTipi == "HEADER")?.Aciklama ?? "";
            var urunBaslik = string.IsNullOrWhiteSpace(urunAdi) ? ""
                : "YALÇIN DORSE  ·  " + urunAdi.ToUpperInvariant()
                  + (quote.SatisTipi == "SecondHand" ? "  ·  2. EL ÜRÜN" : "");
            var urunAltYazi = string.IsNullOrWhiteSpace(urunAdi) ? ""
                : $"(Fotoğraflar, {urunAdi.ToLowerInvariant()} ürününe aittir.)";

            // Firma bilgileri (singleton) + IBAN listesi
            FirmaBilgileriModel? firma = null;
            var ibanList = new List<string>();
            try
            {
                firma = await _firmaSvc.GetFirmaAsync();
                var hesaplar = await _firmaSvc.GetHesaplarAsync(onlyActive: true);
                ibanList = hesaplar.Select(h =>
                {
                    var parts = new[]
                    {
                        h.BankaAdi,
                        h.ParaBirimi,
                        FormatIban(h.IBAN)
                    }.Where(p => !string.IsNullOrWhiteSpace(p));
                    return string.Join("  —  ", parts);
                }).ToList();
            }
            catch { }

            // Logo: oncelik DB (FirmaBilgileri.LogoBytes), fallback disk dosyasi
            byte[]? logoBytes = firma?.LogoBytes;
            if (logoBytes == null || logoBytes.Length == 0)
            {
                try
                {
                    var logoPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo.png");
                    if (File.Exists(logoPath))
                        logoBytes = await File.ReadAllBytesAsync(logoPath);
                }
                catch { }
            }

            // Spec grupları
            var specGroups = new List<TeklifQuestPdfReport.SpecGroup>();
            var headers    = items.Where(i => i.KalemTipi == "HEADER").OrderBy(i => i.SiraNo).ToList();
            foreach (var header in headers)
            {
                var specRows = items
                    .Where(i => i.UstKalemId == header.Id && i.KalemTipi == "SPEC")
                    .OrderBy(i => i.SiraNo).ToList();
                if (specRows.Count > 0)
                    specGroups.Add(new TeklifQuestPdfReport.SpecGroup
                    {
                        GrupAdi = header.Aciklama,
                        Rows    = specRows.Select(r => (r.Aciklama, r.Birim ?? "")).ToList()
                    });
            }

            // Numaralı liste — SPEC-only header'lar (Tablo tipi: ÖLÇÜLER, AĞIRLIKLAR vb.)
            // SpecSection'da zaten gosterildigi icin bunlari listeden cikar.
            var listItems = new List<TeklifQuestPdfReport.ListItem>();
            int hNum = 0;
            foreach (var header in headers)
            {
                var children = items
                    .Where(i => i.UstKalemId == header.Id && i.KalemTipi == "ITEM")
                    .OrderBy(i => i.SiraNo).ToList();
                if (children.Count == 0) continue;  // SPEC-only header — listede yer alma

                hNum++;
                listItems.Add(new TeklifQuestPdfReport.ListItem($"{hNum}.", header.Aciklama, true, true));
                int cNum = 0;
                foreach (var child in children)
                {
                    cNum++;
                    listItems.Add(new TeklifQuestPdfReport.ListItem(
                        $"{hNum}.{cNum}.", child.Aciklama, child.BaslikMi, false));
                }
            }

            // Raporu doldur
            var report = new TeklifQuestPdfReport
            {
                TeklifNo         = quote.TeklifNo,
                Tarih            = quote.TalepTarihi.ToString("dd.MM.yyyy"),
                GecerlilikTarihi = quote.GecerlilikTarihi.ToString("dd.MM.yyyy"),
                MusteriAdi       = musteriAdi,
                MusteriKodu      = musteriKodu,
                IlgiliKisi       = ilgiliKisi,
                IlgiliEmail      = ilgiliEmail,
                IlgiliMobil      = ilgiliMobil,
                SaticiAdi        = saticiAdi,
                SaticiEmail      = saticiEmail,
                SaticiTelefon    = saticiTelefon,
                NetTutar         = quote.NetTutar.ToString("N2"),
                ParaBirimi       = quote.ParaBirimi,
                UrunBaslik       = urunBaslik,
                UrunAltYazi      = urunAltYazi,
                SasiNo           = quote.SasiNo ?? "",
                ModelYili        = quote.ModelYili?.ToString() ?? "",
                LogoBytes        = logoBytes,
                UrunFoto1        = foto1,
                UrunFoto2        = foto2,
                SpecGroups       = specGroups,
                CizimImages      = cizimImages,
                ListItems        = listItems,
                // Firma + IBAN
                FirmaUnvan       = firma?.TamUnvan ?? "",
                FirmaAdresTam    = BuildAdresTam(firma),
                FirmaTelefon     = firma?.Telefon ?? "",
                FirmaEmail       = firma?.Email ?? "",
                FirmaWeb         = firma?.Web ?? "",
                FirmaVergiNo     = firma?.VergiNo ?? "",
                FirmaKapakFoto   = firma?.KapakFotoBytes,
                IBANListesi      = ibanList,
            };

            return report.GeneratePdf();
        }

        /// <summary>Firma adres satirlarini tek bir string'e birlestirir.</summary>
        private static string BuildAdresTam(FirmaBilgileriModel? f)
        {
            if (f == null) return "";
            var parts = new[]
            {
                f.AdresSatir1, f.AdresSatir2,
                string.Join(" ", new[] { f.PostaKodu, f.Sehir }.Where(s => !string.IsNullOrWhiteSpace(s))),
                f.Ulke
            }.Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(", ", parts);
        }

        /// <summary>IBAN'i okunabilir 4'erli gruplar halinde formatla: "TR44 0006 2000 ..."</summary>
        private static string FormatIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return "";
            var raw = iban.Replace(" ", "").ToUpperInvariant();
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append(' ');
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        // ─────────────────────────────────────────────
        //  Yardimci metotlar
        // ─────────────────────────────────────────────

        private static string SanitizeFileName(string name) =>
            string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    }
}
