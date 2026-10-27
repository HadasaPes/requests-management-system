/*
    02_seed.sql - fills the database with 100,000 test requests.

    - Repeatable: clears both tables and resets identity seeds before inserting.
    - Fast: one set-based INSERT ... SELECT over a generated numbers sequence (no loops, no cursors).
    - Deterministic: "random" values come from an integer hash of the row number,
      so every run produces exactly the same data (only dates shift with the current time).

    Run against the API database, e.g.:
      sqlcmd -S "(localdb)\MSSQLLocalDB" -d RequestsManagementDb -i 02_seed.sql
*/

SET NOCOUNT ON;
SET XACT_ABORT ON; -- any error rolls back the whole transaction

DECLARE @RowCount  int          = 100000;
DECLARE @Now       datetime2(7) = SYSUTCDATETIME();  -- all dates are UTC
DECLARE @TwoYearsInSeconds int  = 730 * 24 * 60 * 60;
DECLARE @TwoPow32  bigint       = 4294967296;        -- keeps hash values in 32 bits
DECLARE @StartTime datetime2(7) = SYSUTCDATETIME();

------------------------------------------------------------------------------
-- Lookup lists. Ids are 0-based so a row picks an item with "hash % count".
------------------------------------------------------------------------------

-- ~50 company names
DECLARE @Organizations TABLE (Id int PRIMARY KEY, Name nvarchar(200) NOT NULL);
INSERT INTO @Organizations (Id, Name)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, Name
FROM (VALUES
    (N'Contoso Ltd'), (N'Fabrikam Inc'), (N'Northwind Traders'), (N'Adventure Works'), (N'Tailspin Toys'),
    (N'Wide World Importers'), (N'Litware Inc'), (N'Proseware Inc'), (N'Woodgrove Bank'), (N'Alpine Ski House'),
    (N'Blue Yonder Airlines'), (N'Coho Winery'), (N'Fourth Coffee'), (N'Graphic Design Institute'), (N'Humongous Insurance'),
    (N'Lucerne Publishing'), (N'Margie''s Travel'), (N'Southridge Video'), (N'Trey Research'), (N'Wingtip Toys'),
    (N'City Power & Light'), (N'Consolidated Messenger'), (N'First Up Consultants'), (N'Relecloud'), (N'VanArsdel Ltd'),
    (N'Bellows College'), (N'School of Fine Art'), (N'A. Datum Corporation'), (N'Lamna Healthcare'), (N'Munson''s Pickles'),
    (N'Nod Publishers'), (N'Olympia Logistics'), (N'Phoenix Medical Center'), (N'Redwood Engineering'), (N'Silverline Telecom'),
    (N'Summit Financial Group'), (N'Greenfield Foods'), (N'Harbor Freight Lines'), (N'Ironclad Security'), (N'Juniper Analytics'),
    (N'Keystone Construction'), (N'Lakeside Hospital'), (N'Maple Leaf Realty'), (N'Nimbus Cloud Services'), (N'Oakridge Pharmaceuticals'),
    (N'Pinnacle Legal Partners'), (N'Quantum Robotics'), (N'Riverbend Municipality'), (N'Sterling Retail Group'), (N'Unity Education Network')
) AS v(Name);

-- ~20 request types (first part of the title)
DECLARE @RequestTypes TABLE (Id int PRIMARY KEY, Name nvarchar(100) NOT NULL);
INSERT INTO @RequestTypes (Id, Name)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, Name
FROM (VALUES
    (N'Password reset'), (N'New laptop request'), (N'VPN access'), (N'Printer not working'), (N'Email quota increase'),
    (N'Software installation'), (N'Account locked'), (N'New employee onboarding'), (N'Employee offboarding'), (N'Network outage'),
    (N'Shared drive permissions'), (N'Phone replacement'), (N'Monitor request'), (N'Database access'), (N'Security incident'),
    (N'License renewal'), (N'Meeting room equipment'), (N'Backup restore'), (N'Website update'), (N'Invoice system error')
) AS v(Name);

-- ~10 short details (used instead of the organization name in some titles)
DECLARE @Details TABLE (Id int PRIMARY KEY, Name nvarchar(100) NOT NULL);
INSERT INTO @Details (Id, Name)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, Name
FROM (VALUES
    (N'Finance department'), (N'HR department'), (N'Sales team'), (N'Head office'), (N'Remote office'),
    (N'Building A, floor 2'), (N'Warehouse'), (N'Management'), (N'Customer support'), (N'Development team')
) AS v(Name);

