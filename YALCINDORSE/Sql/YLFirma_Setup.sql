-- ============================================================
-- YLFirmaBilgileri & YLFirmaHesaplari — Schema + IBAN seed
-- Kaynak: barandrive/HESAP BİLGİLERİ.xlsx (BANK ACCOUNTS)
-- - YLFirmaBilgileri singleton (Id=1)
-- - YLFirmaHesaplari: 22 banka hesabi (TL/EUR/USD)
-- - Idempotent: yeniden calistirilabilir (IBAN UNIQUE check)
-- - Schema runtime ile FirmaService.EnsureSchemaAsync da olusturur,
--   bu script bagimsiz olarak da calisir.
-- ============================================================

CREATE TABLE IF NOT EXISTS "YLFirmaBilgileri" (
    "Id"                    INTEGER PRIMARY KEY,
    "TamUnvan"              TEXT NOT NULL DEFAULT '',
    "KisaUnvan"             TEXT NOT NULL DEFAULT '',
    "VergiDairesi"          TEXT NOT NULL DEFAULT '',
    "VergiNo"               TEXT NOT NULL DEFAULT '',
    "MersisNo"              TEXT NOT NULL DEFAULT '',
    "TicaretSicilNo"        TEXT NOT NULL DEFAULT '',
    "AdresSatir1"           TEXT NOT NULL DEFAULT '',
    "AdresSatir2"           TEXT NOT NULL DEFAULT '',
    "Sehir"                 TEXT NOT NULL DEFAULT '',
    "Ulke"                  TEXT NOT NULL DEFAULT '',
    "PostaKodu"             TEXT NOT NULL DEFAULT '',
    "Telefon"               TEXT NOT NULL DEFAULT '',
    "Faks"                  TEXT NOT NULL DEFAULT '',
    "Email"                 TEXT NOT NULL DEFAULT '',
    "Web"                   TEXT NOT NULL DEFAULT '',
    "LogoBytes"             BYTEA,
    "LogoMime"              TEXT,
    "KapakFotoBytes"        BYTEA,
    "KapakFotoMime"         TEXT,
    "GuncellenmeTarihi"     TIMESTAMP,
    "GuncelleyenKullanici"  TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS "YLFirmaHesaplari" (
    "Id"         SERIAL PRIMARY KEY,
    "BankaAdi"   TEXT NOT NULL DEFAULT '',
    "ParaBirimi" TEXT NOT NULL DEFAULT 'TL',
    "Sube"       TEXT NOT NULL DEFAULT '',
    "IBAN"       TEXT NOT NULL DEFAULT '',
    "HesapNo"    TEXT NOT NULL DEFAULT '',
    "SwiftKodu"  TEXT NOT NULL DEFAULT '',
    "AktifMi"    BOOLEAN NOT NULL DEFAULT TRUE,
    "SortOrder"  INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_ylfirmahesaplari_aktif ON "YLFirmaHesaplari"("AktifMi", "SortOrder");

-- Singleton firma kaydi (Id=1) — Excel'deki ana sirket DORSE DAMPER baz alindi
INSERT INTO "YLFirmaBilgileri" ("Id","TamUnvan","KisaUnvan")
VALUES (1,'YALÇIN DORSE DAMPER SAN. VE TİC. LTD. ŞTİ.','YALÇIN DORSE')
ON CONFLICT ("Id") DO NOTHING;

-- Banka hesaplari — IBAN'a gore idempotent INSERT
DO $$
DECLARE
    _bank TEXT; _para TEXT; _sube TEXT; _iban TEXT; _company TEXT;
BEGIN

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'GARANTİ BBVA (Dorse)', 'TL', '595 - 6297007 SİLİVRİ', 'TR440006200059500006297007', TRUE, 10
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR440006200059500006297007');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'GARANTİ BBVA (Dorse)', 'EUR', '595 - 9093069 SİLİVRİ', 'TR850006200059500009093070', TRUE, 20
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR850006200059500009093070');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'GARANTİ BBVA (Dorse)', 'USD', '595 - 9093070 SİLİVRİ', 'TR150006200059500009093069', TRUE, 30
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR150006200059500009093069');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE HALK BANKASI A.Ş. (Dorse)', 'TL', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR940001200137800010100081', TRUE, 40
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR940001200137800010100081');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE HALK BANKASI A.Ş. (Dorse)', 'EUR', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR130001200137800058100049', TRUE, 50
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR130001200137800058100049');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE HALK BANKASI A.Ş. (Dorse)', 'USD', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR580001200137800053100053', TRUE, 60
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR580001200137800053100053');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE VAKIFLAR BANKASI T.A.O. (Dorse)', 'TL', 'Hurrıyet Tekırdag Subesı-S01047', 'TR370001500158007297036415', TRUE, 70
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR370001500158007297036415');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE VAKIFLAR BANKASI T.A.O. (Dorse)', 'EUR', 'Hurrıyet Tekırdag Subesı-S01047', 'TR660001500158048018275802', TRUE, 80
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR660001500158048018275802');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE VAKIFLAR BANKASI T.A.O. (Dorse)', 'USD', 'Hurrıyet Tekırdag Subesı-S01047', 'TR750001500158048018257889', TRUE, 90
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR750001500158048018257889');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE İŞ BANKASI A.Ş. (Dorse)', 'TL', '1522 - Emlakkent Çorlu Tekirdağ Şubesi', 'TR310006400000115220138841', TRUE, 100
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR310006400000115220138841');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE İŞ BANKASI A.Ş. (Dorse)', 'EUR', '1523 - Emlakkent Çorlu Tekirdağ Şubesi', 'TR390006400000215220019382', TRUE, 110
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR390006400000215220019382');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE CUMHURİYETİ ZİRAAT BANKASI A.Ş. (Dorse)', 'TL', 'Çorlu Tekirdağ Tic. Şubesi', 'TR250001002145016074835004', TRUE, 120
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR250001002145016074835004');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE CUMHURİYETİ ZİRAAT BANKASI A.Ş. (Dorse)', 'EUR', 'Çorlu Tekirdağ Tic. Şubesi', 'TR410001002145016074835007', TRUE, 130
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR410001002145016074835007');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE CUMHURİYETİ ZİRAAT BANKASI A.Ş. (Dorse)', 'USD', 'Çorlu Tekirdağ Tic. Şubesi', 'TR680001002145016074835006', TRUE, 140
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR680001002145016074835006');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE EMLAK KATILIM BANKASI A.Ş. (Dorse)', 'TL', 'Çorlu Şube 088', 'TR820021100000073761800001', TRUE, 150
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR820021100000073761800001');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE EMLAK KATILIM BANKASI A.Ş. (Dorse)', 'EUR', 'EMLATRISXXX Swift', 'TR710021100000073761800102', TRUE, 160
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR710021100000073761800102');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'GARANTİ BBVA (Lowbed)', 'TL', '595 - 6295813 SİLİVRİ', 'TR780006200059500006295813', TRUE, 170
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR780006200059500006295813');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'GARANTİ BBVA (Lowbed)', 'EUR', '595 - 9087110 SİLİVRİ', 'TR820006200059500009087110', TRUE, 180
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR820006200059500009087110');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE HALK BANKASI A.Ş. (Lowbed)', 'TL', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR730001200137800010100459', TRUE, 190
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR730001200137800010100459');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE HALK BANKASI A.Ş. (Lowbed)', 'EUR', '1378 SİLİVRİ E-5 ŞUBESİ', 'TR500001200137800058100159', TRUE, 200
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR500001200137800058100159');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE VAKIFLAR BANKASI T.A.O. (Lowbed)', 'TL', '158007311915411 Hurrıyet Tekırdag Subesı-S01047', 'TR170001500158007311915411', TRUE, 210
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR170001500158007311915411');

    INSERT INTO "YLFirmaHesaplari" ("BankaAdi","ParaBirimi","Sube","IBAN","AktifMi","SortOrder")
    SELECT 'TÜRKİYE VAKIFLAR BANKASI T.A.O. (Lowbed)', 'EUR', '158048018866429 Hurrıyet Tekırdag Subesı-S01047', 'TR340001500158048018866429', TRUE, 220
    WHERE NOT EXISTS (SELECT 1 FROM "YLFirmaHesaplari" WHERE "IBAN" = 'TR340001500158048018866429');

END $$;

-- Sonuc kontrol:
-- SELECT COUNT(*) FROM "YLFirmaBilgileri";  -- 1 olmali
-- SELECT COUNT(*) FROM "YLFirmaHesaplari"; -- 22 olmali (ilk calistirma)
