# Database — Resume-JD Matching System

## Database Technology

* Microsoft SQL Server
* SQL Server Management Studio (SSMS)

## Database Name

`ResumeMatcherDB`

## Tables

* Resumes
* JobDescriptions
* Skills
* ResumeSkills
* JobDescriptionSkills
* MatchingResults
* EvaluationPairs

## Setup

1. Open `01_create_database.sql` in SQL Server Management Studio.
2. Connect to your SQL Server instance.
3. Execute the script.
4. Verify that the database and tables were created successfully.

## Evaluation Labels

* 0 — Poor match
* 1 — Weak match
* 2 — Moderate match
* 3 — Strong match

The database stores resume and job-description metadata, extracted skills, matching scores, and human-labelled evaluation pairs.

Note: Actual resume data and personally identifiable information should not be committed to the repository.
