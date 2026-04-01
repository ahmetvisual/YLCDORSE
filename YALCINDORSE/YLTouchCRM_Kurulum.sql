-- ============================================================
-- YL Touch CRM Schema - Temas/Revizyon/Ilgili Kisi Tablolari
-- ============================================================

-- 1. TEMAS (Touch) TABLOSU
CREATE TABLE IF NOT EXISTS "YLTemaslar" (
    "Id" SERIAL PRIMARY KEY,
    "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
    "RevizyonNo" INT NOT NULL DEFAULT 0,
    "TemasTarihi" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TemasEden" INT REFERENCES "YLUsers"("Id"),
    "TemasTipi" VARCHAR(20) NOT NULL DEFAULT 'NOTE',
    "Not" TEXT,
    "SonrakiTemasTarihi" DATE,
    "YonetimDahilMi" BOOLEAN DEFAULT FALSE,
    "DurumGuncelleme" VARCHAR(50),
    "OlusturmaTarihi" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "Olusturan" VARCHAR(100) DEFAULT 'system'
);

CREATE INDEX IF NOT EXISTS "IX_YLTemaslar_TeklifId" ON "YLTemaslar"("TeklifId");
CREATE INDEX IF NOT EXISTS "IX_YLTemaslar_SonrakiTemas" ON "YLTemaslar"("SonrakiTemasTarihi");
CREATE INDEX IF NOT EXISTS "IX_YLTemaslar_TemasEden" ON "YLTemaslar"("TemasEden");

-- 2. REVIZYON TABLOSU
CREATE TABLE IF NOT EXISTS "YLTeklifRevizyonlari" (
    "Id" SERIAL PRIMARY KEY,
    "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
    "RevizyonNo" INT NOT NULL,
    "RevizyonTarihi" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "Fiyat" NUMERIC(18,2),
    "ModelGuncelleme" VARCHAR(200),
    "Neden" TEXT,
    "Not" TEXT,
    "Olusturan" VARCHAR(100) DEFAULT 'system',
    UNIQUE("TeklifId", "RevizyonNo")
);

CREATE INDEX IF NOT EXISTS "IX_YLTeklifRevizyonlari_TeklifId" ON "YLTeklifRevizyonlari"("TeklifId");

-- 3. COKLU ILGILI KISI TABLOSU
CREATE TABLE IF NOT EXISTS "YLTeklifIlgiliKisileri" (
    "Id" SERIAL PRIMARY KEY,
    "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
    "IlgiliKisiId" INT NOT NULL REFERENCES "YLCustomerContacts"("Id")
);

CREATE INDEX IF NOT EXISTS "IX_YLTeklifIlgiliKisileri_TeklifId" ON "YLTeklifIlgiliKisileri"("TeklifId");

-- 4. ALTER YLTeklifler - Yeni Kolonlar
DO $$
BEGIN
    -- Teklif Kanali
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='TeklifKanali') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "TeklifKanali" VARCHAR(30);
    END IF;
    -- Teklif Tipi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='TeklifTipi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "TeklifTipi" VARCHAR(30);
    END IF;
    -- Aks Sayisi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='AksSayisi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "AksSayisi" INT;
    END IF;
    -- Odeme Sistemi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='OdemeSistemi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "OdemeSistemi" VARCHAR(30);
    END IF;
    -- Iskonto Aciklama
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='IskontoAciklama') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "IskontoAciklama" TEXT;
    END IF;
    -- KDV Dahil Mi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='KdvDahilMi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "KdvDahilMi" BOOLEAN DEFAULT FALSE;
    END IF;
    -- Ihracat Mi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='IhracatMi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "IhracatMi" BOOLEAN DEFAULT FALSE;
    END IF;
    -- Ihrac Kayitli Mi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='IhracKayitliMi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "IhracKayitliMi" BOOLEAN DEFAULT FALSE;
    END IF;
    -- Teslimat Haftasi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='TeslimatHaftasi') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "TeslimatHaftasi" VARCHAR(30);
    END IF;
    -- Teslimat Tipi Kodu
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='TeslimatTipiKodu') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "TeslimatTipiKodu" VARCHAR(30);
    END IF;
    -- Teslimat Yeri
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='TeslimatYeri') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "TeslimatYeri" TEXT;
    END IF;
    -- Siparis No
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifler' AND column_name='SiparisNo') THEN
        ALTER TABLE "YLTeklifler" ADD COLUMN "SiparisNo" VARCHAR(30);
    END IF;
END $$;

-- 5. ALTER YLTeklifKalemleri - Hiyerarsik Yapi
DO $$
BEGIN
    -- Ust Kalem Id (self-referencing)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='UstKalemId') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "UstKalemId" INT REFERENCES "YLTeklifKalemleri"("Id") ON DELETE SET NULL;
    END IF;
    -- Urun Kodu
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='UrunKodu') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "UrunKodu" VARCHAR(50);
    END IF;
    -- Miktar
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='Miktar') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "Miktar" NUMERIC(18,3);
    END IF;
    -- Birim
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='Birim') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "Birim" VARCHAR(20);
    END IF;
    -- Birim Fiyat
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='BirimFiyat') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "BirimFiyat" NUMERIC(18,2);
    END IF;
    -- Tutar
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='Tutar') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "Tutar" NUMERIC(18,2);
    END IF;
    -- Opsiyon Mu
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='OpsiyonMu') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "OpsiyonMu" BOOLEAN DEFAULT FALSE;
    END IF;
    -- Kalem Tipi
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='YLTeklifKalemleri' AND column_name='KalemTipi') THEN
        ALTER TABLE "YLTeklifKalemleri" ADD COLUMN "KalemTipi" VARCHAR(20) DEFAULT 'ITEM';
    END IF;
END $$;

-- 6. SIPARIS NO SEKANS
CREATE SEQUENCE IF NOT EXISTS "YLSiparisNoSeq" START WITH 1 INCREMENT BY 1;
