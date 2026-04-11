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
        public string IlgiliKisi        { get; set; } = "";
        public string IlgiliEmail       { get; set; } = "";
        public string IlgiliMobil       { get; set; } = "";
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

        public List<SpecGroup>  SpecGroups  { get; set; } = new();
        public List<byte[]>     CizimImages { get; set; } = new();
        public List<ListItem>   ListItems   { get; set; } = new();

        // ─── İç Tipler ──────────────────────────────────────────────────────
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
                    page.MarginTop(0);
                    page.MarginBottom(0);
                    page.DefaultTextStyle(ts => ts.FontFamily("Segoe UI").FontSize(9));

                    page.Content().Column(col =>
                    {
                        col.Spacing(0);
                        BuildHeader(col);

                        // iç içerik için yatay padding
                        col.Item().PaddingHorizontal(12, Unit.Millimetre).Column(inner =>
                        {
                            inner.Spacing(0);
                            inner.Item().Height(6);
                            BuildMainInfo(inner);
                            inner.Item().Height(6);
                            BuildGreeting(inner);
                            BuildUrunSection(inner);
                            BuildSpecSection(inner);
                            BuildCizimSection(inner);
                            BuildListSection(inner);
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
                // ── SOL: İlgili Kişi Bilgileri ─────────────────────────────
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t =>
                        t.Span($"Sayın {IlgiliKisi},")
                         .Bold().FontSize(9.5f).FontColor(DarkText));
                    c.Item().Height(3);

                    if (!string.IsNullOrWhiteSpace(IlgiliEmail))
                        c.Item().Text(t =>
                        {
                            t.Span("E-mail : ").FontSize(8).FontColor(MutedText);
                            t.Span(IlgiliEmail).FontSize(8).FontColor(BodyText);
                        });
                    if (!string.IsNullOrWhiteSpace(IlgiliMobil))
                        c.Item().Text(t =>
                        {
                            t.Span("Mobil : ").FontSize(8).FontColor(MutedText);
                            t.Span(IlgiliMobil).FontSize(8).FontColor(BodyText);
                        });

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
            col.Item().Column(c =>
            {
                c.Item().Text(t =>
                    t.Span($"Sayın {IlgiliKisi},")
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
        //  ÜRÜN FOTOĞRAF BÖLÜMÜ — panelsiz, sade başlık
        // ════════════════════════════════════════════════════════════════════
        private void BuildUrunSection(ColumnDescriptor col)
        {
            bool hasImage = UrunFoto1?.Length > 0 && !string.IsNullOrWhiteSpace(UrunBaslik);
            if (!hasImage) return;

            // Ürün başlığı — sol mavi çubuk + metin
            col.Item().BorderLeft(3).BorderColor(BlueAccent)
               .PaddingLeft(3, Unit.Millimetre)
               .PaddingVertical(2, Unit.Millimetre)
               .Text(t => t.Span(UrunBaslik).Bold().FontSize(10).FontColor(NavyDark));
            col.Item().Height(3);

            // Fotoğraf(lar)
            bool hasImg2 = UrunFoto2?.Length > 0;
            if (hasImg2)
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Height(72, Unit.Millimetre).Image(UrunFoto1!).FitArea();
                    row.ConstantItem(2, Unit.Millimetre);
                    row.RelativeItem().Height(72, Unit.Millimetre).Image(UrunFoto2!).FitArea();
                });
            }
            else
            {
                col.Item().Height(72, Unit.Millimetre).Image(UrunFoto1!).FitArea();
            }

            // Alt yazı
            if (!string.IsNullOrWhiteSpace(UrunAltYazi))
            {
                col.Item().PaddingVertical(2, Unit.Millimetre)
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
                // Grup başlığı — sol mavi çubuk + metin
                col.Item().BorderLeft(3).BorderColor(BlueAccent)
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

            bool firstHeader = true;
            foreach (var item in ListItems)
            {
                if (item.IsHeader && !firstHeader)
                    col.Item().Height(4);
                if (item.IsHeader) firstHeader = false;

                string metin = item.IsHeader ? item.Metin.ToUpperInvariant() : item.Metin;
                bool   bold  = item.IsHeader || item.Bold;

                if (item.IsHeader)
                {
                    col.Item().BorderLeft(3).BorderColor(BlueAccent)
                       .Row(row =>
                       {
                           row.ConstantItem(3, Unit.Millimetre);
                           row.ConstantItem(14, Unit.Millimetre)
                              .AlignMiddle()
                              .Text(t => t.Span(item.Numara).Bold().FontSize(8.5f).FontColor(BlueAccent));
                           row.RelativeItem()
                              .PaddingVertical(2.5f)
                              .AlignMiddle()
                              .Text(t => t.Span(metin).Bold().FontSize(9).FontColor(NavyDark));
                       });
                }
                else
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(6, Unit.Millimetre);
                        row.ConstantItem(14, Unit.Millimetre)
                           .Text(t => t.Span(item.Numara).FontSize(8).FontColor(CyanTxt));
                        row.RelativeItem()
                           .PaddingVertical(1.5f)
                           .Text(t =>
                           {
                               if (bold)
                                   t.Span(metin).Bold().FontSize(8.5f).FontColor(BodyText);
                               else
                                   t.Span(metin).FontSize(8.5f).FontColor(BodyText);
                           });
                    });
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  FOOTER — koyu lacivert tam genişlik bandı
        // ════════════════════════════════════════════════════════════════════
        private void BuildFooter(QContainer container)
        {
            container.Background(FooterBg)
               .PaddingHorizontal(12, Unit.Millimetre)
               .PaddingVertical(3, Unit.Millimetre)
               .Row(row =>
               {
                   row.RelativeItem().AlignMiddle()
                      .Text(t =>
                          t.Span("Yalçın Dorse Damper San. ve Tic. Ltd. Şti.  |  " +
                                 "www.yalcintrailer.com  |  Tel: +90 212 735 39 49")
                           .FontSize(7).FontColor(LightOnDark));

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
