namespace YALCINDORSE
{
    partial class TeklifReport
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            // ── Parameters ──
            this.pTeklifNo = new DevExpress.XtraReports.Parameters.Parameter();
            this.pTarih = new DevExpress.XtraReports.Parameters.Parameter();
            this.pGecerlilikTarihi = new DevExpress.XtraReports.Parameters.Parameter();
            this.pMusteriAdi = new DevExpress.XtraReports.Parameters.Parameter();
            this.pMusteriKodu = new DevExpress.XtraReports.Parameters.Parameter();
            this.pIlgiliKisi = new DevExpress.XtraReports.Parameters.Parameter();
            this.pIlgiliEmail = new DevExpress.XtraReports.Parameters.Parameter();
            this.pIlgiliMobil = new DevExpress.XtraReports.Parameters.Parameter();
            this.pSaticiAdi = new DevExpress.XtraReports.Parameters.Parameter();
            this.pSaticiEmail = new DevExpress.XtraReports.Parameters.Parameter();
            this.pNetTutar = new DevExpress.XtraReports.Parameters.Parameter();
            this.pParaBirimi = new DevExpress.XtraReports.Parameters.Parameter();
            this.pUrunAdi = new DevExpress.XtraReports.Parameters.Parameter();

            // ── Bands ──
            this.topMarginBand = new DevExpress.XtraReports.UI.TopMarginBand();
            this.reportHeaderBand = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.detailBand = new DevExpress.XtraReports.UI.DetailBand();
            this.bottomMarginBand = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.pageFooterBand = new DevExpress.XtraReports.UI.PageFooterBand();

            // ── Controls ──
            this.picLogo = new DevExpress.XtraReports.UI.XRPictureBox();
            this.lblCompanyName = new DevExpress.XtraReports.UI.XRLabel();
            this.lblCompanyInfo = new DevExpress.XtraReports.UI.XRLabel();
            this.lineHeader = new DevExpress.XtraReports.UI.XRLine();
            this.lblTeklifTitle = new DevExpress.XtraReports.UI.XRLabel();
            this.lblTeklifNo = new DevExpress.XtraReports.UI.XRLabel();
            this.lblTarih = new DevExpress.XtraReports.UI.XRLabel();
            this.lblGecerlilik = new DevExpress.XtraReports.UI.XRLabel();
            this.lblSayin = new DevExpress.XtraReports.UI.XRLabel();
            this.lblIlgiliEmail = new DevExpress.XtraReports.UI.XRLabel();
            this.lblIlgiliMobil = new DevExpress.XtraReports.UI.XRLabel();
            this.lblMusteriAdi = new DevExpress.XtraReports.UI.XRLabel();
            this.lblMusteriKodu = new DevExpress.XtraReports.UI.XRLabel();
            this.lineContent = new DevExpress.XtraReports.UI.XRLine();
            this.lineFooter = new DevExpress.XtraReports.UI.XRLine();
            this.lblFooter = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPageInfo = new DevExpress.XtraReports.UI.XRPageInfo();

            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            // ═══════════════════════════════════════════
            //  PARAMETERS
            // ═══════════════════════════════════════════
            this.pTeklifNo.Name = "pTeklifNo";
            this.pTeklifNo.Type = typeof(string);
            this.pTeklifNo.ValueInfo = "";
            this.pTeklifNo.Visible = false;

            this.pTarih.Name = "pTarih";
            this.pTarih.Type = typeof(string);
            this.pTarih.ValueInfo = "";
            this.pTarih.Visible = false;

            this.pGecerlilikTarihi.Name = "pGecerlilikTarihi";
            this.pGecerlilikTarihi.Type = typeof(string);
            this.pGecerlilikTarihi.ValueInfo = "";
            this.pGecerlilikTarihi.Visible = false;

            this.pMusteriAdi.Name = "pMusteriAdi";
            this.pMusteriAdi.Type = typeof(string);
            this.pMusteriAdi.ValueInfo = "";
            this.pMusteriAdi.Visible = false;

