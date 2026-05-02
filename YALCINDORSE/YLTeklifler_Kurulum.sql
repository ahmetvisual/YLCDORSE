CREATE SEQUENCE IF NOT EXISTS "YLTeklifNoSeq" START WITH 1 INCREMENT BY 1;

CREATE OR REPLACE FUNCTION "YLGenerateTeklifNo"()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW."TeklifNo" IS NULL OR BTRIM(NEW."TeklifNo") = '' THEN
        -- Generate something like YT2603001 (Yil=26, Ay=03, Sira=001)
        NEW."TeklifNo" := 'YT' || TO_CHAR(CURRENT_DATE, 'YYMM') || LPAD(nextval('"YLTeklifNoSeq"')::text, 3, '0');
    END IF;
    RETURN NEW;
END;
$$;

CREATE TABLE IF NOT EXISTS "YLTeklifler" (
    "Id" SERIAL PRIMARY KEY,
    "TeklifNo" VARCHAR(20) NOT NULL UNIQUE,
    "MusteriId" INT NOT NULL REFERENCES "YLCustomers"("Id"),
    "IlgiliKisiId" INT,
    "SatisTipi" VARCHAR(50) DEFAULT 'NewProduction',
    "Kaynak" VARCHAR(50) DEFAULT 'Email',
    "Dil" VARCHAR(10) DEFAULT 'TR',
    "ParaBirimi" VARCHAR(10) DEFAULT 'EUR',
    "Durum" VARCHAR(50) DEFAULT 'Draft',
    "Puan" VARCHAR(20) DEFAULT 'WARM',
    "TalepTarihi" DATE NOT NULL DEFAULT CURRENT_DATE,
    "GecerlilikTarihi" DATE NOT NULL,
    "SaticiId" INT REFERENCES "YLUsers"("Id"),
    "Notlar" TEXT,
    "ToplamTutar" NUMERIC(18,2) DEFAULT 0,
    "IndirimYuzde" NUMERIC(5,2) DEFAULT 0,
    "IndirimTutar" NUMERIC(18,2) DEFAULT 0,
    "NetTutar" NUMERIC(18,2) DEFAULT 0,
    "RevizyonNo" INT DEFAULT 0,
    "OnayGerektirir" BOOLEAN DEFAULT false,
    "OlusturmaTarihi" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "Olusturan" VARCHAR(100) DEFAULT 'system',
    "DegistirmeTarihi" TIMESTAMP,
    "Degistiren" VARCHAR(100)
);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'YLTeklifNoTrigger') THEN
        CREATE TRIGGER "YLTeklifNoTrigger"
        BEFORE INSERT ON "YLTeklifler"
        FOR EACH ROW
        EXECUTE FUNCTION "YLGenerateTeklifNo"();
    END IF;
END
$$;

CREATE TABLE IF NOT EXISTS "YLTeklifKalemleri" (
    "Id" SERIAL PRIMARY KEY,
    "TeklifId" INT NOT NULL REFERENCES "YLTeklifler"("Id") ON DELETE CASCADE,
    "BaslikMi" BOOLEAN DEFAULT false,
    "ItalicMi" BOOLEAN NOT NULL DEFAULT false,
    "Aciklama" TEXT NOT NULL,
    "SiraNo" INT DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "IX_YLTeklifler_MusteriId" ON "YLTeklifler"("MusteriId");
CREATE INDEX IF NOT EXISTS "IX_YLTeklifler_SaticiId" ON "YLTeklifler"("SaticiId");
CREATE INDEX IF NOT EXISTS "IX_YLTeklifKalemleri_TeklifId" ON "YLTeklifKalemleri"("TeklifId");
