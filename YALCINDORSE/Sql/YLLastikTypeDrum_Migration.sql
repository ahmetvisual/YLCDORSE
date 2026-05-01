-- ============================================================
-- Migration: LastikType + LastikDrum kolonları + lookup değerleri
-- Tarih     : 2026-04-30
-- Açıklama  : Lastik alanı TYPE ve DRUM olarak iki ayrı alana ayrıldı.
--             Eski "Lastik" kolonu geriye uyumluluk için bırakıldı.
-- ============================================================

-- 1. Yeni kolonlar
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "LastikType" TEXT;
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "LastikDrum" TEXT;

-- 2. LASTIK_TYPE değerleri
INSERT INTO "YLLookupValues" ("LookupType", "Value", "SortOrder") VALUES
    ('LASTIK_TYPE', '11',     10), ('LASTIK_TYPE', '12',     20),
    ('LASTIK_TYPE', '12.00',  30), ('LASTIK_TYPE', '13',     40),
    ('LASTIK_TYPE', '14.00',  50), ('LASTIK_TYPE', '185/55', 60),
    ('LASTIK_TYPE', '205/65', 70), ('LASTIK_TYPE', '205/75', 80),
    ('LASTIK_TYPE', '215/75', 90), ('LASTIK_TYPE', '225/75',100),
    ('LASTIK_TYPE', '235/75',110), ('LASTIK_TYPE', '245/70',120),
    ('LASTIK_TYPE', '255/70',130), ('LASTIK_TYPE', '265/70',140),
    ('LASTIK_TYPE', '275/70',150), ('LASTIK_TYPE', '285/70',160),
    ('LASTIK_TYPE', '295/55',170), ('LASTIK_TYPE', '295/60',180),
    ('LASTIK_TYPE', '295/80',190), ('LASTIK_TYPE', '305/70',200),
    ('LASTIK_TYPE', '315/45',210), ('LASTIK_TYPE', '315/60',220),
    ('LASTIK_TYPE', '315/70',230), ('LASTIK_TYPE', '315/80',240),
    ('LASTIK_TYPE', '325/95',250), ('LASTIK_TYPE', '335/80',260),
    ('LASTIK_TYPE', '355/50',270), ('LASTIK_TYPE', '355/60',280),
    ('LASTIK_TYPE', '365/80',290), ('LASTIK_TYPE', '365/85',300),
    ('LASTIK_TYPE', '385/55',310), ('LASTIK_TYPE', '385/65',320),
    ('LASTIK_TYPE', '395/85',330), ('LASTIK_TYPE', '435/50',340),
    ('LASTIK_TYPE', '445/45',350), ('LASTIK_TYPE', '445/60',360)
ON CONFLICT ("LookupType", "Value") DO NOTHING;

-- 3. LASTIK_DRUM değerleri
INSERT INTO "YLLookupValues" ("LookupType", "Value", "SortOrder") VALUES
    ('LASTIK_DRUM', 'R 17.5', 10),
    ('LASTIK_DRUM', 'R 19.5', 20),
    ('LASTIK_DRUM', 'R 20',   30),
    ('LASTIK_DRUM', 'R 22.5', 40),
    ('LASTIK_DRUM', 'R 24',   50)
ON CONFLICT ("LookupType", "Value") DO NOTHING;
