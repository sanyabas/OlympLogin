SELECT TOP 100 * FROM kladr.dbo.Territory
WHERE NAME='Свердловская'
GO

SELECT TOP 100 * FROM Abbreviation
GO

CREATE VIEW Regions
AS
SELECT DISTINCT
Territory.Name as Name,
Abbreviation.FullName as Type,
Territory.Code as Code
FROM Territory
INNER JOIN dbo.Abbreviation ON Territory.Abbreviation=Abbreviation.ShortName AND Abbreviation.Level=1
WHERE Territory.Code LIKE '%00000000000'
GO

SELECT * FROM Regions
GO