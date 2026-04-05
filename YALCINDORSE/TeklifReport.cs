using DevExpress.XtraReports.UI;
using System;
using System.IO;

namespace YALCINDORSE
{
    public partial class TeklifReport : DevExpress.XtraReports.UI.XtraReport
    {
        public TeklifReport()
        {
            InitializeComponent();
            LoadLogo();
        }

        /// <summary>
        /// Teklif verilerini rapor parametrelerine aktarir.
        /// </summary>
        public void SetQuoteData(
            string teklifNo = "", string tarih = "", string gecerlilikTarihi = "",
            string musteriAdi = "", string musteriKodu = "",
            string ilgiliKisi = "", string ilgiliEmail = "", string ilgiliMobil = "",
            string saticiAdi = "", string saticiEmail = "", string saticiTelefon = "",
            string netTutar = "", string paraBirimi = "", string urunAdi = "",
            string sasiNo = "", string modelYili = "")
        {
            Parameters["pTeklifNo"].Value = teklifNo;
            Parameters["pTarih"].Value = tarih;
            Parameters["pGecerlilikTarihi"].Value = gecerlilikTarihi;
            Parameters["pMusteriAdi"].Value = musteriAdi;
            Parameters["pMusteriKodu"].Value = musteriKodu;
            Parameters["pIlgiliKisi"].Value = ilgiliKisi;
            Parameters["pIlgiliEmail"].Value = ilgiliEmail;
            Parameters["pIlgiliMobil"].Value = ilgiliMobil;
            Parameters["pSaticiAdi"].Value = saticiAdi;
            Parameters["pSaticiEmail"].Value = saticiEmail;
            Parameters["pSaticiTelefon"].Value = saticiTelefon;
            Parameters["pNetTutar"].Value = netTutar;
            Parameters["pParaBirimi"].Value = paraBirimi;
            Parameters["pUrunAdi"].Value = urunAdi;
            Parameters["pSasiNo"].Value = sasiNo;
            Parameters["pModelYili"].Value = modelYili;
        }