            this.pMusteriKodu.Name = "pMusteriKodu";
            this.pMusteriKodu.Type = typeof(string);
            this.pMusteriKodu.ValueInfo = "";
            this.pMusteriKodu.Visible = false;

            this.pIlgiliKisi.Name = "pIlgiliKisi";
            this.pIlgiliKisi.Type = typeof(string);
            this.pIlgiliKisi.ValueInfo = "";
            this.pIlgiliKisi.Visible = false;

            this.pIlgiliEmail.Name = "pIlgiliEmail";
            this.pIlgiliEmail.Type = typeof(string);
            this.pIlgiliEmail.ValueInfo = "";
            this.pIlgiliEmail.Visible = false;

            this.pIlgiliMobil.Name = "pIlgiliMobil";
            this.pIlgiliMobil.Type = typeof(string);
            this.pIlgiliMobil.ValueInfo = "";
            this.pIlgiliMobil.Visible = false;

            this.pSaticiAdi.Name = "pSaticiAdi";
            this.pSaticiAdi.Type = typeof(string);
            this.pSaticiAdi.ValueInfo = "";
            this.pSaticiAdi.Visible = false;

            this.pSaticiEmail.Name = "pSaticiEmail";
            this.pSaticiEmail.Type = typeof(string);
            this.pSaticiEmail.ValueInfo = "";
            this.pSaticiEmail.Visible = false;

            this.pNetTutar.Name = "pNetTutar";
            this.pNetTutar.Type = typeof(string);
            this.pNetTutar.ValueInfo = "";
            this.pNetTutar.Visible = false;

            this.pParaBirimi.Name = "pParaBirimi";
            this.pParaBirimi.Type = typeof(string);
            this.pParaBirimi.ValueInfo = "";
            this.pParaBirimi.Visible = false;

            this.pUrunAdi.Name = "pUrunAdi";
            this.pUrunAdi.Type = typeof(string);
            this.pUrunAdi.ValueInfo = "";
            this.pUrunAdi.Visible = false;

            // ═══════════════════════════════════════════
            //  REPORT HEADER BAND — Logo, Company, Contact
            // ═══════════════════════════════════════════

            // ── Logo (sol ust) ──
            this.picLogo.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.picLogo.SizeF = new System.Drawing.SizeF(170F, 60F);
            this.picLogo.Sizing = DevExpress.XtraPrinting.ImageSizeMode.ZoomImage;
            this.picLogo.Name = "picLogo";

