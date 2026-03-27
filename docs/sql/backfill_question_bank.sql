/*
Backfill Question.QuestionBankId for legacy data.

How to run:
1) Set @ApplyChanges = 0 to preview (ROLLBACK), then review output.
2) Set @ApplyChanges = 1 to commit.
3) Optional: set @FallbackCourseId (e.g. 'course-csharp') to assign unresolved rows.
*/

SET NOCOUNT ON;

DECLARE @ApplyChanges BIT = 0;
DECLARE @FallbackCourseId NVARCHAR(450) = NULL; -- Example: N'course-csharp'
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

IF @FallbackCourseId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM Course WHERE IsDeleted = 0 AND Id = @FallbackCourseId)
BEGIN
    THROW 50001, 'FallbackCourseId does not exist or has been deleted.', 1;
END

BEGIN TRANSACTION;

-- 1) Ensure QuestionBank exists for mapped courses (Subject -> Course.SubjectArea exact match by Subject.Id/Subject.Name)
;WITH CandidateCourse AS
(
    SELECT DISTINCT c.Id, c.Name
    FROM Question q
    JOIN Subject s ON s.Id = q.SubjectId AND s.IsDeleted = 0
    JOIN Course c ON c.IsDeleted = 0
        AND (
            LOWER(LTRIM(RTRIM(c.SubjectArea))) = LOWER(LTRIM(RTRIM(s.Id)))
            OR LOWER(LTRIM(RTRIM(c.SubjectArea))) = LOWER(LTRIM(RTRIM(s.Name)))
        )
    WHERE q.IsDeleted = 0
      AND (q.QuestionBankId IS NULL OR LTRIM(RTRIM(q.QuestionBankId)) = N'')
)
INSERT INTO QuestionBank (Id, CourseId, Name, CreatedBy, CreatedAt, IsDeleted)
SELECT
    LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')) AS Id,
    cc.Id AS CourseId,
    CONCAT(N'AutoBank - ', cc.Name) AS Name,
    N'system-backfill' AS CreatedBy,
    @Now AS CreatedAt,
    0 AS IsDeleted
FROM CandidateCourse cc
WHERE NOT EXISTS
(
    SELECT 1
    FROM QuestionBank qb
    WHERE qb.IsDeleted = 0
      AND qb.CourseId = cc.Id
);

-- 2) Ensure fallback QuestionBank exists (optional)
IF @FallbackCourseId IS NOT NULL
BEGIN
    INSERT INTO QuestionBank (Id, CourseId, Name, CreatedBy, CreatedAt, IsDeleted)
    SELECT
        LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N'')),
        c.Id,
        CONCAT(N'AutoBank - ', c.Name),
        N'system-backfill',
        @Now,
        0
    FROM Course c
    WHERE c.IsDeleted = 0
      AND c.Id = @FallbackCourseId
      AND NOT EXISTS
      (
          SELECT 1
          FROM QuestionBank qb
          WHERE qb.IsDeleted = 0
            AND qb.CourseId = c.Id
      );
END

-- 3) Backfill by unique mapping Subject -> Course
;WITH SubjectCourseMatch AS
(
    SELECT
        s.Id AS SubjectId,
        c.Id AS CourseId,
        COUNT(*) OVER (PARTITION BY s.Id) AS MatchCount,
        ROW_NUMBER() OVER
        (
            PARTITION BY s.Id
            ORDER BY
                CASE
                    WHEN LOWER(LTRIM(RTRIM(c.SubjectArea))) = LOWER(LTRIM(RTRIM(s.Id))) THEN 0
                    ELSE 1
                END,
                c.Id
        ) AS RowNum
    FROM Subject s
    JOIN Course c ON c.IsDeleted = 0
        AND (
            LOWER(LTRIM(RTRIM(c.SubjectArea))) = LOWER(LTRIM(RTRIM(s.Id)))
            OR LOWER(LTRIM(RTRIM(c.SubjectArea))) = LOWER(LTRIM(RTRIM(s.Name)))
        )
    WHERE s.IsDeleted = 0
),
UniqueSubjectCourse AS
(
    SELECT SubjectId, CourseId
    FROM SubjectCourseMatch
    WHERE MatchCount = 1 AND RowNum = 1
)
UPDATE q
SET q.QuestionBankId = qbPick.Id
FROM Question q
JOIN UniqueSubjectCourse usc ON usc.SubjectId = q.SubjectId
CROSS APPLY
(
    SELECT TOP 1 qb.Id
    FROM QuestionBank qb
    WHERE qb.IsDeleted = 0
      AND qb.CourseId = usc.CourseId
    ORDER BY qb.CreatedAt, qb.Id
) qbPick
WHERE q.IsDeleted = 0
  AND (q.QuestionBankId IS NULL OR LTRIM(RTRIM(q.QuestionBankId)) = N'');

-- 4) Fallback unresolved rows to one course (optional)
IF @FallbackCourseId IS NOT NULL
BEGIN
    DECLARE @FallbackBankId NVARCHAR(450);
    SELECT TOP 1 @FallbackBankId = qb.Id
    FROM QuestionBank qb
    WHERE qb.IsDeleted = 0
      AND qb.CourseId = @FallbackCourseId
    ORDER BY qb.CreatedAt, qb.Id;

    UPDATE Question
    SET QuestionBankId = @FallbackBankId
    WHERE IsDeleted = 0
      AND (QuestionBankId IS NULL OR LTRIM(RTRIM(QuestionBankId)) = N'');
END

-- 5) Summary
SELECT
    COUNT(*) AS TotalQuestions,
    SUM(CASE WHEN QuestionBankId IS NULL OR LTRIM(RTRIM(QuestionBankId)) = N'' THEN 1 ELSE 0 END) AS RemainingWithoutQuestionBank
FROM Question
WHERE IsDeleted = 0;

SELECT
    qb.CourseId,
    c.Name AS CourseName,
    COUNT(*) AS QuestionCount
FROM Question q
JOIN QuestionBank qb ON qb.Id = q.QuestionBankId AND qb.IsDeleted = 0
LEFT JOIN Course c ON c.Id = qb.CourseId
WHERE q.IsDeleted = 0
GROUP BY qb.CourseId, c.Name
ORDER BY QuestionCount DESC;

IF @ApplyChanges = 1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'Committed backfill changes.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Preview mode only (rolled back). Set @ApplyChanges = 1 to commit.';
END