        /// <summary>
        /// PDF byte[] olarak export eder.
        /// </summary>
        public byte[] ExportToPdfBytes()
        {
            using var ms = new MemoryStream();
            ExportToPdf(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// Urun fotograflarini ve baslik/alt yazi metinlerini rapora yukler.
        /// Tek resim: tam genislikte gosterilir (727px).
        /// Iki resim: yan yana gosterilir (her biri ~360px).
        /// imageBytes null/bos ise resim bolumu tamamen gizlenir.
        /// </summary>
        public void SetUrunImage(byte[]? imageBytes, string baslik, string altYazi = "",
                                 byte[]? imageBytes2 = null)
        {
            bool hasImage  = imageBytes  != null && imageBytes.Length  > 0
                             && !string.IsNullOrWhiteSpace(baslik);
            bool hasImage2 = imageBytes2 != null && imageBytes2.Length > 0;

            Parameters["pUrunBaslik"].Value  = baslik;
            Parameters["pUrunAltYazi"].Value = altYazi;

            lblUrunBaslik.Visible  = hasImage;
            picUrun.Visible        = hasImage;
            picUrun2.Visible       = hasImage && hasImage2;
            lblUrunAltYazi.Visible = hasImage && !string.IsNullOrWhiteSpace(altYazi);

            if (hasImage)
            {
                if (hasImage2)
                {
                    // Yan yana: her resim ~360px genislik, aralarinda 7px bosluk
                    picUrun.LocationFloat  = new DevExpress.Utils.PointFloat(0F,    379F);
                    picUrun.SizeF          = new System.Drawing.SizeF(360F, 260F);
                    picUrun2.LocationFloat = new DevExpress.Utils.PointFloat(367F,  379F);
                    picUrun2.SizeF         = new System.Drawing.SizeF(360F, 260F);

                    using var ms1 = new MemoryStream(imageBytes!);
                    picUrun.Image = System.Drawing.Image.FromStream(ms1);
                    using var ms2 = new MemoryStream(imageBytes2!);
                    picUrun2.Image = System.Drawing.Image.FromStream(ms2);
                }
                else
                {
                    // Tek resim: tam genislik
                    picUrun.LocationFloat = new DevExpress.Utils.PointFloat(0F, 379F);
                    picUrun.SizeF         = new System.Drawing.SizeF(727F, 260F);
                    picUrun2.Image        = null;

                    using var ms = new MemoryStream(imageBytes!);
                    picUrun.Image = System.Drawing.Image.FromStream(ms);
                }

                reportHeaderBand.HeightF = 667F;
            }
            else
            {
                picUrun.Image  = null;
                picUrun2.Image = null;
                // Resim yoksa header daha kisa olsun
                reportHeaderBand.HeightF = 337F;
            }
        }

        /// <summary>
        /// Urun fotograflarinin altina iki-kolon SPEC tablolarini ve teknik resimleri ekler.
        /// specGroups bos / null ise ve cizimImages bos ise detailBand height=0 olur.
        /// </summary>
        public void SetSpecData(List<SpecGroup>? specGroups, List<byte[]>? cizimImages)
        {
            detailBand.Controls.Clear();

            bool hasSpec  = specGroups?.Any(g => g.Rows.Count > 0) == true;
            bool hasCizim = cizimImages?.Any(b => b?.Length > 0) == true;

            if (!hasSpec && !hasCizim)
            {
                detailBand.HeightF = 0F;
                return;
            }

            float y            = 6F;
            const float W      = 727F;
            const float ROW_H  = 17F;
            const float HDR_H  = 20F;
            const float LEFT_W = 350F;
            const float COL_W  = 27F;
            const float RIGHT_W = W - LEFT_W - COL_W;  // 350F

            var navyBg  = System.Drawing.Color.FromArgb(16, 42, 85);
            var altBg   = System.Drawing.Color.FromArgb(248, 249, 251);
            var whiteBg = System.Drawing.Color.White;
            var bordClr = System.Drawing.Color.FromArgb(210, 215, 220);

            // ── SPEC TABLOLARI ────────────────────────────────────────────
            if (hasSpec)
            {
                foreach (var grp in specGroups!)
                {
                    if (grp.Rows.Count == 0) continue;

                    // Grup baslik satiri (lacivert arka plan, beyaz bold)
                    var hdrLbl = new DevExpress.XtraReports.UI.XRLabel
                    {
                        BackColor     = navyBg,
                        ForeColor     = System.Drawing.Color.White,
                        Font          = new DevExpress.Drawing.DXFont("Segoe UI", 9F, DevExpress.Drawing.DXFontStyle.Bold),
                        Text          = grp.GrupAdi.ToUpperInvariant(),
                        LocationFloat = new DevExpress.Utils.PointFloat(0F, y),
                        SizeF         = new System.Drawing.SizeF(W, HDR_H),
                        Padding       = new DevExpress.XtraPrinting.PaddingInfo(8, 8, 4, 4, 100F),
                        TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft
                    };
                    hdrLbl.StylePriority.UseBackColor = true;
                    hdrLbl.StylePriority.UseForeColor = true;
                    detailBand.Controls.Add(hdrLbl);
                    y += HDR_H;

                    bool alt = false;
                    foreach (var (ozellik, deger) in grp.Rows)
                    {
                        var bg = alt ? altBg : whiteBg;

                        // Sol sutun: ozellik adi
                        var lblLeft = new DevExpress.XtraReports.UI.XRLabel
                        {
                            BackColor     = bg,
                            Text          = ozellik,
                            Font          = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F),
                            LocationFloat = new DevExpress.Utils.PointFloat(0F, y),
                            SizeF         = new System.Drawing.SizeF(LEFT_W, ROW_H),
                            Padding       = new DevExpress.XtraPrinting.PaddingInfo(8, 4, 2, 2, 100F),
                            TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft,
                            Borders       = DevExpress.XtraPrinting.BorderSide.Bottom | DevExpress.XtraPrinting.BorderSide.Right,
                            BorderColor   = bordClr
                        };
                        lblLeft.StylePriority.UseBackColor = lblLeft.StylePriority.UseBorders = lblLeft.StylePriority.UseBorderColor = true;
                        detailBand.Controls.Add(lblLeft);

                        // Iki nokta sutunu
                        var lblColon = new DevExpress.XtraReports.UI.XRLabel
                        {
                            BackColor     = bg,
                            Text          = ":",
                            Font          = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F),
                            LocationFloat = new DevExpress.Utils.PointFloat(LEFT_W, y),
                            SizeF         = new System.Drawing.SizeF(COL_W, ROW_H),
                            TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                            Borders       = DevExpress.XtraPrinting.BorderSide.Bottom,
                            BorderColor   = bordClr
                        };
                        lblColon.StylePriority.UseBackColor = lblColon.StylePriority.UseBorders = lblColon.StylePriority.UseBorderColor = true;
                        detailBand.Controls.Add(lblColon);

                        // Sag sutun: deger
                        var lblVal = new DevExpress.XtraReports.UI.XRLabel
                        {
                            BackColor     = bg,
                            Text          = deger,
                            Font          = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F),
                            LocationFloat = new DevExpress.Utils.PointFloat(LEFT_W + COL_W, y),
                            SizeF         = new System.Drawing.SizeF(RIGHT_W, ROW_H),
                            Padding       = new DevExpress.XtraPrinting.PaddingInfo(8, 4, 2, 2, 100F),
                            TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft,
                            Borders       = DevExpress.XtraPrinting.BorderSide.Bottom,
                            BorderColor   = bordClr
                        };
                        lblVal.StylePriority.UseBackColor = lblVal.StylePriority.UseBorders = lblVal.StylePriority.UseBorderColor = true;
                        detailBand.Controls.Add(lblVal);

                        y += ROW_H;
                        alt = !alt;
                    }

                    y += 5F; // gruplar arasi bosluk
                }

                // Dipnot: TC Karayollari notu (spec tablolarin hemen altina)
                y += 3F;
                var noteLbl = new DevExpress.XtraReports.UI.XRLabel
                {
                    Text          = "**  TC. Karayolları' nın müsaade ettiği ağırlıklardır, çalışacağı ülke kurallarına göre farklılık gösterebilir.",
                    Font          = new DevExpress.Drawing.DXFont("Segoe UI", 7.5F,
                                        DevExpress.Drawing.DXFontStyle.Bold | DevExpress.Drawing.DXFontStyle.Italic),
                    ForeColor     = System.Drawing.Color.FromArgb(60, 60, 60),
                    LocationFloat = new DevExpress.Utils.PointFloat(0F, y),
                    SizeF         = new System.Drawing.SizeF(W, 15F),
                    Padding       = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 1, 1, 100F),
                    TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft
                };
                noteLbl.StylePriority.UseForeColor = true;
                detailBand.Controls.Add(noteLbl);
                y += 15F;
            }

