using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QContainer = QuestPDF.Infrastructure.IContainer;

namespace YALCINDORSE
{
    /// <summary>
    /// Profesyonel teklif belgesi — QuestPDF ile üretilir.
    /// Sade, temiz kurumsal tasarım — TeklifReport düzenine uygun.
    /// </summary>
    public class TeklifQuestPdfReport
    {
        // ─── Marka Renk Paleti ───────────────────────────────────────────────
        const string NavyDark    = "#0D1F3C";
        const string ListHeaderNavy = "#143A66";
        const string TableHeaderBg = "#EEF6FF";
        const string TableHeaderBorder = "#B7CCE2";
        const string BlueAccent  = "#1D4ED8";
        const string AccentLine  = "#38BDF8";
        const string NearWhite   = "#F8FAFC";
        const string DarkText    = "#0F172A";
        const string BodyText    = "#334155";
        const string MutedText   = "#64748B";
        const string BorderClr   = "#CBD5E1";
        const string SoftBorder  = "#E2E8F0";
        const string SectionBg   = "#F8FAFC";
        const string AltRowBg    = "#F1F5F9";
        const string CyanTxt     = "#0EA5E9";
        const string FooterBg    = "#0D1F3C";
        const string White       = "#FFFFFF";
        const string LightOnDark = "#BAE6FD";

        // ─── Veri ───────────────────────────────────────────────────────────
        public string TeklifNo          { get; set; } = "";
        public string Tarih             { get; set; } = "";
        public string GecerlilikTarihi  { get; set; } = "";
        public string MusteriAdi        { get; set; } = "";
        public string MusteriKodu       { get; set; } = "";
        // Eski tek kisi alanlari — geri uyumluluk + Greeting fallback'i.
        // Yeni cogul yapi: IlgiliKisiler listesi (asagida).
        public string IlgiliKisi        { get; set; } = "";
        public string IlgiliEmail       { get; set; } = "";
        public string IlgiliMobil       { get; set; } = "";
        /// <summary>Cari (musteri) icin tum aktif ilgili kisiler — coklu rendered edilir.</summary>
        public List<IlgiliKisiInfo> IlgiliKisiler { get; set; } = new();
        public string SaticiAdi         { get; set; } = "";
        public string SaticiEmail       { get; set; } = "";
        public string SaticiTelefon     { get; set; } = "";
        public string NetTutar          { get; set; } = "";
        public string ParaBirimi        { get; set; } = "";
        public string UrunBaslik        { get; set; } = "";
        public string UrunAltYazi       { get; set; } = "";
        public string SasiNo            { get; set; } = "";
        public string ModelYili         { get; set; } = "";

        public byte[]? LogoBytes  { get; set; }
        public byte[]? UrunFoto1  { get; set; }
        public byte[]? UrunFoto2  { get; set; }

        // ─── Firma bilgileri (FirmaService'ten doldurulur) ────────────────
        public string FirmaUnvan       { get; set; } = "";
        public string FirmaAdresTam    { get; set; } = "";
        public string FirmaTelefon     { get; set; } = "";
        public string FirmaEmail       { get; set; } = "";
        public string FirmaWeb         { get; set; } = "";
        public string FirmaVergiNo     { get; set; } = "";
        public byte[]? FirmaKapakFoto  { get; set; }
        /// <summary>Aktif IBAN'lar — her satir "BANKA — PARA — IBAN" formatinda.</summary>
        public List<string> IBANListesi { get; set; } = new();

        // ─── Teslimat detaylari (QuoteModel'den doldurulur) ───────────────
        public string TeslimatHaftasi  { get; set; } = "";
        public string TeslimatTipiKodu { get; set; } = "";  // EXW, FOB, CIF, FCA, CFR, DAP, DDP
        public string TeslimatYeri     { get; set; } = "";
        public string TeslimatNotlari  { get; set; } = "";  // Cok satirli — '\n' veya '\r\n' ile bolunur

        public List<SpecGroup>  SpecGroups  { get; set; } = new();
        public List<byte[]>     CizimImages { get; set; } = new();
        public List<ListItem>   ListItems   { get; set; } = new();

