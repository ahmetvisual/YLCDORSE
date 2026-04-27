using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YALCINDORSE.Services;
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
        const string BlueAccent  = "#1D4ED8";
        const string AccentLine  = "#38BDF8";
        const string NearWhite   = "#F8FAFC";
        const string DarkText    = "#0F172A";
        const string BodyText    = "#334155";
        const string MutedText   = "#64748B";
        const string BorderClr   = "#CBD5E1";
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
        /// <summary>Teklif dili — "TR" / "EN" / "DE" / "FR" / "RU". OM formu icin TR/EN bolumlenir.</summary>
        public string Dil               { get; set; } = "TR";

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
            public List<(string Ozellik, string Deger)> Rows { get; set; } = new();
        }

        public class ListItem
        {
            public string Metin    { get; set; } = "";
            public string Numara   { get; set; } = "";
            public bool   IsHeader { get; set; }
            public bool   Bold     { get; set; }

            public ListItem() { }
            public ListItem(string numara, string metin, bool isHeader, bool bold)
            {
                Numara   = numara;
                Metin    = metin;
                IsHeader = isHeader;
                Bold     = bold;
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
                            BuildBankaSection(inner);
                            BuildOmSection(inner);   // Faz 3: barandrive OM formu (TR/EN)
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
            col.Item().Background(White)
               .PaddingHorizontal(12, Unit.Millimetre)
               .PaddingVertical(6, Unit.Millimetre)
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

                   // Şirket adı + iletişim
                   row.RelativeItem().AlignMiddle().Column(c =>
                   {
                       c.Item().Text(t =>
                           t.Span("YALÇIN DORSE").Bold().FontSize(16).FontColor(NavyDark));
                       c.Item().Height(2);
                       c.Item().Text(t =>
                           t.Span("Fevzipaşa Mah. Erdoğan Sk. N:14 Silivri / İSTANBUL")
                            .FontSize(7.5f).FontColor(MutedText));
                       c.Item().Text(t =>
                           t.Span("Tel: +90 212 735 39 49  |  www.yalcintrailer.com")
                            .FontSize(7.5f).FontColor(MutedText));
                   });

                   row.ConstantItem(5, Unit.Millimetre);

                   // TEKLİF rozeti
                   row.ConstantItem(28, Unit.Millimetre)
                      .AlignMiddle()
                      .AlignRight()
                      .Column(c =>
                      {
                          c.Item().Text(t =>
                          {
                              t.AlignRight();
                              t.Span("TEKLİF").Bold().FontSize(20).FontColor(BlueAccent);
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

            // İnce accent çizgisi
            col.Item().Height(2).Background(AccentLine);
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
                row.RelativeItem().Column(c =>
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

                    if (kisiler.Count == 0 && !hasAracBilgi) return;

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

                // ── SAĞ: Teklif meta + SDU + Müşteri ──────────────────────
                row.RelativeItem().Column(c =>
                {
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

                    c.Item().Height(4);

                    // Satış Destek Uzmanı
                    c.Item().Text(t =>
                    {
                        t.AlignRight();
                        t.Span("Satış Destek Uzmanı : ").Bold().FontSize(8).FontColor(DarkText);
                        t.Span(SaticiAdi).Bold().FontSize(8).FontColor(DarkText);
                    });

                    // SDU iletişim — tek satır: email | telefon
                    var sduParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(SaticiEmail)) sduParts.Add(SaticiEmail);
                    if (!string.IsNullOrWhiteSpace(SaticiTelefon)) sduParts.Add(SaticiTelefon);
                    if (sduParts.Count > 0)
                    {
                        c.Item().Text(t =>
                        {
                            t.AlignRight();
                            t.Span(string.Join("  |  ", sduParts))
                             .FontSize(7.5f).FontColor(MutedText);
                        });
                    }

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

            col.Item().Height(5);
            col.Item().Height(0.5f).Background(BorderClr);
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

            // Urun basligi — diger bolum basliklariyla hizali (12mm padding) + alt cizgi
            col.Item().PaddingHorizontal(12, Unit.Millimetre)
               .BorderLeft(3)
               .BorderBottom(0.75f)
               .BorderColor(BlueAccent)
               .PaddingLeft(3, Unit.Millimetre)
               .PaddingVertical(2, Unit.Millimetre)
               .Text(t => t.Span(UrunBaslik).Bold().FontSize(10).FontColor(NavyDark));
            col.Item().Height(3);

            // Fotograf(lar)
            bool hasImg2 = UrunFoto2?.Length > 0;
            if (hasImg2)
            {
                // Cift foto: ic icerik alaninda (12mm padding) yan yana
                col.Item().PaddingHorizontal(12, Unit.Millimetre).Row(row =>
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
                col.Item().PaddingHorizontal(15, Unit.Millimetre)
                   .Image(UrunFoto1!).FitWidth();
            }

            // Alt yazi — basliklarla hizali olmasi icin 12mm yatay padding
            if (!string.IsNullOrWhiteSpace(UrunAltYazi))
            {
                col.Item().PaddingHorizontal(12, Unit.Millimetre)
                   .PaddingVertical(2, Unit.Millimetre)
                   .Text(t =>
                   {
                       t.AlignCenter();
                       t.Span(UrunAltYazi).Italic().FontSize(7.5f).FontColor(MutedText);
                   });
            }

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
                // Grup basligi — sol mavi cubuk + metin + alt cizgi (BorderBottom)
                col.Item()
                   .BorderLeft(3)
                   .BorderBottom(0.75f)
                   .BorderColor(BlueAccent)
                   .PaddingLeft(3, Unit.Millimetre)
                   .PaddingVertical(2.5f)
                   .Text(t =>
                       t.Span(grp.GrupAdi.ToUpperInvariant())
                        .Bold().FontSize(9).FontColor(NavyDark));

                bool alt = false;
                foreach (var (ozellik, deger) in grp.Rows)
                {
                    string bg = alt ? AltRowBg : White;
                    col.Item().Background(bg).Row(row =>
                    {
                        row.RelativeItem()
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(2).PaddingLeft(10).PaddingRight(4)
                           .Text(t => t.Span(ozellik).FontSize(8.5f).FontColor(BodyText));

                        row.ConstantItem(0.5f).Background(BorderClr);

                        row.ConstantItem(7, Unit.Millimetre)
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .AlignCenter()
                           .Text(t => t.Span(":").FontSize(9).FontColor(MutedText));

                        row.ConstantItem(0.5f).Background(BorderClr);

                        row.RelativeItem()
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(2).PaddingLeft(6).PaddingRight(4)
                           .Text(t => t.Span(deger).Bold().FontSize(8.5f).FontColor(DarkText));
                    });
                    alt = !alt;
                }

                // Grup altı ince çizgi
                col.Item().Height(1).Background(BlueAccent);
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
                c.BorderLeft(3)
                 .BorderBottom(0.75f)
                 .BorderColor(BlueAccent)
                 .Row(row =>
                 {
                     row.ConstantItem(3, Unit.Millimetre);
                     row.ConstantItem(14, Unit.Millimetre)
                        .AlignMiddle()
                        .Text(t => t.Span(h.Numara).Bold().FontSize(8.5f).FontColor(BlueAccent));
                     row.RelativeItem()
                        .PaddingVertical(2.5f)
                        .AlignMiddle()
                        .Text(t => t.Span(h.Metin.ToUpperInvariant()).Bold().FontSize(9).FontColor(NavyDark));
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
                           if (li.Bold) t.Span(li.Metin).Bold().FontSize(8.5f).FontColor(BodyText);
                           else        t.Span(li.Metin).FontSize(8.5f).FontColor(BodyText);
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
        //  OM FORMU (Faz 3) — barandrive/OM_TR.xlsx & OM_EN.xlsx kaynakli
        //  Teklif sonuna eklenen tam fiyat listesi + musteri bilgi formu.
        //  Yeni sayfada baslar; quote.Dil = "EN" -> EN, digerleri -> TR.
        // ════════════════════════════════════════════════════════════════════
        private void BuildOmSection(ColumnDescriptor col)
        {
            var rows = OmFormVerileri.ForDil(Dil);
            if (rows == null || rows.Count == 0) return;

            bool isEn = string.Equals(Dil, "EN", StringComparison.OrdinalIgnoreCase);

            // Yeni sayfada basla — banka section'in altinda devam etmesin.
            col.Item().PageBreak();

            // Baslik
            col.Item().PaddingTop(2)
               .BorderLeft(3).BorderBottom(0.75f).BorderColor(BlueAccent)
               .PaddingLeft(3, Unit.Millimetre).PaddingVertical(2.5f)
               .Text(t => t.Span(isEn ? "OFFER FORM / PRICE LIST" : "TEKLİF FORMU / FİYAT LİSTESİ")
                           .Bold().FontSize(10).FontColor(NavyDark));
            col.Item().Height(4);

            // Musteri bilgi formu (auto-fill)
            BuildOmCustomerInfo(col, isEn);
            col.Item().Height(4);

            // Fiyat tablosu basligi
            col.Item().Background(NavyDark).Padding(2).Row(row =>
            {
                row.ConstantItem(18, Unit.Millimetre).AlignMiddle()
                   .Text(t => t.Span(isEn ? "CODE" : "KOD").Bold().FontSize(8).FontColor(White));
                row.RelativeItem().AlignMiddle()
                   .Text(t => t.Span(isEn ? "DESCRIPTION" : "AÇIKLAMA").Bold().FontSize(8).FontColor(White));
                row.ConstantItem(28, Unit.Millimetre).AlignMiddle().AlignRight()
                   .Text(t => t.Span(isEn ? "UNIT PRICE" : "BİRİM FİYAT").Bold().FontSize(8).FontColor(White));
            });

            bool altRow = false;
            foreach (var r in rows)
            {
                if (r.Tip == "SECTION")
                {
                    altRow = false;
                    col.Item().Height(2);
                    col.Item().BorderLeft(2).BorderColor(BlueAccent)
                       .PaddingLeft(3, Unit.Millimetre).PaddingVertical(2)
                       .Text(t => t.Span(r.Aciklama).Bold().FontSize(9).FontColor(NavyDark));
                }
                else // ITEM
                {
                    string bg = altRow ? AltRowBg : White;
                    col.Item().Background(bg).Row(row =>
                    {
                        row.ConstantItem(18, Unit.Millimetre)
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(1.5f).PaddingHorizontal(2)
                           .Text(t => t.Span(r.Kod).Bold().FontSize(7.5f).FontColor(BlueAccent));

                        row.RelativeItem()
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(1.5f).PaddingHorizontal(2)
                           .Column(c =>
                           {
                               c.Item().Text(t => t.Span(r.Aciklama).FontSize(7.5f).FontColor(DarkText));
                               if (!string.IsNullOrWhiteSpace(r.Detay))
                                   c.Item().Text(t => t.Span(r.Detay).FontSize(6.5f).FontColor(MutedText));
                           });

                        row.ConstantItem(28, Unit.Millimetre)
                           .BorderBottom(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(1.5f).PaddingHorizontal(2)
                           .AlignRight().AlignMiddle()
                           .Text(t => t.Span(FormatOmPrice(r.BirimFiyat)).Bold().FontSize(7.5f).FontColor(DarkText));
                    });
                    altRow = !altRow;
                }
            }

            // Dipnot
            col.Item().Height(3);
            col.Item().BorderLeft(2).BorderColor(MutedText)
               .PaddingLeft(3, Unit.Millimetre)
               .Text(t => t.Span(isEn
                    ? "* Prices are reference list prices in EUR. Final prices subject to confirmation."
                    : "* Fiyatlar referans liste fiyatlarıdır (EUR). Nihai fiyatlar onaya tabidir.")
                    .Italic().FontSize(7).FontColor(MutedText));
        }

        /// <summary>OM ust kismi: musteri bilgi formu (FİRMA İSMİ / COMPANY NAME vb) auto-fill.</summary>
        private void BuildOmCustomerInfo(ColumnDescriptor col, bool isEn)
        {
            var labels = isEn
                ? new[] { ("COMPANY NAME", MusteriAdi),
                          ("AUTHORIZED PERSON", IlgiliKisiler.FirstOrDefault()?.Ad ?? IlgiliKisi),
                          ("E-MAIL", IlgiliKisiler.FirstOrDefault()?.Email ?? IlgiliEmail),
                          ("MOBILE", IlgiliKisiler.FirstOrDefault()?.Mobil ?? IlgiliMobil),
                          ("DATE", Tarih),
                          ("OFFER NO", TeklifNo) }
                : new[] { ("FİRMA İSMİ", MusteriAdi),
                          ("FİRMA YETKİLİSİ", IlgiliKisiler.FirstOrDefault()?.Ad ?? IlgiliKisi),
                          ("E-MAİL", IlgiliKisiler.FirstOrDefault()?.Email ?? IlgiliEmail),
                          ("MOBİL", IlgiliKisiler.FirstOrDefault()?.Mobil ?? IlgiliMobil),
                          ("TARİH", Tarih),
                          ("TEKLİF NO", TeklifNo) };

            col.Item().Border(0.5f).BorderColor(BorderClr).Column(c =>
            {
                bool altRow = false;
                foreach (var (label, value) in labels)
                {
                    string bg = altRow ? AltRowBg : White;
                    c.Item().Background(bg).Row(row =>
                    {
                        row.ConstantItem(40, Unit.Millimetre)
                           .BorderRight(0.5f).BorderColor(BorderClr)
                           .PaddingVertical(2).PaddingHorizontal(3)
                           .Text(t => t.Span(label).Bold().FontSize(8).FontColor(NavyDark));
                        row.RelativeItem()
                           .PaddingVertical(2).PaddingHorizontal(3)
                           .Text(t => t.Span(value ?? "").FontSize(8).FontColor(BodyText));
                    });
                    altRow = !altRow;
                }
            });
        }

        private static string FormatOmPrice(decimal? p)
        {
            if (!p.HasValue) return "";
            return p.Value.ToString("N0") + " EUR";
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
