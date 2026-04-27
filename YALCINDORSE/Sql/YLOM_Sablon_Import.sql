-- ============================================================
-- YLOM Sablonlari - OM_TR/OM_EN.xlsx kaynakli teklif sablonlari
-- 3 grup (BPW/SAF, TURK MALI, OPSIYONEL) + 86 detay (TR + EN)
-- TablTipi=2 (Liste); Fiyat EUR cinsinden
-- Idempotent: ayni isimde grup varsa atlar
-- ============================================================

DO $$
DECLARE
    grup_id INT;
BEGIN

    -- ======== Grup 1: FİYAT LİSTESİ BPW VEYA SAF AKSLARLA ========
    -- ======== Grup 1: FİYAT LİSTESİ BPW VEYA SAF AKSLARLA ========
    SELECT "Id" INTO grup_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'FİYAT LİSTESİ BPW VEYA SAF AKSLARLA' LIMIT 1;
    IF grup_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('FİYAT LİSTESİ BPW VEYA SAF AKSLARLA', 'PRICE LIST WITH BPW OR SAF AXLES', '', '', '', '', '', 2, 900, TRUE, NOW(), 'OM Manual Import')
        RETURNING "Id" INTO grup_id;
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB2 - 2 Aks Lowbed Yarı Römork — 1 Sabit - 1 Kendinden Döner Aks BPW veya SAF', 'LB2 - 2 Axles Lowbed Semi Trailer — 1 Fixed - 1 Self Steering Axle BPW or SAF', '', '', '', '', '', 50000.0, 'EUR', 10);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB3 - 3 Aks Lowbed Yarı Römork — 2 Sabit - 1 Kendinden Döner Aks BPW veya SAF', 'LB3 - 3 Axles Lowbed Semi Trailer — 2 Fixed - 1 Self Steering Axle BPW or SAF', '', '', '', '', '', 53700.0, 'EUR', 20);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB4 - 4 Aks Lowbed Yarı Römork — 2 Sabit - 2 Kendinden Döner Aks BPW veya SAF', 'LB4 - 4 Axles Lowbed Semi Trailer — 2 Fixed - 2 Self Steering Axle BPW or SAF', '', '', '', '', '', 60900.0, 'EUR', 30);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB5 - 5 Aks Lowbed Yarı Römork — 3 Sabit - 2 Kendinden Döner Aks BPW veya SAF', 'LB5 - 5 Axles Lowbed Semi Trailer — 3 Fixed - 2 Self Steering Axle BPW or SAF', '', '', '', '', '', 67700.0, 'EUR', 40);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB5 - 5 Aks Lowbed Yarı Römork — 2 Sabit - 3 Kendinden Döner Aks BPW veya SAF', 'LB5 - 5 Axles Lowbed Semi Trailer — 2 Fixed - 3 Self Steering Axle BPW or SAF', '', '', '', '', '', 71500.0, 'EUR', 50);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB6 - 6 Aks Lowbed Yarı Römork — 3 Sabit - 3 Kendinden Döner Aks BPW veya SAF', 'LB6 - 6 Axles Lowbed Semi Trailer — 3 Fixed - 3 Self Steering Axle BPW or SAF', '', '', '', '', '', 79000.0, 'EUR', 60);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LB6 - 6 Aks Lowbed Yarı Römork — 2 Sabit - 4 Kendinden Döner Aks BPW veya SAF', 'LB6 - 6 Axles Lowbed Semi Trailer — 2 Fixed - 4 Self Steering Axle BPW or SAF', '', '', '', '', '', 82800.0, 'EUR', 70);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LL2 - 2 Aks Low Loader Yarı Römork — 2 Hidrolik Dümenlenir Aks BPW veya SAF', 'LL2 - 2 Axles Low Loader Semi Trailer — 2 Hydraulic Steering Axles BPW or SAF', '', '', '', '', '', 106000.0, 'EUR', 80);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LL3 - 3 Aks Low Loader Yarı Römork — 3 Hidrolik Dümenlenir Aks BPW veya SAF', 'LL3 - 3 Axles Low Loader Semi Trailer — 3 Hydraulic Steering Axles BPW or SAF', '', '', '', '', '', 130000.0, 'EUR', 90);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LL4 - 4 Aks Low Loader Yarı Römork — 4 Hidrolik Dümenlenir Aks BPW veya SAF', 'LL4 - 4 Axles Low Loader Semi Trailer — 4 Hydraulic Steering Axles BPW or SAF', '', '', '', '', '', 154000.0, 'EUR', 100);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'LL4 - 4 Aks Low Loader Yarı Römork — 4 Hidrolik Dümenlenir Aks (Ağır Görev Tipi) BPW veya SAF', 'LL4 - 4 Axles Low Loader Semi Trailer — 4 Hydraulic Steering Axles (Heavy Duty) BPW or SAF', '', '', '', '', '', 169000.0, 'EUR', 110);
    END IF;

    -- ======== Grup 2: FİYAT LİSTESİ TÜRK MALI AKSLAR İLE ========
    SELECT "Id" INTO grup_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'FİYAT LİSTESİ TÜRK MALI AKSLAR İLE' LIMIT 1;
    IF grup_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('FİYAT LİSTESİ TÜRK MALI AKSLAR İLE', 'PRICE LIST WITH TURKISH MADE AXLES', '', '', '', '', '', 2, 910, TRUE, NOW(), 'OM Manual Import')
        RETURNING "Id" INTO grup_id;
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'TR2 - 2 Aks Lowbed Yarı Römork — 1 Sabit - 1 Kendinden Döner Aks TÜRK MALI AKSLAR', 'TR2 - 2 Axles Lowbed Semi Trailer — 1 Fixed - 1 Self Steering Axle TURKISH MADE AXLES', '', '', '', '', '', 46500.0, 'EUR', 10);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'TR3 - 3 Aks Lowbed Yarı Römork — 2 Sabit - 1 Kendinden Döner Aks TÜRK MALI AKSLAR', 'TR3 - 3 Axles Lowbed Semi Trailer — 2 Fixed - 1 Self Steering Axle TURKISH MADE AXLES', '', '', '', '', '', 49200.0, 'EUR', 20);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'TR4 - 4 Aks Lowbed Yarı Römork — 2 Sabit - 2 Kendinden Döner Aks TÜRK MALI AKSLAR', 'TR4 - 4 Axles Lowbed Semi Trailer — 2 Fixed - 2 Self Steering Axle TURKISH MADE AXLES', '', '', '', '', '', 53900.0, 'EUR', 30);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'TR5 - 5 Aks Lowbed Yarı Römork — 3 Sabit - 2 Kendinden Döner Aks TÜRK MALI AKSLAR', 'TR5 - 5 Axles Lowbed Semi Trailer — 3 Fixed - 2 Self Steering Axle TURKISH MADE AXLES', '', '', '', '', '', 59700.0, 'EUR', 40);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'TR5 - 5 Aks Lowbed Yarı Römork — 2 Sabit - 3 Kendinden Döner Aks TÜRK MALI AKSLAR', 'TR5 - 5 Axles Lowbed Semi Trailer — 2 Fixed - 3 Self Steering Axle TURKISH MADE AXLES', '', '', '', '', '', 62000.0, 'EUR', 50);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'TR6 - 6 Aks Lowbed Yarı Römork — 3 Sabit - 3 Kendinden Döner Aks TÜRK MALI AKSLAR', 'TR6 - 6 Axles Lowbed Semi Trailer — 3 Fixed - 3 Self Steering Axle TURKISH MADE AXLES', '', '', '', '', '', 68500.0, 'EUR', 60);
    END IF;

    -- ======== Grup 3: OPSİYONEL ÖZELLİKLER ========
    SELECT "Id" INTO grup_id FROM "YLArabaslikGruplar" WHERE "GrupAdi" = 'OPSİYONEL ÖZELLİKLER' LIMIT 1;
    IF grup_id IS NULL THEN
        INSERT INTO "YLArabaslikGruplar"
            ("GrupAdi","GrupAdi_EN","GrupAdi_FR","GrupAdi_DE","GrupAdi_RO","GrupAdi_AR","GrupAdi_RU",
             "TablTipi","SortOrder","IsActive","CreatedDate","CreatedBy")
        VALUES ('OPSİYONEL ÖZELLİKLER', 'OPTIONAL FEATURES', '', '', '', '', '', 2, 920, TRUE, NOW(), 'OM Manual Import')
        RETURNING "Id" INTO grup_id;
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A001 - Tek uzar yükleme platformu', 'A001 - Single extendable loading platform', '', '', '', '', '', 7200.0, 'EUR', 10);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A002 - Çift uzar yükleme platformu', 'A002 - Double extendable loading platform', '', '', '', '', '', 25000.0, 'EUR', 20);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A003 - Hidrolik, ağır görev tipi, tek parça yükleme rampaları', 'A003 - Hydraulic operated heavy duty type single piece loading ramps', '', '', '', '', '', 5100.0, 'EUR', 30);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A004 - Hidrolik, ağır görev tipi, çift parça yükleme rampaları', 'A004 - Hydraulic operated heavy duty type double piece loading ramps', '', '', '', '', '', 7100.0, 'EUR', 40);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A005 - Hidrolik, hafif görev tipi, çift parça yükleme rampaları', 'A005 - Hydraulic operated light weight type double piece loading ramps', '', '', '', '', '', 6350.0, 'EUR', 50);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A006 - Hidrolik olarak içe ve dışa kayar yükleme rampaları', 'A006 - Hydraulic in and out sliding mechanism for loading ramps', '', '', '', '', '', 1650.0, 'EUR', 60);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A007 - Sökülebilir yükleme rampaları (5 dk içinde hızlı sökme ve takma)', 'A007 - Detachable ramp system (quick attach and detach in 5 min)', '', '', '', '', '', 4650.0, 'EUR', 70);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A008 - Çifti 20 ton kapasiteli alüminyum yükleme rampaları', 'A008 - Aluminium loading ramps with 20 tons capacity per pair', '', '', '', '', '', 2000.0, 'EUR', 80);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A009 - Çifti 30 ton kapasiteli alüminyum yükleme rampaları', 'A009 - Aluminium loading ramps with 30 tons capacity per pair', '', '', '', '', '', 2500.0, 'EUR', 90);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A010 - Yükleme rampaları için sıcak daldırma galvaniz kaplama', 'A010 - Hot dipped galvanization for loading ramps', '', '', '', '', '', 1150.0, 'EUR', 100);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A011 - Boyalı, kısa, çelik ön duvar', 'A011 - Short, painted steel head board', '', '', '', '', '', 650.0, 'EUR', 110);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A012 - Galvaniz kaplı, kısa, çelik ön duvar', 'A012 - Galvanized, short steel head board', '', '', '', '', '', 750.0, 'EUR', 120);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A013 - Galvaniz kaplı, uzun, çelik ön duvar', 'A013 - Galvanized, long steel head board', '', '', '', '', '', 900.0, 'EUR', 130);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A014 - Takım dolabı formunda çelik ön duvar', 'A014 - Tool box formed steel head board', '', '', '', '', '', 1350.0, 'EUR', 140);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A015 - Geçme tip, alüminyum yan kapaklar', 'A015 - Joggle type, aluminium side boards', '', '', '', '', '', 500.0, 'EUR', 150);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A016 - Aşağı açılır tip, alüminyum yan kapaklar', 'A016 - Fold down type aluminium side boards', '', '', '', '', '', 600.0, 'EUR', 160);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A017 - Aşağı açılır tip, alüminyum yan kapaklar KINNEGRIP babalı', 'A017 - Fold down type aluminium side boards with KINNEGRIP pillars', '', '', '', '', '', 1300.0, 'EUR', 170);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A018 - Geçme tip, alüminyum arka kapak', 'A018 - Joggle type, aluminium close board', '', '', '', '', '', 250.0, 'EUR', 180);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A019 - Alüminyum, deveboynu tırmanma rampası', 'A019 - Aluminium gooseneck climbing ramps', '', '', '', '', '', 1050.0, 'EUR', 190);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A020 - Çelik, deveboynu tırmanma rampası', 'A020 - Steel gooseneck climbing ramps', '', '', '', '', '', 400.0, 'EUR', 200);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A021 - Hidrolik olarak kaldırılabilir deveboynu tırmanma platformu', 'A021 - Hydraulic liftable gooseneck climbing platform', '', '', '', '', '', 6500.0, 'EUR', 210);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A022 - 1 çift 18 ton kapasiteli alüminyum köprü U: 1.000 mm', 'A022 - 1 pair of aluminium bridge with 18 tons capacity L: 1.000 mm', '', '', '', '', '', 2100.0, 'EUR', 220);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A023 - 1 çift teker havuzu ve galvaniz kaplı çelik çerçeveli ahşap kapaklar', 'A023 - 1 pair of wheel wells with galvanized steel framed wood covers', '', '', '', '', '', 3100.0, 'EUR', 230);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A024 - Ekskavatör bom havuzu', 'A024 - Excavator well', '', '', '', '', '', 2250.0, 'EUR', 240);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A025 - Galvaniz kaplı çelik çerçeveli yana genişleme ahşapları', 'A025 - Outrigger woods with galvanized steel frame', '', '', '', '', '', 1550.0, 'EUR', 250);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A026 - Çerçevesiz yana genişleme ahşapları', 'A026 - Outrigger woods without galvanized steel frame', '', '', '', '', '', 1100.0, 'EUR', 260);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A027 - Döner tip konteyner kilidi', 'A027 - Twist type container lock', '', '', '', '', '', 280.0, 'EUR', 270);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A028 - İskandinav tip konteyner kilidi', 'A028 - Scandinavian type container lock', '', '', '', '', '', 280.0, 'EUR', 280);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A029 - Marina tipi konteyner kilidi', 'A029 - Marine type container lock', '', '', '', '', '', 140.0, 'EUR', 290);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A030 - Marina tipi konteyner kilidi için slot', 'A030 - Slots for marine type container lock', '', '', '', '', '', 140.0, 'EUR', 300);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A031 - Deveboynu ve yükleme platformunu hizalamak için sehpa (kilitler ile uyumlu)', 'A031 - Desk for leveling the gooseneck and loading platform (compatible with locks)', '', '', '', '', '', 1750.0, 'EUR', 310);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A032 - Deveboynu ve yükleme plaftormu yanlarında baba yuvaları', 'A032 - Pillar pockets on the sides of the gooseneck and loading platform', '', '', '', '', '', 650.0, 'EUR', 320);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A033 - Yükleme platformu üzerinde baba yuvası grupları', 'A033 - Pillar pocket batches on the loading platform', '', '', '', '', '', 750.0, 'EUR', 330);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A034 - Sıcak daldırma galvaniz kaplı baba seti ve tutucuları', 'A034 - Hot dipped galvanized pillar set and their holder', '', '', '', '', '', 1250.0, 'EUR', 340);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A035 - 1 adet siyah boyalı çelik takım dolabı', 'A035 - 1 pc. black painted, steel tool box located under the loading platform', '', '', '', '', '', 350.0, 'EUR', 350);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A036 - 1 adet paslanmaz çelik (INOX) takım dolabı', 'A036 - 1pc. stainless steel tool box located under the loading platform', '', '', '', '', '', 600.0, 'EUR', 360);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A037 - Sıcak daldırma galvaniz kaplı açık ekipman dolabı', 'A037 - Hot dipped galvanized open storage box located under the loading platform', '', '', '', '', '', 600.0, 'EUR', 370);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A038 - Sıcak daldırma galvaniz kaplı platform genişliğinde açık ekipman dolabı', 'A038 - Hot dipped galvanized full wide open storage box', '', '', '', '', '', 1250.0, 'EUR', 380);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A039 - 1 adet paslanmaz çelik (INOX), deveboynu üstünde takım dolabı', 'A039 - 1 pc. stainless steel tool box located on the gooseneck', '', '', '', '', '', 1200.0, 'EUR', 390);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A040 - 15 ton kapasiteli, çift devirli hidrolik vinç ve uzaktan kumandası', 'A040 - 15 tons capacity, double speed, hydraulic winch with remote control', '', '', '', '', '', 5500.0, 'EUR', 400);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A041 - 13 ton kapasiteli elektrikli vinç ve uzaktan kumandası', 'A041 - 13 tons capacity, electric winch with remote control', '', '', '', '', '', 2150.0, 'EUR', 410);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A042 - 10 ton kapasiteli elektrikli vinç ve uzaktan kumandası', 'A042 - 10 tons capacity, electric winch with remote control', '', '', '', '', '', 1400.0, 'EUR', 420);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A043 - 8 ton kapasiteli elektrikli vinç ve uzaktan kumandası', 'A043 - 8 tons capacity, electric winch with remote control', '', '', '', '', '', 1200.0, 'EUR', 430);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A044 - 1 adet kalkar aks', 'A044 - 1 pc. lift axle', '', '', '', '', '', 800.0, 'EUR', 440);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A045 - 1 adet stepne ve tutucusu', 'A045 - 1 pc. spare wheel with its holder', '', '', '', '', '', 450.0, 'EUR', 450);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A046 - Alüminyum jantlar ALCOA DURA BRIGHT', 'A046 - Aluminium rims instead of steel rims ALCOA DURA BRIGHT', '', '', '', '', '', 550.0, 'EUR', 460);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A047 - Kilometre sayacı BPW, SAF veya JOST', 'A047 - Kilometer recorder BPW, SAF or JOST', '', '', '', '', '', 300.0, 'EUR', 470);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A048 - SAF akslar için lastik basıncı tamamlama sistemi 2 aks için', 'A048 - Tire pressure system only with SAF axles for 2 axles', '', '', '', '', '', 2300.0, 'EUR', 480);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A049 - SAF akslar için lastik basıncı tamamlama sistemi 3 aks için', 'A049 - Tire pressure system only with SAF axles for 3 axles', '', '', '', '', '', 2700.0, 'EUR', 490);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A050 - SAF akslar için lastik basıncı tamamlama sistemi 4 aks için', 'A050 - Tire pressure system only with SAF axles for 4 axles', '', '', '', '', '', 3100.0, 'EUR', 500);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A051 - SAF akslar için lastik basıncı tamamlama sistemi 5 aks için', 'A051 - Tire pressure system only with SAF axles for 5 axles', '', '', '', '', '', 3500.0, 'EUR', 510);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A052 - SAF akslar için lastik basıncı tamamlama sistemi 6 aks için', 'A052 - Tire pressure system only with SAF axles for 6 axles', '', '', '', '', '', 4500.0, 'EUR', 520);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A053 - Hidrolik veya elektro-hidrolik dümenleme sistemi TRIDEC veya VSE 2 aks için', 'A053 - Hydraulic or electro-hydraulic steering system TRIDEC or VSE for 2 axles', '', '', '', '', '', 33000.0, 'EUR', 530);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A054 - Hidrolik veya elektro-hidrolik dümenleme sistemi TRIDEC veya VSE 3 aks için', 'A054 - Hydraulic or electro-hydraulic steering system TRIDEC or VSE for 3 axles', '', '', '', '', '', 43000.0, 'EUR', 540);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A055 - Hidrolik veya elektro-hidrolik dümenleme sistemi TRIDEC veya VSE 4 aks için', 'A055 - Hydraulic or electro-hydraulic steering system TRIDEC or VSE for 4 axles', '', '', '', '', '', 51000.0, 'EUR', 550);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A056 - Hidrolik veya elektro-hidrolik dümenleme sistemi TRIDEC veya VSE 5 aks için', 'A056 - Hydraulic or electro-hydraulic steering system TRIDEC or VSE for 5 axles', '', '', '', '', '', 61000.0, 'EUR', 560);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A057 - Hidrolik veya elektro-hidrolik dümenleme sistemi TRIDEC veya VSE 6 aks için', 'A057 - Hydraulic or electro-hydraulic steering system TRIDEC or VSE for 6 axles', '', '', '', '', '', 71000.0, 'EUR', 570);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A058 - Hidrolik veya elektro-hidrolik dümenleme sistemi TRIDEC veya VSE 7 aks için', 'A058 - Hydraulic or electro-hydraulic steering system TRIDEC or VSE for 7 axles', '', '', '', '', '', 81000.0, 'EUR', 580);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A059 - Hava yerine hidrolik süspansiyon sistemi', 'A059 - Hydraulic suspension system instead of pneumatic suspension', '', '', '', '', '', 5000.0, 'EUR', 590);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A060 - 4 adet uzayabilir geniş yük uyarı levhası', 'A060 - 4 pcs. extendable overload marker boards', '', '', '', '', '', 1050.0, 'EUR', 600);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A061 - "CONVOI EXCEPTIONNEL" yazılı reflektif lamba', 'A061 - "CONVOI EXCEPTIONNEL" written reflective board', '', '', '', '', '', 450.0, 'EUR', 610);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A062 - 4 adet yerine 6 adet hamburger tipi stop lambası', 'A062 - 6 pcs. hamburger type stop lamps instead of 4 pcs.', '', '', '', '', '', 150.0, 'EUR', 620);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A063 - Tek pompalı otomatik yağlama sistemi GROENEVELD', 'A063 - Automatic lubrication system with single pump GROENEVELD', '', '', '', '', '', 2800.0, 'EUR', 630);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A064 - Çift pompalı otomatik yağlama sistemi GROENEVELD', 'A064 - Automatic lubrication system with double pump GROENEVELD', '', '', '', '', '', 3500.0, 'EUR', 640);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A065 - Smart Board WABCO', 'A065 - Smart Board WABCO', '', '', '', '', '', 400.0, 'EUR', 650);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A066 - Full şasi %85 çinko, %15 alüminyum karışımlı metalizasyon (15 yıl anti-pas garantili)', 'A066 - Full body metallization with 85% zinc, 15% aluminium (15 years anti-rust warranty)', '', '', '', '', '', 3000.0, 'EUR', 660);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A067 - Kısmi %85 çinko, %15 alüminyum karışımlı metalizasyon (15 yıl anti-pas garantili)', 'A067 - Partial metallization with 85% zinc, 15% aluminium (15 years anti-rust warranty)', '', '', '', '', '', 1000.0, 'EUR', 670);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A068 - Yükleme platformu kuyruğu altında hidrolik destek ayakları', 'A068 - Hydraulic support legs under the loading platform tail', '', '', '', '', '', 2250.0, 'EUR', 680);
        INSERT INTO "YLArabaslikDetaylar"
            ("GrupId","SatirMetni","SatirMetni_EN","SatirMetni_FR","SatirMetni_DE","SatirMetni_RO","SatirMetni_AR","SatirMetni_RU","Fiyat","ParaBirimi","SortOrder")
        VALUES (grup_id, 'A069 - Hidrolik deveboynu', 'A069 - Hydraulic gooseneck', '', '', '', '', '', 8900.0, 'EUR', 690);
    END IF;

END $$;

-- Sonuc kontrol:
-- SELECT "GrupAdi", COUNT(d."Id") FROM "YLArabaslikGruplar" g
--   LEFT JOIN "YLArabaslikDetaylar" d ON d."GrupId"=g."Id"
--   WHERE "CreatedBy" IN ('OM Manual Import','OM Auto-Seed') GROUP BY "GrupAdi";