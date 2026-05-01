-- ============================================================
-- Migration: YLLookupValues — TIP_ADI degerleri eklendi
-- Tarih     : 2026-04-30
-- Aciklama  : Teklif formundaki "Tip Adi" dropdown secenekleri.
--             Uygulama ilk acildiginda EnsureSchema bu kayitlari
--             otomatik ekler. Manuel calistirmak gerekirse:
-- ============================================================
INSERT INTO "YLLookupValues" ("LookupType", "Value", "SortOrder")
VALUES
    ('TIP_ADI', '1DDUZ',  10),
    ('TIP_ADI', '2DDUZ',  20),
    ('TIP_ADI', '2PAUZ',  30),
    ('TIP_ADI', '2TAUZ',  40),
    ('TIP_ADI', '2YSFC',  50),
    ('TIP_ADI', '3LBUZ',  60),
    ('TIP_ADI', '3PAUZ',  70),
    ('TIP_ADI', '3TAUZ',  80),
    ('TIP_ADI', '3YSFC',  90),
    ('TIP_ADI', '4DDUZ', 100),
    ('TIP_ADI', '4PAUZ', 110),
    ('TIP_ADI', '4TAUZ', 120),
    ('TIP_ADI', '4YSFC', 130),
    ('TIP_ADI', '5LBUZ', 140),
    ('TIP_ADI', '5PAUZ', 150),
    ('TIP_ADI', '5YSFC', 160),
    ('TIP_ADI', '6DDUZ', 170),
    ('TIP_ADI', '6LBUZ', 180),
    ('TIP_ADI', '6PAUZ', 190),
    ('TIP_ADI', '7DDUZ', 200),
    ('TIP_ADI', '7LBUZ', 210),
    ('TIP_ADI', '8DDUZ', 220),
    ('TIP_ADI', '8LWUZ', 230),
    ('TIP_ADI', '8PAUZ', 240),
    ('TIP_ADI', '9DDUZ', 250),
    ('TIP_ADI', '10DUZ', 260),
    ('TIP_ADI', '11DUZ', 270),
    ('TIP_ADI', '12DUZ', 280)
ON CONFLICT ("LookupType", "Value") DO NOTHING;
