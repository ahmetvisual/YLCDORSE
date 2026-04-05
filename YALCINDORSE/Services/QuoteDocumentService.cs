using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Npgsql;
using System.Diagnostics;
using System.Text;
using YALCINDORSE.Helpers;

namespace YALCINDORSE.Services
{
    /// <summary>
    /// Teklif belgesi uretim servisi.
    /// Yaklasim: .docx sablonu + {{PLACEHOLDER}} metin yer tutuculari.
    /// Sablon AppDataDirectory/templates/ klasorunde saklanir; kullanici Word ile duzenliyebilir.
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
        //  Sablon dizin yonetimi
        // ─────────────────────────────────────────────

        public static string TemplatesDir =>
            Path.Combine(FileSystem.AppDataDirectory, "templates");

        /// <summary>Tek sablon dosyasi — dil farki veri duzeyinde ceviri ile cozulur.</summary>
        public static string GetTemplatePath(string dil = "") =>
            Path.Combine(TemplatesDir, "teklif_template.docx");

        /// <summary>
        /// Sablon dosyasini ilk calismada olusturur (yoksa).
        /// Kullanici bu dosyayi kendi Word sablonuyla degistirebilir.
        /// </summary>
        public async Task EnsureTemplatesAsync()
        {
            Directory.CreateDirectory(TemplatesDir);
            var path = GetTemplatePath();
            if (!File.Exists(path))
                await CreateDefaultTemplateAsync(path, "TR");
        }

        public static void OpenTemplatesFolder()
        {
#if WINDOWS
            Process.Start("explorer.exe", TemplatesDir);
#endif
        }

        // ─────────────────────────────────────────────
        //  Ana uretim metodu
        // ─────────────────────────────────────────────