        // ─── İç Tipler ──────────────────────────────────────────────────────
        /// <summary>PDF'in sol ust kontak blogunda gosterilecek tek bir ilgili kisi.</summary>
        public class IlgiliKisiInfo
        {
            public string Ad      { get; set; } = "";  // ContactName
            public string Unvan   { get; set; } = "";  // ContactTitle
            public string Email   { get; set; } = "";
            public string Mobil   { get; set; } = "";  // Mobile
            public string Telefon { get; set; } = "";  // Phone
        }

        public class SpecGroup
        {
            public string GrupAdi { get; set; } = "";
            public List<SpecRow> Rows { get; set; } = new();
        }

        public class SpecRow
        {
            public string Ozellik { get; set; } = "";
            public string Deger { get; set; } = "";
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public string AltAciklama { get; set; } = "";
            public bool AltBold { get; set; }
            public bool AltItalic { get; set; }
        }

        public class ListItem
        {
            public string Metin    { get; set; } = "";
            public string Numara   { get; set; } = "";
            public bool   IsHeader { get; set; }
            public bool   Bold     { get; set; }
            public bool   Italic   { get; set; }

            public ListItem() { }
            public ListItem(string numara, string metin, bool isHeader, bool bold, bool italic = false)
            {
                Numara   = numara;
                Metin    = metin;
                IsHeader = isHeader;
                Bold     = bold;
                Italic   = italic;
            }
        }

        // ─── Üretim ─────────────────────────────────────────────────────────
        public byte[] GeneratePdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(0);
                    // Tum sayfalarda ust bosluk — 1. sayfa header'inin ustunde minimal beyaz
                    // bant, 2.+ sayfa icin profesyonel ust kenar bosluk olusturur.
                    page.MarginTop(8, Unit.Millimetre);
                    page.MarginBottom(0);
                    page.DefaultTextStyle(ts => ts.FontFamily("Segoe UI").FontSize(9));

                    page.Content().Column(col =>
                    {
                        col.Spacing(0);
                        BuildHeader(col);

                        // Ust bolum: ic icerik icin yatay padding (12mm)
                        col.Item().PaddingHorizontal(12, Unit.Millimetre).Column(inner =>
                        {
                            inner.Spacing(0);
                            inner.Item().Height(6);
                            BuildMainInfo(inner);
                            inner.Item().Height(6);
                            BuildGreeting(inner);
                        });

                        // Urun foto bolumu — outer col seviyesinde rendered ediliyor.
                        // Sebep: Tek goruntuyu TAM SAYFA genisligine (210mm) gore
                        // ortalayabilmek icin inner padding'in disinda olmasi gerekiyor.
                        BuildUrunSection(col);

                        // Alt bolum: kalan icerik tekrar 12mm yatay padding'de
                        col.Item().PaddingHorizontal(12, Unit.Millimetre).Column(inner =>
                        {
                            inner.Spacing(0);
                            BuildSpecSection(inner);
                            BuildCizimSection(inner);
                            BuildListSection(inner);
                            BuildTeslimatSection(inner);
                            BuildBankaSection(inner);
                            inner.Item().Height(6);
                        });
                    });

