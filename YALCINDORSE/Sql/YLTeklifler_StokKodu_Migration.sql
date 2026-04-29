-- ============================================================
-- Migration: YLTeklifler — StokKodu kolonu eklendi
-- Tarih     : 2026-04-29
-- Aciklama  : Stok / Ikinci El satis tiplerinde kullanilan
--             ACF ile baslayan stok kodu alani.
-- ============================================================
ALTER TABLE "YLTeklifler" ADD COLUMN IF NOT EXISTS "StokKodu" TEXT;

-- Kontrol / verify:
-- SELECT "Id","TeklifNo","SatisTipi","StokKodu","SasiNo","ModelYili"
-- FROM "YLTeklifler" WHERE "SatisTipi" IN ('Stock','SecondHand') LIMIT 20;
