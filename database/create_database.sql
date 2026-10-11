
/*
  Project: Resume-JD Matching and Skill Gap Analysis
  Database: ResumeMatcherDB
  Platform: Microsoft SQL Server

  Run in SSMS.
  This script preserves existing databases and tables.
*/

USE master;
GO

IF DB_ID(N'ResumeMatcherDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE ResumeMatcherDB');
END;
GO

USE ResumeMatcherDB;
GO

-- 1. Resumes
IF OBJECT_ID(N'dbo.Resumes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resumes
    (
        ResumeId BIGINT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        ExtractedText NVARCHAR(MAX) NOT NULL,
        UploadedAt DATETIME2 NOT NULL
            CONSTRAINT DF_Resumes_UploadedAt
            DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 2. Job descriptions
IF OBJECT_ID(N'dbo.JobDescriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobDescriptions
    (
        JobDescriptionId BIGINT IDENTITY(1,1) PRIMARY KEY,
        JobTitle NVARCHAR(255) NOT NULL,
        CompanyName NVARCHAR(255) NULL,
        DescriptionText NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_JobDescriptions_CreatedAt
            DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- 3. Normalized skill dictionary
IF OBJECT_ID(N'dbo.Skills', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Skills
    (
        SkillId BIGINT IDENTITY(1,1) PRIMARY KEY,
        SkillName NVARCHAR(150) NOT NULL UNIQUE,
        Category NVARCHAR(100) NULL
    );
END;
GO

-- 4. Skills identified in resumes
IF OBJECT_ID(N'dbo.ResumeSkills', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResumeSkills
    (
        ResumeId BIGINT NOT NULL,
        SkillId BIGINT NOT NULL,
        Confidence DECIMAL(5,4) NULL,

        CONSTRAINT PK_ResumeSkills
            PRIMARY KEY (ResumeId, SkillId),

        CONSTRAINT FK_ResumeSkills_Resumes
            FOREIGN KEY (ResumeId)
            REFERENCES dbo.Resumes(ResumeId)
            ON DELETE CASCADE,

        CONSTRAINT FK_ResumeSkills_Skills
            FOREIGN KEY (SkillId)
            REFERENCES dbo.Skills(SkillId),

        CONSTRAINT CK_ResumeSkills_Confidence
            CHECK (Confidence IS NULL OR
                   Confidence BETWEEN 0 AND 1)
    );
END;
GO

-- 5. Skills required or preferred by a job
IF OBJECT_ID(N'dbo.JobDescriptionSkills', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobDescriptionSkills
    (
        JobDescriptionId BIGINT NOT NULL,
        SkillId BIGINT NOT NULL,
        Importance NVARCHAR(20) NOT NULL
            CONSTRAINT DF_JobDescriptionSkills_Importance
            DEFAULT N'Required',

        CONSTRAINT PK_JobDescriptionSkills
            PRIMARY KEY (JobDescriptionId, SkillId),

        CONSTRAINT FK_JobDescriptionSkills_Jobs
            FOREIGN KEY (JobDescriptionId)
            REFERENCES dbo.JobDescriptions(JobDescriptionId)
            ON DELETE CASCADE,

        CONSTRAINT FK_JobDescriptionSkills_Skills
            FOREIGN KEY (SkillId)
            REFERENCES dbo.Skills(SkillId),

        CONSTRAINT CK_JobDescriptionSkills_Importance
            CHECK (Importance IN (N'Required', N'Preferred'))
    );
END;
GO

-- 6. Results from matching experiments
IF OBJECT_ID(N'dbo.MatchingResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MatchingResults
    (
        MatchingResultId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ResumeId BIGINT NOT NULL,
        JobDescriptionId BIGINT NOT NULL,

        TfIdfScore DECIMAL(7,6) NULL,
        SemanticScore DECIMAL(7,6) NULL,
        SkillCoverageScore DECIMAL(7,6) NULL,
        FinalScore DECIMAL(7,6) NULL,

        ModelVersion NVARCHAR(100) NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_MatchingResults_CreatedAt
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_MatchingResults_Resumes
            FOREIGN KEY (ResumeId)
            REFERENCES dbo.Resumes(ResumeId),

        CONSTRAINT FK_MatchingResults_Jobs
            FOREIGN KEY (JobDescriptionId)
            REFERENCES dbo.JobDescriptions(JobDescriptionId),

        CONSTRAINT CK_MatchingResults_Scores
            CHECK (
                (TfIdfScore IS NULL OR TfIdfScore BETWEEN 0 AND 1)
                AND
                (SemanticScore IS NULL OR SemanticScore BETWEEN 0 AND 1)
                AND
                (SkillCoverageScore IS NULL OR SkillCoverageScore BETWEEN 0 AND 1)
                AND
                (FinalScore IS NULL OR FinalScore BETWEEN 0 AND 1)
            )
    );
END;
GO

-- 7. Human-labelled evaluation pairs
IF OBJECT_ID(N'dbo.EvaluationPairs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EvaluationPairs
    (
        EvaluationPairId BIGINT IDENTITY(1,1) PRIMARY KEY,
        ResumeId BIGINT NOT NULL,
        JobDescriptionId BIGINT NOT NULL,
        HumanLabel SMALLINT NOT NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_EvaluationPairs_CreatedAt
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_EvaluationPairs_Resumes
            FOREIGN KEY (ResumeId)
            REFERENCES dbo.Resumes(ResumeId),

        CONSTRAINT FK_EvaluationPairs_Jobs
            FOREIGN KEY (JobDescriptionId)
            REFERENCES dbo.JobDescriptions(JobDescriptionId),

        CONSTRAINT CK_EvaluationPairs_HumanLabel
            CHECK (HumanLabel BETWEEN 0 AND 3),

        CONSTRAINT UQ_EvaluationPairs_Resume_Job
            UNIQUE (ResumeId, JobDescriptionId)
    );
END;
GO

-- 8. Indexes for common lookups
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MatchingResults_ResumeId'
      AND object_id = OBJECT_ID(N'dbo.MatchingResults')
)
BEGIN
    CREATE INDEX IX_MatchingResults_ResumeId
        ON dbo.MatchingResults(ResumeId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_MatchingResults_JobDescriptionId'
      AND object_id = OBJECT_ID(N'dbo.MatchingResults')
)
BEGIN
    CREATE INDEX IX_MatchingResults_JobDescriptionId
        ON dbo.MatchingResults(JobDescriptionId);
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_EvaluationPairs_HumanLabel'
      AND object_id = OBJECT_ID(N'dbo.EvaluationPairs')
)
BEGIN
    CREATE INDEX IX_EvaluationPairs_HumanLabel
        ON dbo.EvaluationPairs(HumanLabel);
END;
GO

-- 9. Seed initial technical skills
-- Existing skill names are not inserted again.
INSERT INTO dbo.Skills (SkillName, Category)
SELECT v.SkillName, v.Category
FROM (VALUES
    (N'Python', N'Programming'),
    (N'C#', N'Programming'),
    (N'Java', N'Programming'),
    (N'JavaScript', N'Programming'),
    (N'TypeScript', N'Programming'),
    (N'React', N'Framework'),
    (N'ASP.NET Core', N'Framework'),
    (N'Flask', N'Framework'),
    (N'SQL', N'Database'),
    (N'SQL Server', N'Database'),
    (N'PostgreSQL', N'Database'),
    (N'Docker', N'DevOps'),
    (N'Azure', N'Cloud'),
    (N'AWS', N'Cloud'),
    (N'Kubernetes', N'DevOps'),
    (N'Git', N'Tools'),
    (N'REST API', N'Backend'),
    (N'Machine Learning', N'AI/ML'),
    (N'Natural Language Processing', N'AI/ML'),
    (N'Scikit-learn', N'AI/ML'),
    (N'Sentence-BERT', N'AI/ML'),
    (N'spaCy', N'AI/ML')
) AS v(SkillName, Category)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Skills s
    WHERE s.SkillName = v.SkillName
);
GO

-- 10. Verify the tables
SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- Verify the seeded skills
SELECT SkillId, SkillName, Category
FROM dbo.Skills
ORDER BY SkillName;
GO