                    page.Footer().Element(BuildFooter);
                });
            }).GeneratePdf();
        }

        // ════════════════════════════════════════════════════════════════════
        //  HEADER — beyaz zemin, temiz kurumsal
        // ════════════════════════════════════════════════════════════════════
        private void BuildHeader(ColumnDescriptor col)
        {
            var firmaBaslik = string.IsNullOrWhiteSpace(FirmaUnvan) ? "YALÇIN DORSE" : FirmaUnvan;
            var firmaDetay = new List<string>();
            if (!string.IsNullOrWhiteSpace(FirmaAdresTam)) firmaDetay.Add(FirmaAdresTam);
            if (!string.IsNullOrWhiteSpace(FirmaTelefon)) firmaDetay.Add("Tel: " + FirmaTelefon);
            if (!string.IsNullOrWhiteSpace(FirmaWeb)) firmaDetay.Add(FirmaWeb);

            col.Item().Background(White)
               .PaddingHorizontal(12, Unit.Millimetre)
               .PaddingTop(7, Unit.Millimetre)
               .PaddingBottom(5, Unit.Millimetre)
               .Row(row =>
               {
                   // Logo
                   if (LogoBytes?.Length > 0)
                   {
                       row.ConstantItem(30, Unit.Millimetre)
                          .Height(16, Unit.Millimetre)
                          .Image(LogoBytes).FitArea();
                   }
                   else
                   {
                       row.ConstantItem(30, Unit.Millimetre)
                          .Height(16, Unit.Millimetre)
                          .AlignMiddle()
                          .Text(t => t.Span("LOGO").Bold().FontSize(10).FontColor(NavyDark));
                   }

                   row.ConstantItem(5, Unit.Millimetre);

                   row.RelativeItem().AlignMiddle().Column(c =>
                   {
                       c.Item().Text(t =>
                           t.Span(firmaBaslik.ToUpperInvariant()).Bold().FontSize(14).FontColor(NavyDark));
                       if (firmaDetay.Count > 0)
                       {
                           c.Item().Height(1.5f);
                           c.Item().Text(t =>
                               t.Span(string.Join("  |  ", firmaDetay))
                                .FontSize(7.2f).FontColor(MutedText));
                       }
                   });

                   row.ConstantItem(5, Unit.Millimetre);

                   // TEKLİF rozeti
                   row.ConstantItem(34, Unit.Millimetre)
                      .AlignMiddle()
                      .AlignRight()
                      .Border(0.75f)
                      .BorderColor(TableHeaderBorder)
                      .Padding(3, Unit.Millimetre)
                      .Column(c =>
                      {
                          c.Item().Text(t =>
                          {
                              t.AlignRight();
                              t.Span("TEKLİF FORMU").Bold().FontSize(13.5f).FontColor(BlueAccent);
                          });
                          c.Item().Height(1.5f).Background(AccentLine);
                          c.Item().Height(2);
                          c.Item().Text(t =>
                          {
                              t.AlignRight();
                              t.Span(TeklifNo).FontSize(8.5f).FontColor(MutedText);
                          });
                      });
               });

            col.Item().PaddingHorizontal(12, Unit.Millimetre)
               .Height(1.2f)
               .Background(AccentLine);
            col.Item().Height(5);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ANA BİLGİ — sol: ilgili kişi / sağ: teklif meta + SDU + müşteri
        //  TeklifReport düzenine uygun
        // ════════════════════════════════════════════════════════════════════
        private void BuildMainInfo(ColumnDescriptor col)
        {
            col.Item().Row(row =>
            {
                // ── SOL: Cari'nin tum aktif ilgili kisileri ────────────────────
                // Referans (HARPUT docx) duzeni: her kisi icin "Sayin {Ad}" basligi
                // altinda Unvan / E-mail / Mobil / Tel. Cogul kisi -> ust uste blok blok.
                row.RelativeItem()
                   .Background(NearWhite)
                   .Border(0.5f)
                   .BorderColor(SoftBorder)
                   .Padding(4, Unit.Millimetre)
                   .Column(c =>
                {
                    // Cogul liste yoksa eski tek-kisi alanindan tek elemanli liste yap (geri uyumluluk)
                    var kisiler = IlgiliKisiler.Count > 0
                        ? IlgiliKisiler
                        : (!string.IsNullOrWhiteSpace(IlgiliKisi)
                            ? new List<IlgiliKisiInfo>
                              {
                                  new()
                                  {
                                      Ad = IlgiliKisi,
                                      Email = IlgiliEmail,
                                      Mobil = IlgiliMobil
                                  }
                              }
                            : new List<IlgiliKisiInfo>());

                    bool hasAracBilgi =
                        !string.IsNullOrWhiteSpace(SasiNo) ||
                        !string.IsNullOrWhiteSpace(ModelYili);
                    bool hasSaticiBilgi =
                        !string.IsNullOrWhiteSpace(SaticiAdi) ||
                        !string.IsNullOrWhiteSpace(SaticiEmail) ||
                        !string.IsNullOrWhiteSpace(SaticiTelefon);

                    if (kisiler.Count == 0 && !hasAracBilgi && !hasSaticiBilgi) return;

                    c.Item().Text(t =>
                        t.Span("İLGİLİ KİŞİ").Bold().FontSize(7).FontColor(BlueAccent));
                    c.Item().Height(2);

                    bool firstKisi = true;
                    foreach (var k in kisiler)
                    {
                        // Kisiler arasi ince bosluk
                        if (!firstKisi) c.Item().Height(3);
                        firstKisi = false;

                        // "Sayin {Ad}" — kisi blogunun basligi (referans docx pattern'i)
                        if (!string.IsNullOrWhiteSpace(k.Ad))
                        {
                            c.Item().Text(t =>
                                t.Span($"Sayın {k.Ad}")
                                 .Bold().FontSize(9.5f).FontColor(DarkText));
                        }

                        // Unvan / Pozisyon (varsa)
                        if (!string.IsNullOrWhiteSpace(k.Unvan))
                        {
                            c.Item().Text(t =>
                                t.Span(k.Unvan).Italic().FontSize(7.5f).FontColor(MutedText));
                        }

                        if (!string.IsNullOrWhiteSpace(k.Email))
                            c.Item().Text(t =>
                            {
                                t.Span("E-mail : ").FontSize(8).FontColor(MutedText);
                                t.Span(k.Email).FontSize(8).FontColor(BodyText);
                            });
                        if (!string.IsNullOrWhiteSpace(k.Mobil))
                            c.Item().Text(t =>
                            {
                                t.Span("Mobil : ").FontSize(8).FontColor(MutedText);
                                t.Span(k.Mobil).FontSize(8).FontColor(BodyText);
                            });
                        if (!string.IsNullOrWhiteSpace(k.Telefon))
                            c.Item().Text(t =>
                            {
                                t.Span("Tel : ").FontSize(8).FontColor(MutedText);
                                t.Span(k.Telefon).FontSize(8).FontColor(BodyText);
                            });
                    }

                    if (hasSaticiBilgi)
                    {
                        if (kisiler.Count > 0) c.Item().Height(5);
                        c.Item().Text(t =>
                            t.Span("Satış Destek Uzmanı").Bold().FontSize(8).FontColor(MutedText));
                        if (!string.IsNullOrWhiteSpace(SaticiAdi))
                        {
                            c.Item().Text(t =>
                                t.Span(SaticiAdi).Bold().FontSize(9.5f).FontColor(DarkText));
                        }

                        var sduParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(SaticiEmail)) sduParts.Add(SaticiEmail);
                        if (!string.IsNullOrWhiteSpace(SaticiTelefon)) sduParts.Add(SaticiTelefon);
                        if (sduParts.Count > 0)
                        {
                            c.Item().Text(t =>
                                t.Span(string.Join("  |  ", sduParts))
                                 .FontSize(7.5f).FontColor(BodyText));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(SasiNo))
                    {
                        c.Item().Height(4);
                        c.Item().Text(t =>
                        {
                            t.Span("Şasi No : ").FontSize(8).FontColor(MutedText);
                            t.Span(SasiNo).FontSize(8).FontColor(BodyText);
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(ModelYili))
                        c.Item().Text(t =>
                        {
                            t.Span("Model Yılı : ").FontSize(8).FontColor(MutedText);
                            t.Span(ModelYili).FontSize(8).FontColor(BodyText);
                        });
                });

                row.ConstantItem(5, Unit.Millimetre);

                // ── SAĞ: Teklif meta + Müşteri ──────────────────────
                row.RelativeItem()
                   .Background(NearWhite)
                   .Border(0.5f)
                   .BorderColor(SoftBorder)
                   .Padding(4, Unit.Millimetre)
                   .Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.AlignRight();
                        t.Span("TEKLİF BİLGİLERİ").Bold().FontSize(7).FontColor(BlueAccent);
                    });
                    c.Item().Height(2);

                    // Tarih, Geçerlilik
                    c.Item().Text(t =>
                    {
                        t.AlignRight();
                        t.Span("Tarih : ").FontSize(8).FontColor(MutedText);
                        t.Span(Tarih).FontSize(8).FontColor(DarkText);
                    });
                    c.Item().Text(t =>
                    {
                        t.AlignRight();
                        t.Span("Geçerlilik : ").FontSize(8).FontColor(MutedText);
                        t.Span(GecerlilikTarihi).FontSize(8).FontColor(DarkText);
                    });

                    c.Item().Height(6);

                    // Müşteri adı + kodu
                    c.Item().Text(t =>
                    {
                        t.AlignRight();
                        t.Span(MusteriAdi).Bold().FontSize(10).FontColor(DarkText);
                    });
                    c.Item().Text(t =>
                    {
                        t.AlignRight();
                        t.Span("Müşteri Kodu : ").FontSize(8).FontColor(MutedText);
                        t.Span(MusteriKodu).FontSize(8).FontColor(DarkText);
                    });
                });
            });

            col.Item().Height(7);
            col.Item().Height(0.5f).Background(BorderClr);
            col.Item().Height(7);
        }

        // ════════════════════════════════════════════════════════════════════
        //  KARŞILAMA — italik açılış paragrafı
        // ════════════════════════════════════════════════════════════════════
        private void BuildGreeting(ColumnDescriptor col)
        {
            // Selamlama icin oncelik: cogul listenin ilk kisisi -> tek-kisi alani -> fallback.
            // MainInfo'da kisi blogu zaten basligi "Sayin {Ad}" ile basliyor; bu greeting
            // gov de yine ekleniyor (referans docx ile ayni). Ad yoksa "Sayin Yetkilim,".
            var greetingAd = IlgiliKisiler.FirstOrDefault()?.Ad
                             ?? IlgiliKisi;
            var greeting = !string.IsNullOrWhiteSpace(greetingAd)
                ? $"Sayın {greetingAd},"
                : "Sayın Yetkilim,";

            col.Item().Column(c =>
            {
                c.Item().Text(t =>
                    t.Span(greeting)
                     .Bold().FontSize(9.5f).FontColor(DarkText));
                c.Item().Height(3);
                c.Item().Text(t =>
                    t.Span("Firmamıza göstermiş olduğunuz ilgi için teşekkür ederiz. " +
                           "Aşağıdaki ürün/hizmet için hazırladığımız teklifimizi " +
                           "bilgilerinize saygıyla sunarız.")
                     .FontSize(9).FontColor(BodyText));
            });
            // 2 satır boşluk
            col.Item().Height(18);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ÜRÜN FOTOĞRAF BÖLÜMÜ — outer col seviyesinde (tam sayfa genisligi)
        //  Tek foto: 210mm sayfa genisligine gore ortalanir
        //  Cift foto: ic icerik genisliginde (12mm padding) yan yana
        // ════════════════════════════════════════════════════════════════════
        private void BuildUrunSection(ColumnDescriptor col)
        {
            bool hasImage = UrunFoto1?.Length > 0 && !string.IsNullOrWhiteSpace(UrunBaslik);
            if (!hasImage) return;

            col.Item().ShowEntire().Column(block =>
            {
                // Urun basligi
                block.Item().PaddingHorizontal(12, Unit.Millimetre)
                   .Row(row =>
                   {
                       row.AutoItem()
                          .Background(ListHeaderNavy)
                          .PaddingHorizontal(4, Unit.Millimetre)
                          .PaddingVertical(1.8f)
                          .Text(t => t.Span(UrunBaslik.ToUpperInvariant()).Bold().FontSize(9).FontColor(White));
                       row.RelativeItem()
                          .AlignBottom()
                          .Height(0.9f)
                          .Background(AccentLine);
                   });
                block.Item().Height(3);

            // Fotograf(lar)
            bool hasImg2 = UrunFoto2?.Length > 0;
            if (hasImg2)
            {
                // Cift foto: ic icerik alaninda (12mm padding) yan yana
                block.Item().PaddingHorizontal(12, Unit.Millimetre).Row(row =>
                {
                    row.RelativeItem().Height(95, Unit.Millimetre).Image(UrunFoto1!).FitArea();
                    row.ConstantItem(3, Unit.Millimetre);
                    row.RelativeItem().Height(95, Unit.Millimetre).Image(UrunFoto2!).FitArea();
                });
            }
            else
            {
                // Tek foto: TAM SAYFA (210mm) genisligine gore SIMETRIK ortalama.
                // PaddingHorizontal(15) + FitWidth -> goruntu kesin 180mm genislige
                // oturur, her iki yanda 15mm esit bosluk kalir. AlignCenter+FitArea
                // QuestPDF'te bazen icerigi sol-uste hizaliyor; bu pattern
                // matematiksel olarak hatasiz simetri saglar.
                block.Item().PaddingHorizontal(15, Unit.Millimetre)
                   .Image(UrunFoto1!).FitWidth();
            }

            // Alt yazi — basliklarla hizali olmasi icin 12mm yatay padding
            if (!string.IsNullOrWhiteSpace(UrunAltYazi))
            {
                block.Item().PaddingHorizontal(12, Unit.Millimetre)
                   .PaddingVertical(2, Unit.Millimetre)
                   .Text(t =>
                   {
                       t.AlignCenter();
                       t.Span(UrunAltYazi).Italic().FontSize(7.5f).FontColor(MutedText);
                   });
            }
            });

            col.Item().Height(6);
        }

        // ════════════════════════════════════════════════════════════════════
        //  SPEC TABLOLARI — sade grup başlıkları, panelsiz
        // ════════════════════════════════════════════════════════════════════
        private void BuildSpecSection(ColumnDescriptor col)
        {
            var activeGroups = SpecGroups.Where(g => g.Rows.Count > 0).ToList();
            if (activeGroups.Count == 0) return;

            foreach (var grp in activeGroups)
            {
                col.Item().EnsureSpace(128);

                // Grup basligi
                col.Item()
                   .Background(TableHeaderBg)
                   .Border(0.5f)
                   .BorderColor(TableHeaderBorder)
                   .PaddingVertical(2.2f)
                   .Row(row =>
                   {
                       row.ConstantItem(2.5f, Unit.Millimetre)
                          .Background(ListHeaderNavy);
                       row.ConstantItem(3, Unit.Millimetre);
                       row.RelativeItem()
                          .AlignMiddle()
                          .Text(t =>
                              t.Span(grp.GrupAdi.ToUpperInvariant())
                               .Bold().FontSize(8.8f).FontColor(NavyDark));
                   });

                bool alt = false;
                foreach (var spec in grp.Rows)
                {
                    string bg = alt ? AltRowBg : White;
                    col.Item().Background(bg).Row(row =>
                    {
                        row.RelativeItem()
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(2).PaddingLeft(10).PaddingRight(4)
                           .Text(t => t.Span(spec.Ozellik).FontSize(8.5f).FontColor(BodyText));

                        row.ConstantItem(0.5f).Background(BorderClr);

                        row.ConstantItem(7, Unit.Millimetre)
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .AlignCenter()
                           .Text(t => t.Span(":").FontSize(9).FontColor(MutedText));

                        row.ConstantItem(0.5f).Background(BorderClr);

                        row.RelativeItem()
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(2).PaddingLeft(6).PaddingRight(4)
                           .Text(t =>
                           {
                               var span = t.Span(spec.Deger).FontSize(8.5f).FontColor(DarkText);
                               if (spec.Bold) span.Bold();
                               if (spec.Italic) span.Italic();
                           });
                    });
                    if (!string.IsNullOrWhiteSpace(spec.AltAciklama))
                    {
                        col.Item().Background(bg)
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingLeft(14).PaddingRight(4).PaddingBottom(2)
                           .Text(t =>
                           {
                               var span = t.Span(spec.AltAciklama).FontSize(7.5f).FontColor(MutedText);
                               if (spec.AltBold) span.Bold();
                               if (spec.AltItalic) span.Italic();
                           });
                    }
                    alt = !alt;
                }

                // Grup altı ince çizgi
                col.Item().Height(0.5f).Background(TableHeaderBorder);
                col.Item().Height(3);
            }

            // Dipnot
            col.Item().Height(2);
            col.Item()
               .BorderLeft(2).BorderColor(MutedText)
               .PaddingLeft(3, Unit.Millimetre)
               .Text(t =>
                   t.Span("** TC. Karayolları' nın müsaade ettiği ağırlıklardır, " +
                          "çalışacağı ülke kurallarına göre farklılık gösterebilir.")
                    .Italic().FontSize(7.5f).FontColor(MutedText));

            col.Item().Height(6);
        }

        // ════════════════════════════════════════════════════════════════════
        //  TEKNİK RESİMLER
        // ════════════════════════════════════════════════════════════════════
        private void BuildCizimSection(ColumnDescriptor col)
        {
            var validImages = CizimImages.Where(b => b?.Length > 0).ToList();
            if (validImages.Count == 0) return;

            col.Item().Height(0.5f).Background(BorderClr);
            col.Item().Height(4);

            foreach (var img in validImages)
            {
                col.Item().Image(img).FitWidth();
                col.Item().Height(4);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  NUMARALI LİSTE
        // ════════════════════════════════════════════════════════════════════
        private void BuildListSection(ColumnDescriptor col)
        {
            if (ListItems.Count == 0) return;

            col.Item().Height(0.5f).Background(BorderClr);
            col.Item().Height(4);

            // Header bazinda gruplara bol — her grup: [header, ...children]
            var groups = new List<List<ListItem>>();
            List<ListItem>? cur = null;
            foreach (var li in ListItems)
            {
                if (li.IsHeader) { cur = new List<ListItem> { li }; groups.Add(cur); }
                else if (cur != null) cur.Add(li);
            }

            void RenderHeader(QContainer c, ListItem h)
            {
                c.Background(ListHeaderNavy)
                 .Border(0.5f)
                 .BorderColor(ListHeaderNavy)
                 .PaddingVertical(2.2f)
                 .PaddingHorizontal(4)
                 .Row(row =>
                 {
                     row.ConstantItem(11, Unit.Millimetre)
                        .Background(White)
                        .PaddingVertical(1)
                        .AlignCenter()
                        .AlignMiddle()
                        .Text(t => t.Span(h.Numara).Bold().FontSize(7.5f).FontColor(ListHeaderNavy));
                     row.ConstantItem(4, Unit.Millimetre);
                     row.RelativeItem()
                        .AlignMiddle()
                        .Text(t => t.Span(h.Metin.ToUpperInvariant()).Bold().FontSize(8.8f).FontColor(White));
                 });
            }

            void RenderChild(QContainer c, ListItem li)
            {
                c.Row(row =>
                {
                    row.ConstantItem(6, Unit.Millimetre);
                    row.ConstantItem(14, Unit.Millimetre)
                       .Text(t => t.Span(li.Numara).FontSize(8).FontColor(CyanTxt));
                    row.RelativeItem()
                       .PaddingVertical(1.5f)
                       .Text(t =>
                       {
                           var span = t.Span(li.Metin).FontSize(8.5f).FontColor(BodyText);
                           if (li.Bold) span.Bold();
                           if (li.Italic) span.Italic();
                       });
                });
            }

            bool firstGroup = true;
            foreach (var grp in groups)
            {
                var header = grp[0];
                var children = grp.Skip(1).ToList();

                if (!firstGroup) col.Item().Height(4);
                firstGroup = false;

                if (grp.Count <= 14)
                {
                    col.Item().ShowEntire().Column(c =>
                    {
                        c.Item().Element(ctn => RenderHeader(ctn, header));
                        foreach (var ch in children)
                            c.Item().Element(ctn => RenderChild(ctn, ch));
                    });
                    continue;
                }

                col.Item().EnsureSpace(160);

                // Header + ilk child birlikte (sayfada bolunmesinler)
                col.Item().ShowEntire().Column(c =>
                {
                    c.Item().Element(ctn => RenderHeader(ctn, header));
                    if (children.Count > 0)
                        c.Item().Element(ctn => RenderChild(ctn, children[0]));
                });

                // Geri kalan child'lar normal akista (serbest sayfa kirilimi)
                foreach (var ch in children.Skip(1))
                    col.Item().Element(ctn => RenderChild(ctn, ch));
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  TESLIMAT DETAYLARI — Hafta / Incoterms / Yer + cok satirli notlar
        //  IBAN bolumunden hemen once gosterilir.
        // ════════════════════════════════════════════════════════════════════
        private void BuildTeslimatSection(ColumnDescriptor col)
        {
            bool hasMeta = !string.IsNullOrWhiteSpace(TeslimatHaftasi)
                        || !string.IsNullOrWhiteSpace(TeslimatTipiKodu)
                        || !string.IsNullOrWhiteSpace(TeslimatYeri);

            // Notlari satirlara ayir; bos / sadece bosluk olan satirlari at.
            var notSatirlari = (TeslimatNotlari ?? "")
                .Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (!hasMeta && notSatirlari.Count == 0) return;

            // Bolum aliyor: PaddingTop(8) — IBAN bolumuyle ayni "alt bolum" stilinde
            col.Item().PaddingTop(8).Column(c =>
            {
                c.Spacing(2);
                c.Item().Text(t =>
                    t.Span("TESLİMAT DETAYLARI").FontSize(9.5f).Bold().FontColor(NavyDark));
                c.Item().Height(1).Background(AccentLine);
                c.Item().Height(2);

                // Meta satirlar — etiket : deger
                if (!string.IsNullOrWhiteSpace(TeslimatHaftasi))
                {
                    var hafta = TeslimatHaftasi.IndexOf("hafta", StringComparison.OrdinalIgnoreCase) >= 0
                        ? TeslimatHaftasi
                        : TeslimatHaftasi + " hafta";
                    c.Item().Text(t =>
                    {
                        t.Span("Teslimat Süresi : ").FontSize(8).FontColor(MutedText);
                        t.Span(hafta).FontSize(8).FontColor(BodyText);
                    });
                }
                if (!string.IsNullOrWhiteSpace(TeslimatTipiKodu))
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Incoterms : ").FontSize(8).FontColor(MutedText);
                        t.Span(ExpandIncoterm(TeslimatTipiKodu)).FontSize(8).FontColor(BodyText);
                    });
                }
                if (!string.IsNullOrWhiteSpace(TeslimatYeri))
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Teslimat Yeri : ").FontSize(8).FontColor(MutedText);
                        t.Span(TeslimatYeri).FontSize(8).FontColor(BodyText);
                    });
                }

                // Notlar bolumu — varsa ayrici basligi ile madde madde
                if (notSatirlari.Count > 0)
                {
                    if (hasMeta) c.Item().Height(3);
                    c.Item().Text(t =>
                        t.Span("Şartlar / Notlar:").FontSize(8).Bold().FontColor(DarkText));
                    foreach (var nt in notSatirlari)
                    {
                        // Kullanici "•" eklemis olabilir; varsa oldugu gibi tut.
                        var line = nt.StartsWith("•") || nt.StartsWith("-") || nt.StartsWith("*")
                            ? nt
                            : "• " + nt;
                        c.Item().Text(t => t.Span(line).FontSize(8).FontColor(BodyText));
                    }
                }
            });
        }

        /// <summary>Incoterm kodunu kullanici dostu metne cevirir (EXW -> "EXW — Ex Works").</summary>
        private static string ExpandIncoterm(string kod)
        {
            return kod?.Trim().ToUpperInvariant() switch
            {
                "EXW" => "EXW — Ex Works",
                "FCA" => "FCA — Free Carrier",
                "FOB" => "FOB — Free On Board",
                "CIF" => "CIF — Cost Insurance Freight",
                "CFR" => "CFR — Cost and Freight",
                "DAP" => "DAP — Delivered At Place",
                "DDP" => "DDP — Delivered Duty Paid",
                _      => kod ?? ""
            };
        }

        // ════════════════════════════════════════════════════════════════════
        //  BANKA HESAPLARI — IBAN listesi (FirmaService'ten doldurulur)
        // ════════════════════════════════════════════════════════════════════
        private void BuildBankaSection(ColumnDescriptor col)
        {
            if (IBANListesi == null || IBANListesi.Count == 0) return;

            col.Item().PaddingTop(8).Column(c =>
            {
                c.Spacing(2);
                c.Item().Text(t => t.Span("BANKA HESAPLARI / IBAN").FontSize(9.5f).Bold().FontColor(NavyDark));
                c.Item().Height(1).Background(AccentLine);
                c.Item().Height(2);
                foreach (var line in IBANListesi)
                {
                    c.Item().Text(t => t.Span(line).FontSize(8).FontColor(BodyText));
                }
            });
        }

        // ════════════════════════════════════════════════════════════════════
        //  FOOTER — koyu lacivert tam genişlik bandı
        // ════════════════════════════════════════════════════════════════════
        private void BuildFooter(QContainer container)
        {
            // Firma bilgisi varsa onu kullan, yoksa eski hard-coded metni kullan
            var footerText = !string.IsNullOrWhiteSpace(FirmaUnvan)
                ? string.Join("  |  ", new[] { FirmaUnvan, FirmaWeb, !string.IsNullOrWhiteSpace(FirmaTelefon) ? "Tel: " + FirmaTelefon : "" }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                : "Yalçın Dorse Damper San. ve Tic. Ltd. Şti.  |  www.yalcintrailer.com  |  Tel: +90 212 735 39 49";

            container.Background(FooterBg)
               .PaddingHorizontal(12, Unit.Millimetre)
               .PaddingVertical(3, Unit.Millimetre)
               .Row(row =>
               {
                   row.RelativeItem().AlignMiddle()
                      .Text(t => t.Span(footerText).FontSize(7).FontColor(LightOnDark));

                   row.ConstantItem(22, Unit.Millimetre).AlignMiddle()
                      .Text(t =>
                      {
                          t.AlignRight();
                          t.Span("Sayfa ").FontSize(7).FontColor(LightOnDark);
                          t.CurrentPageNumber().FontSize(7).FontColor(White);
                          t.Span(" / ").FontSize(7).FontColor(LightOnDark);
                          t.TotalPages().FontSize(7).FontColor(White);
                      });
               });
        }
    }
}