            // ── Firma Adi (sag ust) ──
            this.lblCompanyName.LocationFloat = new DevExpress.Utils.PointFloat(470F, 5F);
            this.lblCompanyName.SizeF = new System.Drawing.SizeF(257F, 22F);
            this.lblCompanyName.Font = new DevExpress.Drawing.DXFont("Segoe UI", 12F, DevExpress.Drawing.DXFontStyle.Bold);
            this.lblCompanyName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(95)))), ((int)(((byte)(165)))));
            this.lblCompanyName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblCompanyName.Text = "YAL\u00C7IN DORSE";
            this.lblCompanyName.Name = "lblCompanyName";

            // ── Firma Detay (sag ust, adresin altinda) ──
            this.lblCompanyInfo.LocationFloat = new DevExpress.Utils.PointFloat(400F, 27F);
            this.lblCompanyInfo.SizeF = new System.Drawing.SizeF(327F, 38F);
            this.lblCompanyInfo.Font = new DevExpress.Drawing.DXFont("Segoe UI", 7F);
            this.lblCompanyInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblCompanyInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblCompanyInfo.Text = "Org. San. B\u00F6l. 3.Cad. No:7 Honaz / DEN\u0130ZL\u0130\r\nTel: +90 (258) 812 18 88 | info@yalcindorse.com";
            this.lblCompanyInfo.Multiline = true;
            this.lblCompanyInfo.Name = "lblCompanyInfo";

            // ── Ust cizgi ──
            this.lineHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 70F);
            this.lineHeader.SizeF = new System.Drawing.SizeF(727F, 2F);
            this.lineHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(95)))), ((int)(((byte)(165)))));
            this.lineHeader.LineWidth = 2;
            this.lineHeader.Name = "lineHeader";

            // ── TEKLIF Basligi ──
            this.lblTeklifTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 82F);
            this.lblTeklifTitle.SizeF = new System.Drawing.SizeF(200F, 28F);
            this.lblTeklifTitle.Font = new DevExpress.Drawing.DXFont("Segoe UI", 16F, DevExpress.Drawing.DXFontStyle.Bold);
            this.lblTeklifTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(95)))), ((int)(((byte)(165)))));
            this.lblTeklifTitle.Text = "TEKL\u0130F";
            this.lblTeklifTitle.Name = "lblTeklifTitle";

            // ── Teklif No (sag taraf) ──
            this.lblTeklifNo.LocationFloat = new DevExpress.Utils.PointFloat(450F, 82F);
            this.lblTeklifNo.SizeF = new System.Drawing.SizeF(277F, 16F);
            this.lblTeklifNo.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F);
            this.lblTeklifNo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblTeklifNo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblTeklifNo.Name = "lblTeklifNo";
            this.lblTeklifNo.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'Teklif No : ' + ?pTeklifNo")
            });

            // ── Tarih ──
            this.lblTarih.LocationFloat = new DevExpress.Utils.PointFloat(450F, 98F);
            this.lblTarih.SizeF = new System.Drawing.SizeF(277F, 16F);
            this.lblTarih.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F);
            this.lblTarih.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblTarih.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblTarih.Name = "lblTarih";
            this.lblTarih.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'Tarih : ' + ?pTarih")
            });

            // ── Gecerlilik ──
            this.lblGecerlilik.LocationFloat = new DevExpress.Utils.PointFloat(450F, 114F);
            this.lblGecerlilik.SizeF = new System.Drawing.SizeF(277F, 16F);
            this.lblGecerlilik.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F);
            this.lblGecerlilik.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblGecerlilik.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblGecerlilik.Name = "lblGecerlilik";
            this.lblGecerlilik.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'Ge\u00E7erlilik : ' + ?pGecerlilikTarihi")
            });

            // ── Sayin (ilgili kisi) ──
            this.lblSayin.LocationFloat = new DevExpress.Utils.PointFloat(0F, 140F);
            this.lblSayin.SizeF = new System.Drawing.SizeF(420F, 20F);
            this.lblSayin.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9.5F, DevExpress.Drawing.DXFontStyle.Bold);
            this.lblSayin.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.lblSayin.Name = "lblSayin";
            this.lblSayin.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'Say\u0131n ' + ?pIlgiliKisi + ','")
            });

            // ── E-mail ──
            this.lblIlgiliEmail.LocationFloat = new DevExpress.Utils.PointFloat(0F, 160F);
            this.lblIlgiliEmail.SizeF = new System.Drawing.SizeF(380F, 16F);
            this.lblIlgiliEmail.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F);
            this.lblIlgiliEmail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblIlgiliEmail.Name = "lblIlgiliEmail";
            this.lblIlgiliEmail.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'E-mail : ' + ?pIlgiliEmail")
            });

            // ── Mobil ──
            this.lblIlgiliMobil.LocationFloat = new DevExpress.Utils.PointFloat(0F, 176F);
            this.lblIlgiliMobil.SizeF = new System.Drawing.SizeF(380F, 16F);
            this.lblIlgiliMobil.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F);
            this.lblIlgiliMobil.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(65)))), ((int)(((byte)(81)))));
            this.lblIlgiliMobil.Name = "lblIlgiliMobil";
            this.lblIlgiliMobil.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'Mobil : ' + ?pIlgiliMobil")
            });

            // ── Musteri Adi (sag) ──
            this.lblMusteriAdi.LocationFloat = new DevExpress.Utils.PointFloat(450F, 140F);
            this.lblMusteriAdi.SizeF = new System.Drawing.SizeF(277F, 20F);
            this.lblMusteriAdi.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9.5F, DevExpress.Drawing.DXFontStyle.Bold);
            this.lblMusteriAdi.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.lblMusteriAdi.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblMusteriAdi.Name = "lblMusteriAdi";
            this.lblMusteriAdi.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "?pMusteriAdi")
            });

            // ── Musteri Kodu (sag) ──
            this.lblMusteriKodu.LocationFloat = new DevExpress.Utils.PointFloat(450F, 160F);
            this.lblMusteriKodu.SizeF = new System.Drawing.SizeF(277F, 16F);
            this.lblMusteriKodu.Font = new DevExpress.Drawing.DXFont("Segoe UI", 8.5F);
            this.lblMusteriKodu.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(107)))), ((int)(((byte)(114)))), ((int)(((byte)(128)))));
            this.lblMusteriKodu.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblMusteriKodu.Name = "lblMusteriKodu";
            this.lblMusteriKodu.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "'M\u00FC\u015Fteri Kodu : ' + ?pMusteriKodu")
            });

            // ── Icerik oncesi cizgi ──
            this.lineContent.LocationFloat = new DevExpress.Utils.PointFloat(0F, 200F);
            this.lineContent.SizeF = new System.Drawing.SizeF(727F, 2F);
            this.lineContent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(215)))), ((int)(((byte)(220)))));
            this.lineContent.Name = "lineContent";

            // ── ReportHeader Band ──
            this.reportHeaderBand.HeightF = 210F;
            this.reportHeaderBand.Name = "ReportHeader";
            this.reportHeaderBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.picLogo,
                this.lblCompanyName,
                this.lblCompanyInfo,
                this.lineHeader,
                this.lblTeklifTitle,
                this.lblTeklifNo,
                this.lblTarih,
                this.lblGecerlilik,
                this.lblSayin,
                this.lblIlgiliEmail,
                this.lblIlgiliMobil,
                this.lblMusteriAdi,
                this.lblMusteriKodu,
                this.lineContent
            });

            // ═══════════════════════════════════════════
            //  DETAIL BAND — ileride urun/fiyat icerigi eklenecek
            // ═══════════════════════════════════════════
            this.detailBand.HeightF = 0F;
            this.detailBand.Name = "Detail";

            // ═══════════════════════════════════════════
            //  PAGE FOOTER — firma bilgisi + sayfa no
            // ═══════════════════════════════════════════

            this.lineFooter.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.lineFooter.SizeF = new System.Drawing.SizeF(727F, 2F);
            this.lineFooter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(215)))), ((int)(((byte)(220)))));
            this.lineFooter.Name = "lineFooter";

            this.lblFooter.LocationFloat = new DevExpress.Utils.PointFloat(0F, 6F);
            this.lblFooter.SizeF = new System.Drawing.SizeF(580F, 15F);
            this.lblFooter.Font = new DevExpress.Drawing.DXFont("Segoe UI", 7F);
            this.lblFooter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lblFooter.Text = "YAL\u00C7IN DORSE San. ve Tic. A.\u015E. | www.yalcindorse.com | Tel: +90 (258) 812 18 88";
            this.lblFooter.Name = "lblFooter";

            this.lblPageInfo.LocationFloat = new DevExpress.Utils.PointFloat(630F, 6F);
            this.lblPageInfo.SizeF = new System.Drawing.SizeF(97F, 15F);
            this.lblPageInfo.Font = new DevExpress.Drawing.DXFont("Segoe UI", 7F);
            this.lblPageInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(150)))), ((int)(((byte)(150)))));
            this.lblPageInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.lblPageInfo.TextFormatString = "Sayfa {0} / {1}";
            this.lblPageInfo.Name = "lblPageInfo";

            this.pageFooterBand.HeightF = 25F;
            this.pageFooterBand.Name = "PageFooter";
            this.pageFooterBand.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.lineFooter,
                this.lblFooter,
                this.lblPageInfo
            });

            // ═══════════════════════════════════════════
            //  TOP / BOTTOM MARGINS
            // ═══════════════════════════════════════════
            this.topMarginBand.HeightF = 40F;
            this.topMarginBand.Name = "TopMargin";

            this.bottomMarginBand.HeightF = 40F;
            this.bottomMarginBand.Name = "BottomMargin";

            // ═══════════════════════════════════════════
            //  REPORT
            // ═══════════════════════════════════════════
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
                this.topMarginBand,
                this.reportHeaderBand,
                this.detailBand,
                this.pageFooterBand,
                this.bottomMarginBand
            });

            this.Font = new DevExpress.Drawing.DXFont("Segoe UI", 9F);
            this.Margins = new DevExpress.Drawing.DXMargins(50, 50, 40, 40);
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Version = "24.1";

            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
                this.pTeklifNo,
                this.pTarih,
                this.pGecerlilikTarihi,
                this.pMusteriAdi,
                this.pMusteriKodu,
                this.pIlgiliKisi,
                this.pIlgiliEmail,
                this.pIlgiliMobil,
                this.pSaticiAdi,
                this.pSaticiEmail,
                this.pNetTutar,
                this.pParaBirimi,
                this.pUrunAdi
            });

            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        // ── Bands ──
        private DevExpress.XtraReports.UI.TopMarginBand topMarginBand;
        private DevExpress.XtraReports.UI.ReportHeaderBand reportHeaderBand;
        private DevExpress.XtraReports.UI.DetailBand detailBand;
        private DevExpress.XtraReports.UI.BottomMarginBand bottomMarginBand;
        private DevExpress.XtraReports.UI.PageFooterBand pageFooterBand;

        // ── Controls ──
        internal DevExpress.XtraReports.UI.XRPictureBox picLogo;
        private DevExpress.XtraReports.UI.XRLabel lblCompanyName;
        private DevExpress.XtraReports.UI.XRLabel lblCompanyInfo;
        private DevExpress.XtraReports.UI.XRLine lineHeader;
        private DevExpress.XtraReports.UI.XRLabel lblTeklifTitle;
        private DevExpress.XtraReports.UI.XRLabel lblTeklifNo;
        private DevExpress.XtraReports.UI.XRLabel lblTarih;
        private DevExpress.XtraReports.UI.XRLabel lblGecerlilik;
        private DevExpress.XtraReports.UI.XRLabel lblSayin;
        private DevExpress.XtraReports.UI.XRLabel lblIlgiliEmail;
        private DevExpress.XtraReports.UI.XRLabel lblIlgiliMobil;
        private DevExpress.XtraReports.UI.XRLabel lblMusteriAdi;
        private DevExpress.XtraReports.UI.XRLabel lblMusteriKodu;
        private DevExpress.XtraReports.UI.XRLine lineContent;
        private DevExpress.XtraReports.UI.XRLine lineFooter;
        private DevExpress.XtraReports.UI.XRLabel lblFooter;
        private DevExpress.XtraReports.UI.XRPageInfo lblPageInfo;

        // ── Parameters ──
        private DevExpress.XtraReports.Parameters.Parameter pTeklifNo;
        private DevExpress.XtraReports.Parameters.Parameter pTarih;
        private DevExpress.XtraReports.Parameters.Parameter pGecerlilikTarihi;
        private DevExpress.XtraReports.Parameters.Parameter pMusteriAdi;
        private DevExpress.XtraReports.Parameters.Parameter pMusteriKodu;
        private DevExpress.XtraReports.Parameters.Parameter pIlgiliKisi;
        private DevExpress.XtraReports.Parameters.Parameter pIlgiliEmail;
        private DevExpress.XtraReports.Parameters.Parameter pIlgiliMobil;
        private DevExpress.XtraReports.Parameters.Parameter pSaticiAdi;
        private DevExpress.XtraReports.Parameters.Parameter pSaticiEmail;
        private DevExpress.XtraReports.Parameters.Parameter pNetTutar;
        private DevExpress.XtraReports.Parameters.Parameter pParaBirimi;
        private DevExpress.XtraReports.Parameters.Parameter pUrunAdi;
    }
}