            // ── TEKNIK RESIMLER ───────────────────────────────────────────
            if (hasCizim)
            {
                y += 8F;
                foreach (var imgBytes in cizimImages!)
                {
                    if (imgBytes == null || imgBytes.Length == 0) continue;

                    // Orijinal en/boy oranina gore yukseklik hesapla (max 480F)
                    float picH = 360F;
                    try
                    {
                        using var tmp = System.Drawing.Image.FromStream(new MemoryStream(imgBytes));
                        picH = Math.Min(W * ((float)tmp.Height / tmp.Width), 480F);
                    }
                    catch { }

                    var pic = new DevExpress.XtraReports.UI.XRPictureBox
                    {
                        LocationFloat = new DevExpress.Utils.PointFloat(0F, y),
                        SizeF         = new System.Drawing.SizeF(W, picH),
                        Sizing        = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage
                    };
                    using var ms = new MemoryStream(imgBytes);
                    pic.Image = System.Drawing.Image.FromStream(ms);
                    detailBand.Controls.Add(pic);

                    y += picH + 8F;
                }
            }

            detailBand.HeightF = y + 8F;
        }

        private void LoadLogo()
        {
            try
            {
                var logoPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "logo.png");

                if (File.Exists(logoPath))
                {
                    picLogo.ImageUrl = logoPath;
                }
            }
            catch { /* Logo bulunamazsa bos kalir */ }
        }

        /// <summary>PDF'de iki-kolon tablo olarak gosterilecek ozellik grubu.</summary>
        public class SpecGroup
        {
            public string GrupAdi { get; set; } = "";
            public List<(string Ozellik, string Deger)> Rows { get; set; } = new();
        }
    }
}
