-- ============================================================
-- YLArabaslik OLCULER + AGIRLIKLAR sablon import (Turkce, distinct)
-- Kaynak: OLCULER_VE_AGIRLIKLAR.xlsx (Veri (DB Format))
-- - Her kalem ismi TEK satir, Deger BOS (kullanici manuel girer)
-- - Sadece Turkce kalemler; Ingilizce duplikalar haric tutuldu
--   (EN ceviri istenirse arayuzden SatirMetni_EN doldurulabilir)
-- - Idempotent: yeniden calistirilirsa eski detay silinip yeniden yuklenir
-- ============================================================

DO $$
DECLARE
    olculer_id    INT;
    agirliklar_id INT;
BEGIN

    -- ======== ÖLÇÜLER ========
    SELECT "Id" INTO olculer_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'ÖLÇÜLER' LIMIT 1;
    IF olculer_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('ÖLÇÜLER', 'DIMENSIONS', '', '', '', '', '', 1, 100, TRUE, NOW(), 'Excel Import')
        RETURNING "Id" INTO olculer_id;
    END IF;
    DELETE FROM "YLArabaslikDetaylar" WHERE "GrupId" = olculer_id;

    -- ======== AĞIRLIKLAR ========
    SELECT "Id" INTO agirliklar_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'AĞIRLIKLAR' LIMIT 1;
    IF agirliklar_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('AĞIRLIKLAR', 'WEIGHTS', '', '', '', '', '', 1, 110, TRUE, NOW(), 'Excel Import')
        RETURNING "Id" INTO agirliklar_id;
    END IF;
    DELETE FROM "YLArabaslikDetaylar" WHERE "GrupId" = agirliklar_id;

    -- ÖLÇÜLER kalemleri (20 kayit)
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'2’nci 5. teker yüksekliği','','','','','','',NULL,'',10);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'2’nci Yükleme platformu yüksekliği','','','','','','',NULL,'',20);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'5. teker yüksekliği','','','','','','',NULL,'',30);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Aks bojisi genişliği','','','','','','',NULL,'',40);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Aks mesafesi','','','','','','',NULL,'',50);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Arka boyun salınımı','','','','','','',NULL,'',60);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Arka deveboynu salınımı','','','','','','',NULL,'',70);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Boyun uzunluğu','','','','','','',NULL,'',80);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Deveboynu uzunluğu','','','','','','',NULL,'',90);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Platform toplam uzunluk','','','','','','',NULL,'',100);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Süspansiyon','','','','','','',NULL,'',110);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu genişliği','','','','','','',NULL,'',120);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu kalınlığı','','','','','','',NULL,'',130);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu uzaması','','','','','','',NULL,'',140);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu uzunluğu','','','','','','',NULL,'',150);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu yüksekliği','','','','','','',NULL,'',160);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Yükleme platformu zemin yüksekliği','','','','','','',NULL,'',170);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Çeki oku yüksekliği','','','','','','',NULL,'',180);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Ön boyun salınımı','','','','','','',NULL,'',190);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (olculer_id,'Ön deveboynu salınımı','','','','','','',NULL,'',200);

    -- AĞIRLIKLAR kalemleri (20 kayit)
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'10x4 çekici boş ağırlık (yaklaşık)','','','','','','',NULL,'',10);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'4x2 çekici boş ağırlık (yaklaşık)','','','','','','',NULL,'',20);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü','','','','','','',NULL,'',30);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü (4x2 çekiciyle)','','','','','','',NULL,'',40);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü (6x4 çekici ile)','','','','','','',NULL,'',50);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'5. teker yükü (6x4 çekiciyle)','','','','','','',NULL,'',60);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'6x4 çekici boş (yaklaşık)','','','','','','',NULL,'',70);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'6x4 çekici boş ağırlık (yaklaşık)','','','','','','',NULL,'',80);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Aks yükü','','','','','','',NULL,'',90);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Aksesuar ağırlığı (yaklaşık)','','','','','','',NULL,'',100);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Boş ağırlık yaklaşık (yaklaşık)','','','','','','',NULL,'',110);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Hız','','','','','','',NULL,'',120);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Low loader boş ağırlık (yaklaşık)','','','','','','',NULL,'',130);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Low loader boş ağırlık yaklaşık (yaklaşık)','','','','','','',NULL,'',140);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Lowbed boş ağırlık','','','','','','',NULL,'',150);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Lowbed boş ağırlık (yaklaşık)','','','','','','',NULL,'',160);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Platform boş ağırlık (yaklaşık)','','','','','','',NULL,'',170);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Römork boş ağırlık (yaklaşık)','','','','','','',NULL,'',180);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Taşıma kapasitesi','','','','','','',NULL,'',190);
    INSERT INTO "YLArabaslikDetaylar"("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder") VALUES (agirliklar_id,'Toplam ağırlık','','','','','','',NULL,'',200);

    RAISE NOTICE 'Import tamam — ÖLÇÜLER id=%, AĞIRLIKLAR id=%', olculer_id, agirliklar_id;
END$$;