-- ~20 handlers (AssignedTo)
DECLARE @Handlers TABLE (Id int PRIMARY KEY, Name nvarchar(100) NOT NULL);
INSERT INTO @Handlers (Id, Name)
SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, Name
FROM (VALUES
    (N'Dana Levi'), (N'Yossi Cohen'), (N'Noa Mizrahi'), (N'Avi Peretz'), (N'Michal Biton'),
    (N'Daniel Friedman'), (N'Tamar Shapiro'), (N'Eitan Katz'), (N'Shira Azoulay'), (N'Omer Ben David'),
    (N'Sarah Johnson'), (N'Michael Brown'), (N'Emily Davis'), (N'David Miller'), (N'Rachel Green'),
    (N'Ahmed Haddad'), (N'Lina Khoury'), (N'Ivan Petrov'), (N'Maria Garcia'), (N'Kenji Tanaka')
) AS v(Name);

DECLARE @OrgCount     int = (SELECT COUNT(*) FROM @Organizations);
DECLARE @TypeCount    int = (SELECT COUNT(*) FROM @RequestTypes);
DECLARE @DetailCount  int = (SELECT COUNT(*) FROM @Details);
DECLARE @HandlerCount int = (SELECT COUNT(*) FROM @Handlers);

BEGIN TRANSACTION;

------------------------------------------------------------------------------
-- Clear existing data and reset identity seeds (next Id = 1).
-- TRUNCATE is much faster than DELETE (no per-row logging or index maintenance)
-- and resets identity itself, but SQL Server refuses to truncate a table that
-- a foreign key points to. So the FK is dropped and re-created exactly as in
-- the InitialCreate migration. Everything runs inside the transaction.
------------------------------------------------------------------------------
ALTER TABLE dbo.RequestStatusHistory DROP CONSTRAINT FK_RequestStatusHistory_Requests_RequestId;

TRUNCATE TABLE dbo.RequestStatusHistory;
TRUNCATE TABLE dbo.Requests;

ALTER TABLE dbo.RequestStatusHistory ADD CONSTRAINT FK_RequestStatusHistory_Requests_RequestId
    FOREIGN KEY (RequestId) REFERENCES dbo.Requests (Id) ON DELETE CASCADE;

