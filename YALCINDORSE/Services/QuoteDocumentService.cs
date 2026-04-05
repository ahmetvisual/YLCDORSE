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

            return report.ExportToPdfBytes();
        }

        // ─────────────────────────────────────────────
        //  Yardimci metotlar
        // ─────────────────────────────────────────────

        private static string SanitizeFileName(string name) =>
            string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    }
}
