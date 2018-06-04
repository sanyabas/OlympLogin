ALTER TABLE Street
ALTER COLUMN Code VARCHAR(17) NOT NULL
GO

ALTER TABLE Street
ADD CONSTRAINT PK_Street_Code PRIMARY KEY (Code)
GO

ALTER TABLE Territory
ALTER COLUMN Code VARCHAR(13) NOT NULL
GO

ALTER TABLE Territory
ADD CONSTRAINT PK_Territory_Code PRIMARY KEY (Code)
GO

ALTER TABLE Abbreviation
ALTER COLUMN [Level] VARCHAR(5) NOT NULL
GO

ALTER TABLE Abbreviation
ALTER COLUMN ShortName VARCHAR(10) NOT NULL
GO

ALTER TABLE Abbreviation
ADD CONSTRAINT PK_Abbreviation_Code PRIMARY KEY ([Level], ShortName)
GO

ALTER TABLE dbo.Building
ALTER COLUMN Code varchar(19) not null
GO

alter table Building
add constraint PK_Building_Code primary key(Code)
GO

Drop table dbo.Users
GO

CREATE TABLE dbo.Users(
Id INT IDENTITY(1,1) PRIMARY KEY,
[Login] VARCHAR(50),
[Password] VARCHAR(50),
[Role] INT,
LastName NVARCHAR(100),
FirstName NVARCHAR(100),
MiddleName NVARCHAR(100),
[Address] NVARCHAR(200),
[Index] VARCHAR(6),
TerritoryCode VARCHAR(13) NOT NULL REFERENCES dbo.Territory(Code),
StreetCode VARCHAR(17) NOT NULL REFERENCES dbo.Street(Code),
Building INT,
BuilddingCode VARCHAR(20),
Flat INT,
)
GO
