using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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

        public QuoteDocumentService(
            DatabaseHelper db,
            QuoteService quoteSvc,
            QuoteSpecService specSvc,
            QuoteAttachmentService attachSvc,
            CustomerService customerSvc)
        {
            _db = db;
            _quoteSvc = quoteSvc;
            _specSvc = specSvc;
            _attachSvc = attachSvc;
            _customerSvc = customerSvc;
        }

        // ─────────────────────────────────────────────
        //  Ana uretim metodlari
        // ─────────────────────────────────────────────

        /// <summary>DevExpress XtraReport ile PDF uretir ve varsayilan uygulamada acar.</summary>
        public async Task OpenInDefaultAppAsync(int quoteId)
        {
            var pdfBytes = await GeneratePdfAsync(quoteId);

            var tempDir = Path.Combine(FileSystem.AppDataDirectory, "temp");
            Directory.CreateDirectory(tempDir);

            var quote = await _quoteSvc.GetQuoteByIdAsync(quoteId);
            var safeName = SanitizeFileName(quote?.TeklifNo ?? quoteId.ToString());
            var filePath = Path.Combine(tempDir, $"{safeName}.pdf");

            await File.WriteAllBytesAsync(filePath, pdfBytes);

#if WINDOWS
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
#endif
        }

        /// <summary>DevExpress TeklifReport ile PDF byte[] uretir.</summary>
        public async Task<byte[]> GeneratePdfAsync(int quoteId)
        {
            var quote = await _quoteSvc.GetQuoteByIdAsync(quoteId)
                ?? throw new Exception($"Teklif bulunamadi: {quoteId}");

            var items = await _quoteSvc.GetQuoteItemsAsync(quoteId);

            // Musteri ve ilgili kisi bilgileri
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

            // Tum ekleri tek seferde yukle
            var attachments = new List<QuoteAttachmentModel>();
            try { attachments = await _attachSvc.GetAttachmentsAsync(quoteId); }
            catch { }

            // Urun fotograflari (ilk 2 "foto" eki — disk'ten)
            byte[]? urunFotoBytes  = null;
            byte[]? urunFotoBytes2 = null;
            try
            {
                var fotograflar = attachments
                    .Where(a => a.Tip == "foto" && File.Exists(a.DosyaYolu))
                    .OrderBy(a => a.SiraNo)
                    .Take(2)
                    .ToList();
                if (fotograflar.Count >= 1)
                    urunFotoBytes  = await File.ReadAllBytesAsync(fotograflar[0].DosyaYolu);
                if (fotograflar.Count >= 2)
                    urunFotoBytes2 = await File.ReadAllBytesAsync(fotograflar[1].DosyaYolu);
            }
            catch { /* foto yoksa sessiz devam */ }

            // Teknik resimler ("teknikresim" eki — disk'ten)
            var cizimImages = new List<byte[]>();
            try
            {
                foreach (var c in attachments
                    .Where(a => a.Tip == "teknikresim" && File.Exists(a.DosyaYolu))
                    .OrderBy(a => a.SiraNo))
                {
                    cizimImages.Add(await File.ReadAllBytesAsync(c.DosyaYolu));
                }
            }
            catch { }

            // Urun baslik metnini olustur (buyuk harf, firma + urun adi + 2.el etiketi)
            var urunAdi    = items.FirstOrDefault(i => i.KalemTipi == "HEADER")?.Aciklama ?? "";
            var urunBaslik = string.IsNullOrWhiteSpace(urunAdi)
                ? ""
                : "YALÇIN DORSE  ·  " + urunAdi.ToUpperInvariant()
                  + (quote.SatisTipi == "SecondHand" ? "  ·  2. EL ÜRÜN" : "");
            var urunAltYazi = string.IsNullOrWhiteSpace(urunAdi)
                ? ""
                : $"(Fotoğraflar, {urunAdi.ToLowerInvariant()} ürününe aittir.)";

            // Raporu olustur ve doldur
            var report = new TeklifReport();
            report.SetQuoteData(
                teklifNo: quote.TeklifNo,
                tarih: quote.TalepTarihi.ToString("dd.MM.yyyy"),
                gecerlilikTarihi: quote.GecerlilikTarihi.ToString("dd.MM.yyyy"),
                musteriAdi: musteriAdi,
                musteriKodu: musteriKodu,
                ilgiliKisi: ilgiliKisi,
                ilgiliEmail: ilgiliEmail,
                ilgiliMobil: ilgiliMobil,
                saticiAdi: saticiAdi,
                saticiEmail: saticiEmail,
                saticiTelefon: saticiTelefon,
                netTutar: quote.NetTutar.ToString("N2"),
                paraBirimi: quote.ParaBirimi,
                urunAdi: urunAdi,
                sasiNo: quote.SasiNo ?? "",
                modelYili: quote.ModelYili?.ToString() ?? ""
            );
            report.SetUrunImage(urunFotoBytes, urunBaslik, urunAltYazi, urunFotoBytes2);

            // SPEC tablolari (KalemTipi="SPEC" olan iki-kolon tablo satirlari)
            var specGroups = new List<TeklifReport.SpecGroup>();
            var headers = items.Where(i => i.KalemTipi == "HEADER").OrderBy(i => i.SiraNo).ToList();
            foreach (var header in headers)
            {
                var specRows = items
                    .Where(i => i.UstKalemId == header.Id && i.KalemTipi == "SPEC")
                    .OrderBy(i => i.SiraNo)
                    .ToList();
                if (specRows.Count > 0)
                {
                    specGroups.Add(new TeklifReport.SpecGroup
                    {
                        GrupAdi = header.Aciklama,
                        Rows    = specRows.Select(r => (r.Aciklama, r.Birim ?? "")).ToList()
                    });
                }
            }

            report.SetSpecData(specGroups, cizimImages);

            // Numarali liste (HEADER + ITEM satirlari, PDF'de teknik resimden sonra)
            var listItems = new List<TeklifReport.ListItem>();
            int hNum = 0;
            foreach (var header in headers)
            {
                hNum++;
                listItems.Add(new TeklifReport.ListItem($"{hNum}.", header.Aciklama, true, true));

                var listChildren = items
                    .Where(i => i.UstKalemId == header.Id && i.KalemTipi == "ITEM")
                    .OrderBy(i => i.SiraNo)
                    .ToList();
                int cNum = 0;
                foreach (var child in listChildren)
                {
                    cNum++;
                    listItems.Add(new TeklifReport.ListItem(
                        $"{hNum}.{cNum}.", child.Aciklama, child.BaslikMi, false));
                }
            }
            report.SetListData(listItems);

            return report.ExportToPdfBytes();
        }

        // ─────────────────────────────────────────────
        //  QuestPDF karsilastirma
        // ─────────────────────────────────────────────

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

            // Logo
            byte[]? logoBytes = null;
            try
            {
                var logoPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo.png");
                if (File.Exists(logoPath))
                    logoBytes = await File.ReadAllBytesAsync(logoPath);
            }
            catch { }

            // Spec grupları
            var specGroups = new List<TeklifReport.SpecGroup>();
            var headers    = items.Where(i => i.KalemTipi == "HEADER").OrderBy(i => i.SiraNo).ToList();
            foreach (var header in headers)
            {
                var specRows = items
                    .Where(i => i.UstKalemId == header.Id && i.KalemTipi == "SPEC")
                    .OrderBy(i => i.SiraNo).ToList();
                if (specRows.Count > 0)
                    specGroups.Add(new TeklifReport.SpecGroup
                    {
                        GrupAdi = header.Aciklama,
                        Rows    = specRows.Select(r => (r.Aciklama, r.Birim ?? "")).ToList()
                    });
            }

            // Numaralı liste
            var listItems = new List<TeklifReport.ListItem>();
            int hNum = 0;
            foreach (var header in headers)
            {
                hNum++;
                listItems.Add(new TeklifReport.ListItem($"{hNum}.", header.Aciklama, true, true));
                var children = items
                    .Where(i => i.UstKalemId == header.Id && i.KalemTipi == "ITEM")
                    .OrderBy(i => i.SiraNo).ToList();
                int cNum = 0;
                foreach (var child in children)
                {
                    cNum++;
                    listItems.Add(new TeklifReport.ListItem(
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
            };

            return report.GeneratePdf();
        }

        // ─────────────────────────────────────────────
        //  Word (DOCX) uretim
        // ─────────────────────────────────────────────

        /// <summary>Sablon dosyalarinin tutuldugu klasor.</summary>
        public static string TemplatesDir =>
            Path.Combine(FileSystem.AppDataDirectory, "templates");

        /// <summary>Tek sablon dosya yolu.</summary>
        public static string GetTemplatePath() =>
            Path.Combine(TemplatesDir, "teklif_template.docx");

        /// <summary>Sablon klasorunu olusturur; sablon yoksa varsayilan olusturur.</summary>
        public static async Task EnsureTemplatesAsync()
        {
            Directory.CreateDirectory(TemplatesDir);
            var path = GetTemplatePath();
            if (!File.Exists(path))
                await CreateDefaultTemplateAsync(path);
        }

        /// <summary>Sablon dosyasinin bulundugu klasoru Gezgin'de acar.</summary>
        public static void OpenTemplatesFolder()
        {
            Directory.CreateDirectory(TemplatesDir);
#if WINDOWS
            Process.Start(new ProcessStartInfo("explorer.exe", TemplatesDir) { UseShellExecute = true });
#endif
        }

        /// <summary>Belirtilen teklif icin DOCX uretir; dosya yolunu dondurur.</summary>
        public async Task<string> GenerateDocxFileAsync(int quoteId)
        {
            var templatePath = GetTemplatePath();
            if (!File.Exists(templatePath))
                throw new FileNotFoundException(
                    $"Sablon bulunamadi: {templatePath}\n" +
                    "Lutfen 'Sablon Klasoru' butonuna basip klasoru acin ve " +
                    "teklif_template.docx dosyasini oraya koyun.");

            var quote = await _quoteSvc.GetQuoteByIdAsync(quoteId)
                ?? throw new Exception($"Teklif bulunamadi: {quoteId}");

            // Ilgili kisi bilgileri
            string ilgiliKisi = "", ilgiliEmail = "", ilgiliMobil = "";
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                const string sql = """
                    SELECT cc."ContactName", cc."Email", cc."Mobile"
                    FROM "YLTeklifler" q
                    LEFT JOIN "YLCustomerContacts" cc ON cc."Id" = q."IlgiliKisiId"
                    WHERE q."Id" = @id;
                    """;
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", quoteId);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    ilgiliKisi  = r.IsDBNull(0) ? "" : r.GetString(0);
                    ilgiliEmail = r.IsDBNull(1) ? "" : r.GetString(1);
                    ilgiliMobil = r.IsDBNull(2) ? "" : r.GetString(2);
                }
            }
            catch { }

            // Temp dosya hazirla
            var tempDir = Path.Combine(FileSystem.AppDataDirectory, "temp");
            Directory.CreateDirectory(tempDir);
            var safeName = SanitizeFileName(quote.TeklifNo ?? quoteId.ToString());
            var tempPath = Path.Combine(tempDir, $"{safeName}.docx");
            File.Copy(templatePath, tempPath, overwrite: true);

            // Placeholder'lari degistir
            var placeholders = new Dictionary<string, string>
            {
                ["{{ILGILI_KISI}}"]  = ilgiliKisi,
                ["{{ILGILI_EMAIL}}"] = ilgiliEmail,
                ["{{ILGILI_MOBIL}}"] = ilgiliMobil,
            };

            using (var wordDoc = WordprocessingDocument.Open(tempPath, true))
            {
                var mainPart = wordDoc.MainDocumentPart!;

                // Ana govde
                var bodyXml = mainPart.Document.InnerXml;
                foreach (var (key, val) in placeholders)
                    bodyXml = bodyXml.Replace(key, XmlEscape(val));
                mainPart.Document.InnerXml = bodyXml;

                // Ustbilgi (header)
                foreach (var hdr in mainPart.HeaderParts)
                {
                    var hdrXml = hdr.Header.InnerXml;
                    foreach (var (key, val) in placeholders)
                        hdrXml = hdrXml.Replace(key, XmlEscape(val));
                    hdr.Header.InnerXml = hdrXml;
                    hdr.Header.Save();
                }

                // Altbilgi (footer)
                foreach (var ftr in mainPart.FooterParts)
                {
                    var ftrXml = ftr.Footer.InnerXml;
                    foreach (var (key, val) in placeholders)
                        ftrXml = ftrXml.Replace(key, XmlEscape(val));
                    ftr.Footer.InnerXml = ftrXml;
                    ftr.Footer.Save();
                }

                mainPart.Document.Save();
            }

            return tempPath;
        }

        /// <summary>DOCX uretip varsayilan uygulamada (Word) acar.</summary>
        public async Task OpenDocxAsync(int quoteId)
        {
            var filePath = await GenerateDocxFileAsync(quoteId);
#if WINDOWS
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
#endif
        }

        /// <summary>XML icin ozel karakterleri kacirma.</summary>
        private static string XmlEscape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        /// <summary>Varsayilan sablon olusturur (placeholder'larla basit bir belge).</summary>
        private static async Task CreateDefaultTemplateAsync(string path)
        {
            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(
                new Paragraph(new Run(
                    new RunProperties(new Bold()),
                    new Text("Sayin {{ILGILI_KISI}},"))),
                new Paragraph(new Run(new Text("E-mail : {{ILGILI_EMAIL}}"))),
                new Paragraph(new Run(new Text("Mobil  : {{ILGILI_MOBIL}}"))),
                new Paragraph(new Run(new Text(""))),
                new Paragraph(new Run(new Text(
                    "Bu varsayilan sablon dosyasidir. Gercek sablon dosyanizi " +
                    "teklif_template.docx adıyla bu klasore kopyalayin.")))
            ));
            mainPart.Document.Save();
            await Task.CompletedTask;
        }

        // ─────────────────────────────────────────────
        //  Yardimci metotlar
        // ─────────────────────────────────────────────

        private static string SanitizeFileName(string name) =>
            string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    }
}
