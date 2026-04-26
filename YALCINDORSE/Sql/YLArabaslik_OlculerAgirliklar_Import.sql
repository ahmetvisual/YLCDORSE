-- ============================================================
-- YLArabaslik OLCULER + AGIRLIKLAR sablon import (v7 dedup)
-- Kaynak: TUM_BOLUMLER_TURKCE.xlsx (DB Format) — guncel versiyon
-- TablTipi=1 (Tablo / iki kolon: Kalem : Deger)
-- - Prefix-strip + canonical + fuzzy + typo override pipeline
-- - Deger BOS, kullanici teklife eklerken manuel yazar
-- - Idempotent: ayni isimde grup varsa tekrar olusturulmaz; eski detaylar silinir
-- ============================================================

DO $$
DECLARE
    olculer_id    INT;
    agirliklar_id INT;
BEGIN

    -- ======== ÖLÇÜLER (16 kalem) ========
    SELECT "Id" INTO olculer_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'ÖLÇÜLER' LIMIT 1;
    IF olculer_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('ÖLÇÜLER', 'DIMENSIONS', '', '', '', '', '', 1, 100, TRUE, NOW(), 'Excel Import')
        RETURNING "Id" INTO olculer_id;
    END IF;
    DELETE FROM "YLArabaslikDetaylar" WHERE "GrupId" = olculer_id;
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'5. teker yüksekliği','','','','','','',NULL,'',10);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Aks mesafesi','','','','','','',NULL,'',20);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Arka boyun salınımı','','','','','','',NULL,'',30);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Arka deveboynu salınımı','','','','','','',NULL,'',40);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Boyun uzunluğu','','','','','','',NULL,'',50);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Deveboynu uzunluğu','','','','','','',NULL,'',60);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Genişlik','','','','','','',NULL,'',70);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Süspansiyon','','','','','','',NULL,'',80);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu genişliği','','','','','','',NULL,'',90);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu kalınlığı','','','','','','',NULL,'',100);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu uzaması','','','','','','',NULL,'',110);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu uzunluğu','','','','','','',NULL,'',120);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu yüksekliği','','','','','','',NULL,'',130);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu zemin yüksekliği','','','','','','',NULL,'',140);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Ön boyun salınımı','','','','','','',NULL,'',150);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Ön deveboynu salınımı','','','','','','',NULL,'',160);

    -- ======== AĞIRLIKLAR (16 kalem) ========
    SELECT "Id" INTO agirliklar_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'AĞIRLIKLAR' LIMIT 1;
    IF agirliklar_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('AĞIRLIKLAR', 'WEIGHTS', '', '', '', '', '', 1, 110, TRUE, NOW(), 'Excel Import')
        RETURNING "Id" INTO agirliklar_id;
    END IF;
    DELETE FROM "YLArabaslikDetaylar" WHERE "GrupId" = agirliklar_id;
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'4x2 çekici boş ağırlık (yaklaşık)','','','','','','',NULL,'',10);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü','','','','','','',NULL,'',20);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü (4x2 çekici ile)','','','','','','',NULL,'',30);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü (6x4 çekici ile)','','','','','','',NULL,'',40);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5’inci teker yükü','','','','','','',NULL,'',50);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'6x4 çekici boş (yaklaşık)','','','','','','',NULL,'',60);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'6x4 çekici boş ağırlık (yaklaşık)','','','','','','',NULL,'',70);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Aks yükü','','','','','','',NULL,'',80);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Boş ağırlık yaklaşık (yaklaşık)','','','','','','',NULL,'',90);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Hız','','','','','','',NULL,'',100);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Low loader boş ağırlık yaklaşık (yaklaşık)','','','','','','',NULL,'',110);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Lowbed boş ağırlık (yaklaşık)','','','','','','',NULL,'',120);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Platform boş ağırlık (yaklaşık)','','','','','','',NULL,'',130);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Taşıma kapasitesi','','','','','','',NULL,'',140);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Teknik taşıma kapasitesi (25 km/h hızda)','','','','','','',NULL,'',150);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Toplam ağırlık','','','','','','',NULL,'',160);

    RAISE NOTICE 'Tablo import tamam: ÖLÇÜLER id=%, AĞIRLIKLAR id=%', olculer_id, agirliklar_id;
END$$;
