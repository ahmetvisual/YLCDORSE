-- ============================================================
-- YLTeklifler — Step 3 (Teslimat & Fiyat) yeni alanlari
-- Daha onceki sema runtime'da otomatik kurulmaliydi (EnsureSchemaAsync);
-- bazi PostgreSQL surumlerinde NOT NULL DEFAULT clause sessiz fail oluyor.
-- Bu scripti pgAdmin'de bir kez calistirin.
-- Idempotent: yeniden calistirmak guvenlidir.
-- ============================================================

ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "TeslimatNotlari" TEXT;
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "OnOdemeYuzdesi"  NUMERIC(10,4) DEFAULT 0;
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "OnOdemeTutari"   NUMERIC(18,4) DEFAULT 0;
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "BakiyeTutari"    NUMERIC(18,4) DEFAULT 0;
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "VadeGun"         INTEGER;

-- Mevcut NULL kayitlari 0 ile doldur (default uygulanmamis eski satirlar icin)
UPDATE "YLTeklifler" SET "OnOdemeYuzdesi" = 0 WHERE "OnOdemeYuzdesi" IS NULL;
UPDATE "YLTeklifler" SET "OnOdemeTutari"  = 0 WHERE "OnOdemeTutari"  IS NULL;
UPDATE "YLTeklifler" SET "BakiyeTutari"   = 0 WHERE "BakiyeTutari"   IS NULL;

-- Sonuc kontrol:
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns
-- WHERE table_name = 'YLTeklifler' AND column_name IN
--   ('TeslimatNotlari','OnOdemeYuzdesi','OnOdemeTutari','BakiyeTutari','VadeGun');
