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
        /// Urun fotografini ve baslik/alt yazi metinlerini rapora yukler.
        /// imageBytes null ise resim bolumu gizlenir.
        /// baslik: buyuk harf, bold, lacivert arka plan (ornek: "YALÇIN DORSE\n5 AKS LOWBED\n2. EL ÜRÜN")
        /// altYazi: kucuk italic alt aciklama (ornek: "(Fotograflar urune aittir.)")
        /// </summary>
        public void SetUrunImage(byte[]? imageBytes, string baslik, string altYazi = "")
        {
            bool hasImage = imageBytes != null && imageBytes.Length > 0
                            && !string.IsNullOrWhiteSpace(baslik);

            Parameters["pUrunBaslik"].Value  = baslik;
            Parameters["pUrunAltYazi"].Value = altYazi;

            lblUrunBaslik.Visible  = hasImage;
            picUrun.Visible        = hasImage;
            lblUrunAltYazi.Visible = hasImage && !string.IsNullOrWhiteSpace(altYazi);

            if (hasImage)
            {
                using var ms = new MemoryStream(imageBytes!);
                picUrun.Image = System.Drawing.Image.FromStream(ms);
                // Band yuksekligini resim bolumu dahil yap
                reportHeaderBand.HeightF = 645F;
            }
            else
            {
                picUrun.Image = null;
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