------------------------------------------------------------------------------
-- Insert all rows in one statement.
------------------------------------------------------------------------------
WITH
    -- Numbers 1..100,000: 10^5 rows from five cross joins of a 10-row list.
    Digits AS (SELECT d FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) AS v(d)),
    Numbers AS (
        SELECT TOP (@RowCount)
               CAST(a.d + b.d * 10 + c.d * 100 + e.d * 1000 + f.d * 10000 + 1 AS bigint) AS N
        FROM Digits a CROSS JOIN Digits b CROSS JOIN Digits c CROSS JOIN Digits e CROSS JOIN Digits f
        ORDER BY N
    ),
    -- Pseudo-random values: an integer hash of N (multiply + xor-shift, two rounds),
    -- with a different salt for each of the 5 values (X1..X5, each 0..2^32-1).
    -- Plain integer math is cheap even though SQL Server re-evaluates it wherever it is used.
    Mixed AS (
        SELECT n.N, x.X1, x.X2, x.X3, x.X4, x.X5
        FROM Numbers n
        CROSS APPLY (SELECT
            ((n.N + 1000003) * 73244475) % @TwoPow32 AS A1,
            ((n.N + 2000006) * 73244475) % @TwoPow32 AS A2,
            ((n.N + 3000009) * 73244475) % @TwoPow32 AS A3,
            ((n.N + 4000012) * 73244475) % @TwoPow32 AS A4,
            ((n.N + 5000015) * 73244475) % @TwoPow32 AS A5) a
        CROSS APPLY (SELECT
            ((a.A1 ^ (a.A1 / 65536)) * 73244475) % @TwoPow32 AS B1,
            ((a.A2 ^ (a.A2 / 65536)) * 73244475) % @TwoPow32 AS B2,
            ((a.A3 ^ (a.A3 / 65536)) * 73244475) % @TwoPow32 AS B3,
            ((a.A4 ^ (a.A4 / 65536)) * 73244475) % @TwoPow32 AS B4,
            ((a.A5 ^ (a.A5 / 65536)) * 73244475) % @TwoPow32 AS B5) b
        CROSS APPLY (SELECT
            b.B1 ^ (b.B1 / 65536) AS X1,
            b.B2 ^ (b.B2 / 65536) AS X2,
            b.B3 ^ (b.B3 / 65536) AS X3,
            b.B4 ^ (b.B4 / 65536) AS X4,
            b.B5 ^ (b.B5 / 65536) AS X5) x
    ),
    -- Split the hashes into the values each column needs.
    Randoms AS (
        SELECT N,
               X1 % 65536 AS R1,  -- organization
               X1 / 65536 AS R2,  -- request type
               X2 % 65536 AS R3,  -- title variant + detail
               X2 / 65536 AS R4,  -- status
               X3 % 65536 AS R5,  -- priority
               X3 / 65536 AS R6,  -- handler + unassigned flag
               X4         AS R7,  -- CreatedAt
               X5         AS R8   -- UpdatedAt
        FROM Mixed
    ),
    Generated AS (
        SELECT r.*,
               -- Status mix: New 35%, InProgress 30%, Waiting 20%, Completed 15%.
               CAST(CASE
                        WHEN r.R4 % 100 < 35 THEN 0
                        WHEN r.R4 % 100 < 65 THEN 1
                        WHEN r.R4 % 100 < 85 THEN 2
                        ELSE 3
                    END AS tinyint) AS Status,
               -- Priority mix: Low 30%, Medium 45%, High 25%.
               CAST(CASE
                        WHEN r.R5 % 100 < 30 THEN 0
                        WHEN r.R5 % 100 < 75 THEN 1
                        ELSE 2
                    END AS tinyint) AS Priority,
               -- Created at a random second within the last 2 years.
               CAST(r.R7 % @TwoYearsInSeconds AS int) AS CreatedSecondsAgo
        FROM Randoms r
    )
INSERT INTO dbo.Requests (Title, OrganizationName, Status, Priority, AssignedTo, CreatedAt, UpdatedAt)
SELECT
    -- Title: "<type> - <organization>" or "<type> (<detail>)".
    CASE WHEN g.R3 % 2 = 0
         THEN t.Name + N' - ' + o.Name
         ELSE t.Name + N' (' + d.Name + N')'
    END,
    o.Name,
    g.Status,
    g.Priority,
    -- About 40% of New requests are not assigned yet.
    CASE WHEN g.Status = 0 AND g.R6 % 100 < 40 THEN NULL ELSE hd.Name END,
    DATEADD(SECOND, -g.CreatedSecondsAgo, @Now),
    -- New requests were never updated; others were updated somewhere between CreatedAt and now.
    CASE WHEN g.Status = 0
         THEN DATEADD(SECOND, -g.CreatedSecondsAgo, @Now)
         ELSE DATEADD(SECOND, -(g.CreatedSecondsAgo - CAST(g.R8 % (g.CreatedSecondsAgo + 1) AS int)), @Now)
    END
FROM Generated g
JOIN @Organizations o  ON o.Id  = g.R1 % @OrgCount
JOIN @RequestTypes  t  ON t.Id  = g.R2 % @TypeCount
JOIN @Details       d  ON d.Id  = (g.R3 / 2) % @DetailCount
JOIN @Handlers      hd ON hd.Id = g.R6 % @HandlerCount
ORDER BY g.N; -- keeps Ids in the same order as N, so reruns produce identical rows

COMMIT TRANSACTION;

PRINT CONCAT(N'Inserted ', @RowCount, N' requests in ',
             DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()), N' ms.');

-- Rebuild all indexes on Requests. The rows are inserted in Id (clustered) order, so every nonclustered
-- index receives its keys in random order and splits pages: right after the insert they are 93-99% fragmented
-- and about twice their needed size (see docs/performance.md). A rebuild makes them compact again, so the
-- measurements and the API read fewer pages.
DECLARE @RebuildStart datetime2 = SYSUTCDATETIME();
ALTER INDEX ALL ON dbo.Requests REBUILD;
PRINT CONCAT(N'Rebuilt indexes in ', DATEDIFF(MILLISECOND, @RebuildStart, SYSUTCDATETIME()), N' ms.');
