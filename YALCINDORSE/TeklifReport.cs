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
    }
}
