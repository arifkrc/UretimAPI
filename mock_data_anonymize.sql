-- ============================================================
-- Mock Data Anonymization Script
-- Tüm string veriler anonim mock değerlere dönüştürülür.
-- Sayısal veriler (adet, miktar, vb.) değiştirilmez.
-- SSMS veya Azure Data Studio ile çalıştırınız.
-- ============================================================

BEGIN TRANSACTION;

-- ============================================================
-- 1. ProductCode FK kısıtlarını geçici devre dışı bırak
-- ============================================================
ALTER TABLE ProductionTrackingForms NOCHECK CONSTRAINT FK_ProductionTrackingForms_Products_ProductCode;
ALTER TABLE Packings NOCHECK CONSTRAINT FK_Packings_Products_ProductCode;

-- ============================================================
-- 2. ProductCode eşleme tablosu (OldCode → URUN-NNN)
-- ============================================================
SELECT
    ProductCode AS OldCode,
    'URUN-' + FORMAT(ROW_NUMBER() OVER (ORDER BY Id), '000') AS NewCode
INTO #ProductCodeMap
FROM Products;

-- ProductionTrackingForms.ProductCode güncelle
UPDATE ptf
SET ptf.ProductCode = m.NewCode
FROM ProductionTrackingForms ptf
JOIN #ProductCodeMap m ON ptf.ProductCode = m.OldCode;

-- Packings.ProductCode güncelle
UPDATE pk
SET pk.ProductCode = m.NewCode
FROM Packings pk
JOIN #ProductCodeMap m ON pk.ProductCode = m.OldCode;

-- Orders.ProductCode güncelle (FK değil ama tutarlı olsun)
UPDATE o
SET o.ProductCode = m.NewCode
FROM Orders o
JOIN #ProductCodeMap m ON o.ProductCode = m.OldCode;

-- Products tablosunu güncelle
UPDATE p
SET
    p.ProductCode  = m.NewCode,
    p.Name         = 'Ürün ' + SUBSTRING(m.NewCode, 6, 10),
    p.Description  = 'Mock açıklama'
FROM Products p
JOIN #ProductCodeMap m ON p.ProductCode = m.OldCode;

-- Products.Type kategorilere ayır
WITH ProductsRanked AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM Products
)
UPDATE p
SET p.Type = CASE (r.rn % 3)
                WHEN 0 THEN 'Tip-A'
                WHEN 1 THEN 'Tip-B'
                ELSE        'Tip-C'
             END
FROM Products p
JOIN ProductsRanked r ON p.Id = r.Id;

-- FK kısıtlarını yeniden etkinleştir ve doğrula
ALTER TABLE ProductionTrackingForms WITH CHECK CHECK CONSTRAINT FK_ProductionTrackingForms_Products_ProductCode;
ALTER TABLE Packings WITH CHECK CHECK CONSTRAINT FK_Packings_Products_ProductCode;

DROP TABLE #ProductCodeMap;

-- ============================================================
-- 3. Operations: isim ve kısa kod
-- ============================================================
WITH OperationsRanked AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM Operations
)
UPDATE o
SET
    o.Name      = 'Operasyon-' + FORMAT(r.rn, '00'),
    o.ShortCode = 'OP' + FORMAT(r.rn, '00')
FROM Operations o
JOIN OperationsRanked r ON o.Id = r.Id;

-- ============================================================
-- 4. Orders: müşteri adı, belge no, varyant
-- ============================================================
WITH OrdersRanked AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM Orders
)
UPDATE o
SET
    o.DocumentNo = 'DOC-' + FORMAT(r.rn, '0000'),
    o.Customer   = 'Musteri-' + FORMAT(r.rn, '000'),
    o.Variants   = 'Varyant-' + FORMAT(r.rn, '000')
FROM Orders o
JOIN OrdersRanked r ON o.Id = r.Id;

-- ============================================================
-- 5. Packings: denetçi (Supervisor)
-- ============================================================
SELECT DISTINCT
    Supervisor AS OldName,
    'Calisan-' + FORMAT(ROW_NUMBER() OVER (ORDER BY Supervisor), '00') AS NewName
INTO #PackingSupervisorMap
FROM Packings
WHERE Supervisor IS NOT NULL;

UPDATE pk
SET pk.Supervisor = sm.NewName
FROM Packings pk
JOIN #PackingSupervisorMap sm ON pk.Supervisor = sm.OldName;

DROP TABLE #PackingSupervisorMap;

-- ============================================================
-- 6. ProductionTrackingForms: kişi adları (ortak mapping)
--    ShiftSupervisor, OperatorName, SectionSupervisor
--    aynı gerçek isim → aynı mock isim
-- ============================================================
SELECT DISTINCT PersonName
INTO #AllPersonNames
FROM (
    SELECT ShiftSupervisor  AS PersonName FROM ProductionTrackingForms WHERE ShiftSupervisor  IS NOT NULL
    UNION
    SELECT OperatorName                   FROM ProductionTrackingForms WHERE OperatorName     IS NOT NULL
    UNION
    SELECT SectionSupervisor              FROM ProductionTrackingForms WHERE SectionSupervisor IS NOT NULL
) n;

SELECT
    PersonName AS OldName,
    'Calisan-' + FORMAT(ROW_NUMBER() OVER (ORDER BY PersonName), '00') AS NewName
INTO #PersonNameMap
FROM #AllPersonNames;

UPDATE ptf SET ptf.ShiftSupervisor   = nm.NewName FROM ProductionTrackingForms ptf JOIN #PersonNameMap nm ON ptf.ShiftSupervisor   = nm.OldName;
UPDATE ptf SET ptf.OperatorName      = nm.NewName FROM ProductionTrackingForms ptf JOIN #PersonNameMap nm ON ptf.OperatorName     = nm.OldName;
UPDATE ptf SET ptf.SectionSupervisor = nm.NewName FROM ProductionTrackingForms ptf JOIN #PersonNameMap nm ON ptf.SectionSupervisor = nm.OldName;

DROP TABLE #AllPersonNames;
DROP TABLE #PersonNameMap;

-- ============================================================
-- 7. ProductionTrackingForms: makine adları
-- ============================================================
SELECT DISTINCT
    Machine AS OldName,
    'Makine-' + FORMAT(ROW_NUMBER() OVER (ORDER BY Machine), '00') AS NewName
INTO #MachineMap
FROM ProductionTrackingForms
WHERE Machine IS NOT NULL;

UPDATE ptf
SET ptf.Machine = mm.NewName
FROM ProductionTrackingForms ptf
JOIN #MachineMap mm ON ptf.Machine = mm.OldName;

DROP TABLE #MachineMap;

-- ============================================================
-- 8. ProductionTrackingForms: hat (Line)
-- ============================================================
SELECT DISTINCT
    Line AS OldName,
    'Hat-' + FORMAT(ROW_NUMBER() OVER (ORDER BY Line), '00') AS NewName
INTO #LineMap
FROM ProductionTrackingForms
WHERE Line IS NOT NULL;

UPDATE ptf
SET ptf.Line = lm.NewName
FROM ProductionTrackingForms ptf
JOIN #LineMap lm ON ptf.Line = lm.OldName;

DROP TABLE #LineMap;

-- ============================================================
-- Tamamlandı
-- ============================================================
COMMIT TRANSACTION;

PRINT 'Mock data anonymization completed successfully.';