        /// <summary>Dolu .docx byte[] doner.</summary>
        public async Task<byte[]> GenerateDocxAsync(int quoteId)
        {
            await EnsureTemplatesAsync();

            var quote = await _quoteSvc.GetQuoteByIdAsync(quoteId)
                ?? throw new Exception($"Teklif bulunamadi: {quoteId}");

            var items = await _quoteSvc.GetQuoteItemsAsync(quoteId);
            var specs = await _specSvc.GetSpecsAsync(quoteId);
            var attachments = await _attachSvc.GetAttachmentsAsync(quoteId);

            // Musteri ve ilgili kisi bilgileri
            string musteriAdi = "", musteriKodu = "";
            string ilgiliKisi = "", ilgiliEmail = "", ilgiliMobil = "";
            string saticiAdi = "", saticiEmail = "";
            try
            {
                using var conn = _db.GetConnection();
                await conn.OpenAsync();
                const string sql = """
                    SELECT c."Title", c."CustomerCode",
                           cc."ContactName", cc."Email", cc."Mobile",
                           u."FullName", u."Email" as "SaticiEmail"
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
                    musteriAdi   = r.IsDBNull(0) ? "" : r.GetString(0);
                    musteriKodu  = r.IsDBNull(1) ? "" : r.GetString(1);
                    ilgiliKisi   = r.IsDBNull(2) ? "" : r.GetString(2);
                    ilgiliEmail  = r.IsDBNull(3) ? "" : r.GetString(3);
                    ilgiliMobil  = r.IsDBNull(4) ? "" : r.GetString(4);
                    saticiAdi    = r.IsDBNull(5) ? "" : r.GetString(5);
                    saticiEmail  = r.IsDBNull(6) ? "" : r.GetString(6);
                }
            }
            catch { }

            // Tek sablon dosyasi
            var dil = string.IsNullOrEmpty(quote.Dil) ? "TR" : quote.Dil;
            var templatePath = GetTemplatePath();

            // Sablon'u memory'e kopyala, orijinali koru
            var ms = new MemoryStream();
            await using (var fs = File.OpenRead(templatePath))
                await fs.CopyToAsync(ms);
            ms.Position = 0;

            // Placeholder degerleri
            var isEN = dil == "EN";
            var kdvNot = quote.KdvDahilMi
                ? (isEN ? "VAT Included" : "KDV Dahil")
                : (isEN ? "+ VAT" : "+ KDV");

            var placeholders = new Dictionary<string, string>
            {
                ["{{TEKLIF_NO}}"]          = quote.TeklifNo,
                ["{{TARIH}}"]              = quote.TalepTarihi.ToString("dd.MM.yyyy"),
                ["{{GECERLILIK_TARIHI}}"]  = quote.GecerlilikTarihi.ToString("dd.MM.yyyy"),
                ["{{MUSTERI_ADI}}"]        = musteriAdi,
                ["{{MUSTERI_KODU}}"]       = musteriKodu,
                ["{{ILGILI_KISI}}"]        = ilgiliKisi,
                ["{{ILGILI_EMAIL}}"]       = ilgiliEmail,
                ["{{ILGILI_MOBIL}}"]       = ilgiliMobil,
                ["{{SATICI_ADI}}"]         = saticiAdi,
                ["{{SATICI_EMAIL}}"]       = saticiEmail,
                ["{{AKS_SAYISI}}"]         = quote.AksSayisi?.ToString() ?? "",
                ["{{TEKLIF_TIPI}}"]        = quote.TeklifTipi ?? "",
                ["{{TESLIMAT_HAFTASI}}"]   = quote.TeslimatHaftasi ?? "",
                ["{{INCOTERMS}}"]          = quote.TeslimatTipiKodu ?? "",
                ["{{TESLIMAT_YERI}}"]      = quote.TeslimatYeri ?? "",
                ["{{ODEME_SISTEMI}}"]      = FormatOdemeSistemi(quote.OdemeSistemi, isEN),
                ["{{KDV_NOT}}"]            = kdvNot,
                ["{{NET_TUTAR}}"]          = quote.NetTutar.ToString("N2"),
                ["{{PARA_BIRIMI}}"]        = quote.ParaBirimi,
                ["{{REVIZYON_NO}}"]        = quote.RevizyonNo.ToString(),
                ["{{NOTLAR}}"]             = quote.Notlar ?? "",
                ["{{URUN_ADI}}"]           = items.FirstOrDefault(i => i.KalemTipi == "HEADER")?.Aciklama ?? "",
            };

            // Belgeleri isle
            using (var doc = WordprocessingDocument.Open(ms, isEditable: true))
            {
                var body = doc.MainDocumentPart!.Document.Body!;

                // 1. Dinamik tablolari isle (once tablolar, sonra metin replacement)
                FillDynamicTables(body, items, specs);

                // 2. Metin placeholder'larini degistir (run birlesimi ile)
                ReplaceTextPlaceholders(body, placeholders);

                // 3. Gorselleri ekle
                await InsertImagesAsync(doc, attachments);

                doc.MainDocumentPart!.Document.Save();
            }

            return ms.ToArray();
        }

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

            // Urun fotografini yukle (ilk "foto" eki — disk'ten)
            byte[]? urunFotoBytes = null;
            try
            {
                var attachments = await _attachSvc.GetAttachmentsAsync(quoteId);
                var ilkFoto = attachments.FirstOrDefault(a => a.Tip == "foto" && File.Exists(a.DosyaYolu));
                if (ilkFoto != null)
                    urunFotoBytes = await File.ReadAllBytesAsync(ilkFoto.DosyaYolu);
            }
            catch { /* foto yoksa sessiz devam */ }

            // Urun baslik metnini olustur (buyuk harf, firma + urun adi + 2.el etiketi)
            var urunAdi    = items.FirstOrDefault(i => i.KalemTipi == "HEADER")?.Aciklama ?? "";
            var urunBaslik = string.IsNullOrWhiteSpace(urunAdi)
                ? ""
                : "YALÇIN DORSE\n" + urunAdi.ToUpperInvariant()
                  + (quote.SatisTipi == "SecondHand" ? "\n2. EL ÜRÜN" : "");
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
            report.SetUrunImage(urunFotoBytes, urunBaslik, urunAltYazi);

            return report.ExportToPdfBytes();
        }

        /// <summary>PDF'e donusturmeyi dener. LibreOffice kuruluysa PDF yolunu, yoksa null doner.</summary>
        public async Task<string?> TryConvertToPdfAsync(string docxPath)
        {
            var sofficePath = FindLibreOffice();
            if (sofficePath == null) return null;

            var outDir = Path.GetDirectoryName(docxPath) ?? FileSystem.AppDataDirectory;
            var psi = new ProcessStartInfo(sofficePath,
                $"--headless --convert-to pdf --outdir \"{outDir}\" \"{docxPath}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            if (proc == null) return null;

            await proc.WaitForExitAsync();
            var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
            return File.Exists(pdfPath) ? pdfPath : null;
        }

        // ─────────────────────────────────────────────
        //  Dinamik tablo satirlari doldurma
        // ─────────────────────────────────────────────

        private void FillDynamicTables(Body body, List<QuoteItemModel> items, List<QuoteSpecModel> specs)
        {
            var tables = body.Descendants<Table>().ToList();
            foreach (var table in tables)
            {
                var rows = table.Elements<TableRow>().ToList();
                foreach (var row in rows)
                {
                    var text = GetRowText(row);
                    if (text.Contains("{{#SPEC_ROW}}"))
                        FillSpecRows(table, row, specs);
                    else if (text.Contains("{{#KALEM_ROW}}"))
                        FillKalemRows(table, row, items.Where(i => i.KalemTipi != "OPTION").ToList());
                    else if (text.Contains("{{#OPSIYON_ROW}}"))
                        FillKalemRows(table, row, items.Where(i => i.KalemTipi == "OPTION").ToList(), isOption: true);
                }
            }
        }

        private void FillSpecRows(Table table, TableRow templateRow, List<QuoteSpecModel> specs)
        {
            if (!specs.Any()) { templateRow.Remove(); return; }

            string? lastGrup = null;
            int grupSira = 0;

            foreach (var spec in specs)
            {
                // Grup basligini ayri satir olarak ekle (degisirse)
                if (spec.Grup != lastGrup)
                {
                    lastGrup = spec.Grup;
                    grupSira++;
                    var grupRow = (TableRow)templateRow.CloneNode(true);
                    SetCellText(grupRow, 0, spec.Grup.ToUpperInvariant());
                    SetCellText(grupRow, 1, "");
                    SetCellText(grupRow, 2, "");
                    MakeBold(grupRow);
                    table.InsertBefore(grupRow, templateRow);
                }

                var newRow = (TableRow)templateRow.CloneNode(true);
                SetCellText(newRow, 0, "");
                SetCellText(newRow, 1, spec.Ozellik);
                SetCellText(newRow, 2, spec.Deger ?? "");
                table.InsertBefore(newRow, templateRow);
            }

            templateRow.Remove();
        }

        private void FillKalemRows(Table table, TableRow templateRow, List<QuoteItemModel> items, bool isOption = false)
        {
            if (!items.Any()) { templateRow.Remove(); return; }

            foreach (var item in items)
            {
                if (item.KalemTipi == "HEADER") continue; // Baslik satirlarini atla

                var newRow = (TableRow)templateRow.CloneNode(true);
                SetCellText(newRow, 0, item.Aciklama);
                if (GetCellCount(newRow) > 1) SetCellText(newRow, 1, item.Miktar?.ToString("N0") ?? "");
                if (GetCellCount(newRow) > 2) SetCellText(newRow, 2, item.Birim ?? "");
                if (GetCellCount(newRow) > 3) SetCellText(newRow, 3, item.BirimFiyat?.ToString("N2") ?? "");
                if (GetCellCount(newRow) > 4) SetCellText(newRow, 4, item.Tutar?.ToString("N2") ?? "");
                table.InsertBefore(newRow, templateRow);
            }

            templateRow.Remove();
        }

        // ─────────────────────────────────────────────
        //  Metin placeholder replacement
        // ─────────────────────────────────────────────

        private void ReplaceTextPlaceholders(Body body, Dictionary<string, string> map)
        {
            // Run birlestirme: OpenXML bazen tek metni birden fazla run'a boler
            // Her paragrafin run'larini birlestiriyoruz
            foreach (var para in body.Descendants<Paragraph>())
                MergeRuns(para);

            // Simdi tum metin degerlerini degistir
            foreach (var run in body.Descendants<Run>())
            {
                var t = run.GetFirstChild<Text>();
                if (t == null) continue;
                var val = t.Text;
                foreach (var kv in map)
                    val = val.Replace(kv.Key, kv.Value ?? "");
                if (val != t.Text)
                {
                    t.Text = val;
                    t.Space = SpaceProcessingModeValues.Preserve;
                }
            }
        }

        private void MergeRuns(Paragraph para)
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count <= 1) return;

            // Sadece text iceren, formatting'si ayni olan ardisik run'lari birlestir
            var sb = new StringBuilder();
            Run? firstRun = null;

            foreach (var run in runs)
            {
                var t = run.GetFirstChild<Text>();
                if (t == null) continue;
                if (firstRun == null) firstRun = run;
                sb.Append(t.Text);
            }

            if (firstRun == null) return;
            var combined = sb.ToString();

            // Herhangi bir placeholder icerenler icin birlestirilmis metin ver
            if (!combined.Contains("{{")) return;

            var firstT = firstRun.GetFirstChild<Text>();
            if (firstT != null) firstT.Text = combined;
            firstT!.Space = SpaceProcessingModeValues.Preserve;

            // Diger run'lari temizle
            foreach (var run in runs.Skip(1))
                run.Remove();
        }

        // ─────────────────────────────────────────────
        //  Gorsel ekleme
        // ─────────────────────────────────────────────

        private async Task InsertImagesAsync(WordprocessingDocument doc, List<QuoteAttachmentModel> attachments)
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            var photos = attachments.Where(a => a.Tip == "foto" && a.PdfeDahilEt)
                                    .OrderBy(a => a.SiraNo).ToList();
            var drawings = attachments.Where(a => a.Tip == "teknikresim" && a.PdfeDahilEt)
                                      .OrderBy(a => a.SiraNo).ToList();

            // Logo
            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "wwwroot", "images", "logo.png");

            await ReplaceImagePlaceholder(doc, body, "{{IMG_LOGO}}", logoPath);

            // Urun fotograflari
            for (int i = 0; i < Math.Min(photos.Count, 3); i++)
                await ReplaceImagePlaceholder(doc, body, $"{{{{IMG_URUN_{i + 1}}}}}", photos[i].DosyaYolu);

            // Teknik cizimler
            for (int i = 0; i < Math.Min(drawings.Count, 3); i++)
                await ReplaceImagePlaceholder(doc, body, $"{{{{IMG_TEKNIK_{i + 1}}}}}", drawings[i].DosyaYolu);

            // Kullanilmayan placeholder'lari temizle
            foreach (var run in body.Descendants<Run>().ToList())
            {
                var t = run.GetFirstChild<Text>();
                if (t?.Text?.Contains("{{IMG_") == true) t.Text = "";
            }
        }

        private async Task ReplaceImagePlaceholder(
            WordprocessingDocument doc, Body body, string placeholder, string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return;

            foreach (var para in body.Descendants<Paragraph>().ToList())
            {
                var text = string.Concat(para.Descendants<Text>().Select(t => t.Text));
                if (!text.Contains(placeholder)) continue;

                // Paragrafin run'larini temizle
                foreach (var r in para.Elements<Run>().ToList()) r.Remove();

                // Resim ekle
                try
                {
                    var imgBytes = await File.ReadAllBytesAsync(imagePath);
                    var ext = Path.GetExtension(imagePath).ToLowerInvariant();
                    var contentType = ext == ".png" ? "image/png" : "image/jpeg";
                    var imgType = ext == ".png"
                        ? ImagePartType.Png
                        : ImagePartType.Jpeg;

                    var imagePart = doc.MainDocumentPart!.AddImagePart(imgType);
                    using var ms = new MemoryStream(imgBytes);
                    imagePart.FeedData(ms);

                    var relId = doc.MainDocumentPart.GetIdOfPart(imagePart);
                    var imgEl = CreateImageElement(relId, 6000000L, 4000000L); // 6x4 cm EMU
                    var imgRun = new Run(imgEl);
                    para.AppendChild(imgRun);
                }
                catch { /* Gorsel yuklenemedi, bos birak */ }
                break;
            }
        }

        private static OpenXmlElement CreateImageElement(string relId, long cx, long cy)
        {
            return new Drawing(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = cx, Cy = cy },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent
                        { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1, Name = "Image" },
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                        new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }),
                    new DocumentFormat.OpenXml.Drawing.Graphic(
                        new DocumentFormat.OpenXml.Drawing.GraphicData(
                            new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties
                                        { Id = 0, Name = "img" },
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                                new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                    new DocumentFormat.OpenXml.Drawing.Blip { Embed = relId },
                                    new DocumentFormat.OpenXml.Drawing.Stretch(
                                        new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                                new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                    new DocumentFormat.OpenXml.Drawing.Transform2D(
                                        new DocumentFormat.OpenXml.Drawing.Offset { X = 0, Y = 0 },
                                        new DocumentFormat.OpenXml.Drawing.Extents { Cx = cx, Cy = cy }),
                                    new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                        new DocumentFormat.OpenXml.Drawing.AdjustValueList())
                                    { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 });
        }

        // ─────────────────────────────────────────────
        //  Varsayilan sablon olusturma
        // ─────────────────────────────────────────────

        private async Task CreateDefaultTemplateAsync(string path, string dil)
        {
            var isEN = dil == "EN";
            await Task.Run(() =>
            {
                using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Sayfa kenar bosluklari
                var sectPr = new SectionProperties(
                    new PageMargin { Top = 720, Right = 720, Bottom = 720, Left = 720, Header = 360, Footer = 360 });
                body.AppendChild(sectPr);

                // === BASLIK BOLUMU ===
                var hdrTable = CreateTable(body, 2);
                SetCellContent(hdrTable, 0, 0, "{{IMG_LOGO}}", bold: false, fontSize: "20");
                var hdrRight = BuildHdrRight(isEN);
                SetCellContent(hdrTable, 0, 1, hdrRight, bold: false);

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === MUSTERI & TEKLIF BILGILERI ===
                AddHeading(body, isEN ? "OFFER INFORMATION" : "TEKLIF BILGILERI");
                var infoTable = CreateTable(body, 2);
                var leftInfo = isEN
                    ? $"Customer: {{{{MUSTERI_ADI}}}}\nContact: {{{{ILGILI_KISI}}}}\nCust. Code: {{{{MUSTERI_KODU}}}}"
                    : $"Musteri: {{{{MUSTERI_ADI}}}}\nIlgili Kisi: {{{{ILGILI_KISI}}}}\nMusteri Kodu: {{{{MUSTERI_KODU}}}}";
                var rightInfo = isEN
                    ? $"Offer No: {{{{TEKLIF_NO}}}}\nDate: {{{{TARIH}}}}\nValid Until: {{{{GECERLILIK_TARIHI}}}}\nSales Rep: {{{{SATICI_ADI}}}}"
                    : $"Teklif No: {{{{TEKLIF_NO}}}}\nTarih: {{{{TARIH}}}}\nGecerlilik: {{{{GECERLILIK_TARIHI}}}}\nSatici: {{{{SATICI_ADI}}}}";
                SetCellContent(infoTable, 0, 0, leftInfo);
                SetCellContent(infoTable, 0, 1, rightInfo);

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === URUN ===
                AddHeading(body, isEN ? "PRODUCT" : "URUN");
                AddParagraph(body, "{{URUN_ADI}}", bold: true);
                AddParagraph(body, isEN
                    ? "Axles: {{AKS_SAYISI}} | Type: {{TEKLIF_TIPI}}"
                    : "Aks Sayisi: {{AKS_SAYISI}} | Tip: {{TEKLIF_TIPI}}");
                AddParagraph(body, "{{IMG_URUN_1}}");

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === TEKNIK OZELLIKLER ===
                AddHeading(body, isEN ? "TECHNICAL SPECIFICATIONS" : "TEKNIK OZELLIKLER");
                var specTable = CreateTable(body, 3);
                // Baslik satiri
                SetCellContent(specTable, 0, 0, isEN ? "GROUP" : "GRUP", bold: true);
                SetCellContent(specTable, 0, 1, isEN ? "SPECIFICATION" : "OZELLIK", bold: true);
                SetCellContent(specTable, 0, 2, isEN ? "VALUE" : "DEGER", bold: true);
                // Template row (marker)
                var specTemplateRow = AddTableRow(specTable);
                SetCellContent(specTemplateRow, 0, "{{#SPEC_ROW}}");
                SetCellContent(specTemplateRow, 1, "{{SPEC_ADI}}");
                SetCellContent(specTemplateRow, 2, "{{SPEC_DEGER}}");

                AddParagraph(body, "{{IMG_TEKNIK_1}}");
                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === FIYATLANDIRMA ===
                AddHeading(body, isEN ? "PRICING" : "FIYATLANDIRMA");
                var priceTable = CreateTable(body, 5);
                SetCellContent(priceTable, 0, 0, isEN ? "DESCRIPTION" : "ACIKLAMA", bold: true);
                SetCellContent(priceTable, 0, 1, isEN ? "QTY" : "MIKTAR", bold: true);
                SetCellContent(priceTable, 0, 2, isEN ? "UNIT" : "BIRIM", bold: true);
                SetCellContent(priceTable, 0, 3, isEN ? "UNIT PRICE" : "BIRIM FIYAT", bold: true);
                SetCellContent(priceTable, 0, 4, isEN ? "TOTAL" : "TOPLAM", bold: true);
                // Template row
                var kalemRow = AddTableRow(priceTable);
                SetCellContent(kalemRow, 0, "{{#KALEM_ROW}}{{KALEM_ACIKLAMA}}");
                SetCellContent(kalemRow, 1, "{{KALEM_MIKTAR}}");
                SetCellContent(kalemRow, 2, "{{KALEM_BIRIM}}");
                SetCellContent(kalemRow, 3, "{{KALEM_FIYAT}}");
                SetCellContent(kalemRow, 4, "{{KALEM_TUTAR}}");

                // Toplam satiri
                var totalRow = AddTableRow(priceTable);
                SetCellContent(totalRow, 0, isEN ? "NET TOTAL" : "NET TUTAR", bold: true);
                SetCellContent(totalRow, 1, "");
                SetCellContent(totalRow, 2, "");
                SetCellContent(totalRow, 3, "");
                SetCellContent(totalRow, 4, "{{NET_TUTAR}} {{PARA_BIRIMI}} {{KDV_NOT}}", bold: true);

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === OPSIYONEL OZELLIKLER ===
                AddHeading(body, isEN ? "OPTIONAL FEATURES" : "OPSIYONEL OZELLIKLER");
                var optTable = CreateTable(body, 2);
                SetCellContent(optTable, 0, 0, isEN ? "DESCRIPTION" : "ACIKLAMA", bold: true);
                SetCellContent(optTable, 0, 1, isEN ? "PRICE" : "FIYAT", bold: true);
                var optRow = AddTableRow(optTable);
                SetCellContent(optRow, 0, "{{#OPSIYON_ROW}}{{OPSIYON_ACIKLAMA}}");
                SetCellContent(optRow, 1, "{{OPSIYON_TUTAR}} {{PARA_BIRIMI}}");

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === TESLIMAT & ODEME ===
                AddHeading(body, isEN ? "DELIVERY & PAYMENT" : "TESLIMAT & ODEME");
                AddParagraph(body, isEN
                    ? "Delivery: {{TESLIMAT_HAFTASI}} weeks | Incoterms: {{INCOTERMS}} | Location: {{TESLIMAT_YERI}}"
                    : "Teslimat: {{TESLIMAT_HAFTASI}} hafta | Incoterms: {{INCOTERMS}} | Yer: {{TESLIMAT_YERI}}");
                AddParagraph(body, isEN
                    ? "Payment: {{ODEME_SISTEMI}}"
                    : "Odeme: {{ODEME_SISTEMI}}");

                body.AppendChild(new Paragraph(new Run(new Text(""))));

                // === NOT ===
                AddParagraph(body, "{{NOTLAR}}");

                // === ALT BILGI ===
                body.AppendChild(new Paragraph(new Run(new Text(""))));
                AddParagraph(body, "YALCIN DORSE DAMPER SAN. VE TIC. LTD. STI.", bold: true);
                AddParagraph(body, isEN
                    ? "E-mail: satis@yalcintrailer.com | Web: www.yalcintrailer.com"
                    : "E-posta: satis@yalcintrailer.com | Web: www.yalcintrailer.com");

                mainPart.Document.Save();
            });
        }

        // ─────────────────────────────────────────────
        //  OpenXML yardimci metotlar
        // ─────────────────────────────────────────────

        private static Table CreateTable(Body body, int colCount)
        {
            var table = new Table();
            var tblPr = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
            table.AppendChild(tblPr);

            var firstRow = new TableRow();
            for (int i = 0; i < colCount; i++)
                firstRow.AppendChild(new TableCell(new Paragraph(new Run(new Text("")))));
            table.AppendChild(firstRow);
            body.AppendChild(table);
            return table;
        }

        private static TableRow AddTableRow(Table table)
        {
            var row = new TableRow();
            var existingRow = table.Elements<TableRow>().First();
            int colCount = existingRow.Elements<TableCell>().Count();
            for (int i = 0; i < colCount; i++)
                row.AppendChild(new TableCell(new Paragraph(new Run(new Text("")))));
            table.AppendChild(row);
            return row;
        }

        private static void SetCellContent(Table table, int rowIdx, int colIdx, string text, bool bold = false, string? fontSize = null)
        {
            var row = table.Elements<TableRow>().ElementAt(rowIdx);
            SetCellContent(row, colIdx, text, bold, fontSize);
        }

        private static void SetCellContent(TableRow row, int colIdx, string text, bool bold = false, string? fontSize = null)
        {
            var cell = row.Elements<TableCell>().ElementAtOrDefault(colIdx);
            if (cell == null) return;
            var para = cell.GetFirstChild<Paragraph>() ?? cell.AppendChild(new Paragraph());
            foreach (var r in para.Elements<Run>().ToList()) r.Remove();

            foreach (var line in text.Split('\n'))
            {
                var run = new Run();
                var rPr = new RunProperties();
                if (bold) rPr.AppendChild(new Bold());
                if (fontSize != null) rPr.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = fontSize });
                if (rPr.HasChildren) run.AppendChild(rPr);
                run.AppendChild(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
                para.AppendChild(run);
                if (text.Contains('\n') && line != text.Split('\n').Last())
                    para.AppendChild(new Run(new Break()));
            }
        }

        private static void SetCellText(TableRow row, int colIdx, string text)
        {
            var cell = row.Elements<TableCell>().ElementAtOrDefault(colIdx);
            if (cell == null) return;
            var para = cell.GetFirstChild<Paragraph>() ?? cell.AppendChild(new Paragraph());
            foreach (var r in para.Elements<Run>().ToList()) r.Remove();
            para.AppendChild(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static int GetCellCount(TableRow row) => row.Elements<TableCell>().Count();

        private static void MakeBold(TableRow row)
        {
            foreach (var run in row.Descendants<Run>())
            {
                var rPr = run.GetFirstChild<RunProperties>() ?? run.PrependChild(new RunProperties());
                if (rPr.GetFirstChild<Bold>() == null) rPr.AppendChild(new Bold());
            }
        }

        private static string GetRowText(TableRow row) =>
            string.Concat(row.Descendants<Text>().Select(t => t.Text));

        private static void AddHeading(Body body, string text)
        {
            var para = new Paragraph();
            var pPr = new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" });
            para.AppendChild(pPr);
            var run = new Run(
                new RunProperties(new Bold(), new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "22" }),
                new Text(text));
            para.AppendChild(run);
            body.AppendChild(para);
        }

        private static void AddParagraph(Body body, string text, bool bold = false)
        {
            var para = new Paragraph();
            var run = new Run();
            if (bold) run.AppendChild(new RunProperties(new Bold()));
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            para.AppendChild(run);
            body.AppendChild(para);
        }

        private static string BuildHdrRight(bool isEN) => isEN
            ? "YALCIN DORSE DAMPER SAN. VE TIC. LTD. STI.\nOFFER / QUOTATION\n{{TEKLIF_NO}} — {{TARIH}}"
            : "YALCIN DORSE DAMPER SAN. VE TIC. LTD. STI.\nTEKLIF BELGESI\n{{TEKLIF_NO}} — {{TARIH}}";

        private static string FormatOdemeSistemi(string? kod, bool isEN) => kod switch
        {
            "PESIN" => isEN ? "Cash" : "Pesin",
            "VADELI" => isEN ? "Deferred" : "Vadeli",
            "KARTI" => isEN ? "Letter of Credit" : "Akreditif",
            _ => kod ?? ""
        };

        private static string? FindLibreOffice()
        {
            var candidates = new[]
            {
                @"C:\Program Files\LibreOffice\program\soffice.exe",
                @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                "soffice"
            };
            return candidates.FirstOrDefault(p => p == "soffice" || File.Exists(p));
        }

        private static string SanitizeFileName(string name) =>
            string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    }
}
