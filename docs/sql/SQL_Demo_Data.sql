/*
SQL Demo Data for UniTestSystem
Target DB: UniTestSystemDb (SQL Server LocalDB)

What this script does:
1) Cleanup previous demo data (targeted cleanup)
2) Insert a full demo dataset for end-to-end testing
3) Run verification queries

Notes:
- All IDs are 32-char hex strings.
- Time fields use GETUTCDATE().
- PasswordHash uses BCrypt hashes.
- Soft-delete fields are set to 0.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [UniTestSystemDb];

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now DATETIME2 = GETUTCDATE();

    -----------------------------------------------------------------------------
    -- 0) IDs
    -----------------------------------------------------------------------------

    -- Faculties
    DECLARE @FacultyCNTT NVARCHAR(450)   = N'10000000000000000000000000000001';
    DECLARE @FacultyKT NVARCHAR(450)     = N'10000000000000000000000000000002';
    DECLARE @FacultyKTE NVARCHAR(450)    = N'10000000000000000000000000000003';

    -- Student classes
    DECLARE @ClassCNTT1 NVARCHAR(450)    = N'11000000000000000000000000000001';
    DECLARE @ClassCNTT2 NVARCHAR(450)    = N'11000000000000000000000000000002';
    DECLARE @ClassKT1 NVARCHAR(450)      = N'11000000000000000000000000000003';
    DECLARE @ClassKTE1 NVARCHAR(450)     = N'11000000000000000000000000000004';

    -- Users
    DECLARE @AdminId NVARCHAR(450)       = (SELECT TOP 1 Id FROM [User] WHERE Email = N'admin@unitest.com' AND IsDeleted = 0);
    IF @AdminId IS NULL SET @AdminId = N'12000000000000000000000000000001';

    DECLARE @Staff1Id NVARCHAR(450)      = N'12000000000000000000000000000002';
    DECLARE @Lecturer1Id NVARCHAR(450)   = N'12000000000000000000000000000003';
    DECLARE @Lecturer2Id NVARCHAR(450)   = N'12000000000000000000000000000004';
    DECLARE @Student1Id NVARCHAR(450)    = N'12000000000000000000000000000005';
    DECLARE @Student2Id NVARCHAR(450)    = N'12000000000000000000000000000006';
    DECLARE @Student3Id NVARCHAR(450)    = N'12000000000000000000000000000007';
    DECLARE @Student4Id NVARCHAR(450)    = N'12000000000000000000000000000008';

    -- Courses
    DECLARE @Course1 NVARCHAR(450)       = N'13000000000000000000000000000001';
    DECLARE @Course2 NVARCHAR(450)       = N'13000000000000000000000000000002';
    DECLARE @Course3 NVARCHAR(450)       = N'13000000000000000000000000000003';
    DECLARE @Course4 NVARCHAR(450)       = N'13000000000000000000000000000004';
    DECLARE @Course5 NVARCHAR(450)       = N'13000000000000000000000000000005';

    -- Enrollments (7)
    DECLARE @Enroll1 NVARCHAR(450)       = N'14000000000000000000000000000001';
    DECLARE @Enroll2 NVARCHAR(450)       = N'14000000000000000000000000000002';
    DECLARE @Enroll3 NVARCHAR(450)       = N'14000000000000000000000000000003';
    DECLARE @Enroll4 NVARCHAR(450)       = N'14000000000000000000000000000004';
    DECLARE @Enroll5 NVARCHAR(450)       = N'14000000000000000000000000000005';
    DECLARE @Enroll6 NVARCHAR(450)       = N'14000000000000000000000000000006';
    DECLARE @Enroll7 NVARCHAR(450)       = N'14000000000000000000000000000007';

    -- Metadata
    DECLARE @SubjectDB NVARCHAR(450)     = N'15000000000000000000000000000001';
    DECLARE @SubjectSE NVARCHAR(450)     = N'15000000000000000000000000000002';
    DECLARE @SubjectACC NVARCHAR(450)    = N'15000000000000000000000000000003';

    DECLARE @SkillSQL NVARCHAR(450)      = N'15100000000000000000000000000001';
    DECLARE @SkillCode NVARCHAR(450)     = N'15100000000000000000000000000002';
    DECLARE @SkillTheory NVARCHAR(450)   = N'15100000000000000000000000000003';

    DECLARE @DiffEasy NVARCHAR(450)      = N'15200000000000000000000000000001';
    DECLARE @DiffMedium NVARCHAR(450)    = N'15200000000000000000000000000002';
    DECLARE @DiffHard NVARCHAR(450)      = N'15200000000000000000000000000003';

    -- Question bank
    DECLARE @QBank1 NVARCHAR(450)        = N'16000000000000000000000000000001';
    DECLARE @QBank2 NVARCHAR(450)        = N'16000000000000000000000000000002';
    DECLARE @QBank3 NVARCHAR(450)        = N'16000000000000000000000000000003';

    -- Questions (14)
    DECLARE @Q01 NVARCHAR(450)           = N'17000000000000000000000000000001';
    DECLARE @Q02 NVARCHAR(450)           = N'17000000000000000000000000000002';
    DECLARE @Q03 NVARCHAR(450)           = N'17000000000000000000000000000003';
    DECLARE @Q04 NVARCHAR(450)           = N'17000000000000000000000000000004';
    DECLARE @Q05 NVARCHAR(450)           = N'17000000000000000000000000000005';
    DECLARE @Q06 NVARCHAR(450)           = N'17000000000000000000000000000006';
    DECLARE @Q07 NVARCHAR(450)           = N'17000000000000000000000000000007';
    DECLARE @Q08 NVARCHAR(450)           = N'17000000000000000000000000000008';
    DECLARE @Q09 NVARCHAR(450)           = N'17000000000000000000000000000009';
    DECLARE @Q10 NVARCHAR(450)           = N'17000000000000000000000000000010';
    DECLARE @Q11 NVARCHAR(450)           = N'17000000000000000000000000000011';
    DECLARE @Q12 NVARCHAR(450)           = N'17000000000000000000000000000012';
    DECLARE @Q13 NVARCHAR(450)           = N'17000000000000000000000000000013';
    DECLARE @Q14 NVARCHAR(450)           = N'17000000000000000000000000000014';

    -- Tests (4)
    DECLARE @Test1 NVARCHAR(450)         = N'18000000000000000000000000000001';
    DECLARE @Test2 NVARCHAR(450)         = N'18000000000000000000000000000002';
    DECLARE @Test3 NVARCHAR(450)         = N'18000000000000000000000000000003';
    DECLARE @Test4 NVARCHAR(450)         = N'18000000000000000000000000000004';

    -- Exam schedules (4)
    DECLARE @Schedule1 NVARCHAR(450)     = N'19000000000000000000000000000001';
    DECLARE @Schedule2 NVARCHAR(450)     = N'19000000000000000000000000000002';
    DECLARE @Schedule3 NVARCHAR(450)     = N'19000000000000000000000000000003';
    DECLARE @Schedule4 NVARCHAR(450)     = N'19000000000000000000000000000004';

    -- Sessions (5)
    DECLARE @Session1 NVARCHAR(450)      = N'1a000000000000000000000000000001';
    DECLARE @Session2 NVARCHAR(450)      = N'1a000000000000000000000000000002';
    DECLARE @Session3 NVARCHAR(450)      = N'1a000000000000000000000000000003';
    DECLARE @Session4 NVARCHAR(450)      = N'1a000000000000000000000000000004';
    DECLARE @Session5 NVARCHAR(450)      = N'1a000000000000000000000000000005';

    -- Results (4)
    DECLARE @Result1 NVARCHAR(450)       = N'1b000000000000000000000000000001';
    DECLARE @Result2 NVARCHAR(450)       = N'1b000000000000000000000000000002';
    DECLARE @Result3 NVARCHAR(450)       = N'1b000000000000000000000000000003';
    DECLARE @Result4 NVARCHAR(450)       = N'1b000000000000000000000000000004';

    -- Transcripts (4)
    DECLARE @Transcript1 NVARCHAR(450)   = N'1c000000000000000000000000000001';
    DECLARE @Transcript2 NVARCHAR(450)   = N'1c000000000000000000000000000002';
    DECLARE @Transcript3 NVARCHAR(450)   = N'1c000000000000000000000000000003';
    DECLARE @Transcript4 NVARCHAR(450)   = N'1c000000000000000000000000000004';

    -- Feedbacks (3)
    DECLARE @Feedback1 NVARCHAR(450)     = N'1d000000000000000000000000000001';
    DECLARE @Feedback2 NVARCHAR(450)     = N'1d000000000000000000000000000002';
    DECLARE @Feedback3 NVARCHAR(450)     = N'1d000000000000000000000000000003';

    -- Password hashes
    -- Admin123!, Staff@123, Lecturer@123, Student@123
    DECLARE @HashAdmin NVARCHAR(MAX)     = N'$2a$11$NS4Cys3SBNpUjw95KUwntu2cAqE1teCe3jmh4EDBPdj5ZWS7W/rZC';
    DECLARE @HashStaff NVARCHAR(MAX)     = N'$2a$11$zQDCjJPics0XyfKqBjjNieB9MQMkD162sF9zSYy4uI87NWivlJFdG';
    DECLARE @HashLecturer NVARCHAR(MAX)  = N'$2a$11$qpWPTWxpP2Gu0XDMaIWj5.BfOUnMr/dHcZZSgTuBErAC0u2tFUoj2';
    DECLARE @HashStudent NVARCHAR(MAX)   = N'$2a$11$u19vcsegK6p.Sz8gqir9pOd1H0tT6tla0QMXh/Aam8CIkacR9n7Sm';

    -----------------------------------------------------------------------------
    -- 1) Cleanup section (targeted)
    -----------------------------------------------------------------------------

    DELETE FROM [StudentAnswer]
    WHERE SessionId IN (@Session1, @Session2, @Session3, @Session4, @Session5)
       OR QuestionId IN (@Q01, @Q02, @Q03, @Q04, @Q05, @Q06, @Q07, @Q08, @Q09, @Q10, @Q11, @Q12, @Q13, @Q14);

    DELETE FROM [Feedback]
    WHERE Id IN (@Feedback1, @Feedback2, @Feedback3)
       OR SessionId IN (@Session1, @Session2, @Session3, @Session4, @Session5);

    DELETE FROM [Result]
    WHERE Id IN (@Result1, @Result2, @Result3, @Result4)
       OR SessionId IN (@Session1, @Session2, @Session3, @Session4)
       OR TestId IN (@Test1, @Test2, @Test3, @Test4);

    DELETE FROM [SessionQuestionSnapshot]
    WHERE SessionId IN (@Session1, @Session2, @Session3, @Session4, @Session5);

    DELETE FROM [SessionLog]
    WHERE SessionId IN (@Session1, @Session2, @Session3, @Session4, @Session5);

    DELETE FROM [DeviceFingerprint]
    WHERE SessionId IN (@Session1, @Session2, @Session3, @Session4, @Session5);

    DELETE FROM [Session]
    WHERE Id IN (@Session1, @Session2, @Session3, @Session4, @Session5)
       OR UserId IN (@Student1Id, @Student2Id, @Student3Id, @Student4Id)
       OR TestId IN (@Test1, @Test2, @Test3, @Test4);

    DELETE FROM [ExamSchedule]
    WHERE Id IN (@Schedule1, @Schedule2, @Schedule3, @Schedule4)
       OR TestId IN (@Test1, @Test2, @Test3, @Test4)
       OR CourseId IN (@Course1, @Course2, @Course3, @Course4, @Course5);

    DELETE FROM [TestQuestionSnapshot]
    WHERE TestId IN (@Test1, @Test2, @Test3, @Test4);

    DELETE FROM [TestQuestion]
    WHERE TestId IN (@Test1, @Test2, @Test3, @Test4)
       OR QuestionId IN (@Q01, @Q02, @Q03, @Q04, @Q05, @Q06, @Q07, @Q08, @Q09, @Q10, @Q11, @Q12, @Q13, @Q14);

    DELETE FROM [Test]
    WHERE Id IN (@Test1, @Test2, @Test3, @Test4)
       OR CourseId IN (@Course1, @Course2, @Course3, @Course4, @Course5)
       OR Title IN (N'DEMO - Database Quiz 1', N'DEMO - Midterm OOP', N'DEMO - Final DB', N'DEMO - Accounting Test');

    DELETE FROM [Assessment]
    WHERE CourseId IN (@Course1, @Course2, @Course3, @Course4, @Course5)
       OR Title LIKE N'DEMO%';

    DELETE FROM [Options]
    WHERE QuestionId IN (@Q01, @Q02, @Q03, @Q04, @Q05, @Q06, @Q07, @Q08, @Q09, @Q10, @Q11, @Q12, @Q13, @Q14);

    DELETE FROM [Question]
    WHERE Id IN (@Q01, @Q02, @Q03, @Q04, @Q05, @Q06, @Q07, @Q08, @Q09, @Q10, @Q11, @Q12, @Q13, @Q14)
       OR Content LIKE N'[DEMO]%';

    DELETE FROM [QuestionBank]
    WHERE Id IN (@QBank1, @QBank2, @QBank3)
       OR Name LIKE N'DEMO%';

    DELETE FROM [Enrollment]
    WHERE Id IN (@Enroll1, @Enroll2, @Enroll3, @Enroll4, @Enroll5, @Enroll6, @Enroll7)
       OR CourseId IN (@Course1, @Course2, @Course3, @Course4, @Course5)
       OR StudentId IN (@Student1Id, @Student2Id, @Student3Id, @Student4Id);

    DELETE FROM [Transcript]
    WHERE Id IN (@Transcript1, @Transcript2, @Transcript3, @Transcript4)
       OR StudentId IN (@Student1Id, @Student2Id, @Student3Id, @Student4Id);

    DELETE FROM [Course]
    WHERE Id IN (@Course1, @Course2, @Course3, @Course4, @Course5)
       OR Code LIKE N'DEMO%';

    DELETE FROM [UserRole]
    WHERE UserId IN (@Staff1Id, @Lecturer1Id, @Lecturer2Id, @Student1Id, @Student2Id, @Student3Id, @Student4Id);

    DELETE FROM [RefreshToken]
    WHERE UserId IN (@Staff1Id, @Lecturer1Id, @Lecturer2Id, @Student1Id, @Student2Id, @Student3Id, @Student4Id, @AdminId);

    DELETE FROM [UserSession]
    WHERE UserId IN (@Staff1Id, @Lecturer1Id, @Lecturer2Id, @Student1Id, @Student2Id, @Student3Id, @Student4Id, @AdminId);

    DELETE FROM [PasswordReset]
    WHERE UserId IN (@Staff1Id, @Lecturer1Id, @Lecturer2Id, @Student1Id, @Student2Id, @Student3Id, @Student4Id, @AdminId)
       OR Email IN (N'staff1@test.edu.vn', N'lecturer1@test.edu.vn', N'lecturer2@test.edu.vn', N'student1@test.edu.vn', N'student2@test.edu.vn', N'student3@test.edu.vn', N'student4@test.edu.vn');

    DELETE FROM [Notification]
    WHERE UserId IN (@Staff1Id, @Lecturer1Id, @Lecturer2Id, @Student1Id, @Student2Id, @Student3Id, @Student4Id, @AdminId);

    DELETE FROM [Student]
    WHERE Id IN (@Student1Id, @Student2Id, @Student3Id, @Student4Id)
       OR Id IN (SELECT Id FROM [User] WHERE Email IN (N'student1@test.edu.vn', N'student2@test.edu.vn', N'student3@test.edu.vn', N'student4@test.edu.vn'));

    DELETE FROM [Lecturer]
    WHERE Id IN (@Lecturer1Id, @Lecturer2Id)
       OR Id IN (SELECT Id FROM [User] WHERE Email IN (N'lecturer1@test.edu.vn', N'lecturer2@test.edu.vn'));

    DELETE FROM [User]
    WHERE Email IN (N'staff1@test.edu.vn', N'lecturer1@test.edu.vn', N'lecturer2@test.edu.vn', N'student1@test.edu.vn', N'student2@test.edu.vn', N'student3@test.edu.vn', N'student4@test.edu.vn');

    DELETE FROM [StudentClass]
    WHERE Id IN (@ClassCNTT1, @ClassCNTT2, @ClassKT1, @ClassKTE1)
       OR Code IN (N'DEMO-CNTT-K18A', N'DEMO-CNTT-K18B', N'DEMO-KT-K18A', N'DEMO-KTE-K18A');

    DELETE FROM [Faculty]
    WHERE Id IN (@FacultyCNTT, @FacultyKT, @FacultyKTE)
       OR Code IN (N'DEMO-CNTT', N'DEMO-KT', N'DEMO-KTE');

    DELETE FROM [Skills]
    WHERE Id IN (@SkillSQL, @SkillCode, @SkillTheory)
       OR Name IN (N'DEMO-SQL', N'DEMO-CODING', N'DEMO-THEORY');

    DELETE FROM [Subjects]
    WHERE Id IN (@SubjectDB, @SubjectSE, @SubjectACC)
       OR Name IN (N'DEMO-DATABASE', N'DEMO-SOFTWARE-ENGINEERING', N'DEMO-ACCOUNTING');

    DELETE FROM [DifficultyLevels]
    WHERE Id IN (@DiffEasy, @DiffMedium, @DiffHard)
       OR Name IN (N'DEMO-EASY', N'DEMO-MEDIUM', N'DEMO-HARD');

    DELETE FROM [AuditEntries]
    WHERE Actor IN (N'admin@unitest.com', N'staff1@test.edu.vn', N'lecturer1@test.edu.vn')
      AND Action LIKE N'DEMO%';

    -----------------------------------------------------------------------------
    -- 2) Insert section
    -----------------------------------------------------------------------------

    -- Faculties (3)
    INSERT INTO [Faculty] (Id, Name, Code, Description, CreatedAt, UpdatedAt, IsDeleted)
    VALUES
    (@FacultyCNTT, N'Khoa Cong Nghe Thong Tin', N'DEMO-CNTT', N'Demo faculty CNTT', @Now, NULL, 0),
    (@FacultyKT,   N'Khoa Ke Toan',             N'DEMO-KT',   N'Demo faculty Ke Toan', @Now, NULL, 0),
    (@FacultyKTE,  N'Khoa Kinh Te',             N'DEMO-KTE',  N'Demo faculty Kinh Te', @Now, NULL, 0);

    -- Student classes (4)
    INSERT INTO [StudentClass] (Id, Name, Code, FacultyId, CreatedAt, UpdatedAt, IsDeleted)
    VALUES
    (@ClassCNTT1, N'CNTT K18A', N'DEMO-CNTT-K18A', @FacultyCNTT, @Now, NULL, 0),
    (@ClassCNTT2, N'CNTT K18B', N'DEMO-CNTT-K18B', @FacultyCNTT, @Now, NULL, 0),
    (@ClassKT1,   N'KT K18A',   N'DEMO-KT-K18A',   @FacultyKT,   @Now, NULL, 0),
    (@ClassKTE1,  N'KTE K18A',  N'DEMO-KTE-K18A',  @FacultyKTE,  @Now, NULL, 0);

    -- Users (8): upsert admin + insert others
    IF EXISTS (SELECT 1 FROM [User] WHERE Email = N'admin@unitest.com')
    BEGIN
        UPDATE [User]
        SET Name = N'System Admin',
            PasswordHash = @HashAdmin,
            Role = 0,
            IsActive = 1,
            IsDeleted = 0,
            AccessFailedCount = 0,
            LockoutEnd = NULL,
            UpdatedAt = @Now
        WHERE Email = N'admin@unitest.com';
    END
    ELSE
    BEGIN
        INSERT INTO [User] (Id, Name, Email, Role, PasswordHash, IsActive, AccessFailedCount, LockoutEnd, LastLoginAt, AvatarUrl, DateOfBirth, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, DeletedBy)
        VALUES (@AdminId, N'System Admin', N'admin@unitest.com', 0, @HashAdmin, 1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL);
    END;

    INSERT INTO [User] (Id, Name, Email, Role, PasswordHash, IsActive, AccessFailedCount, LockoutEnd, LastLoginAt, AvatarUrl, DateOfBirth, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, DeletedBy)
    VALUES
    (@Staff1Id,    N'Staff One',      N'staff1@test.edu.vn',    3, @HashStaff,    1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL),
    (@Lecturer1Id, N'Lecturer One',   N'lecturer1@test.edu.vn', 1, @HashLecturer, 1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL),
    (@Lecturer2Id, N'Lecturer Two',   N'lecturer2@test.edu.vn', 1, @HashLecturer, 1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL),
    (@Student1Id,  N'Student One',    N'student1@test.edu.vn',  2, @HashStudent,  1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL),
    (@Student2Id,  N'Student Two',    N'student2@test.edu.vn',  2, @HashStudent,  1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL),
    (@Student3Id,  N'Student Three',  N'student3@test.edu.vn',  2, @HashStudent,  1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL),
    (@Student4Id,  N'Student Four',   N'student4@test.edu.vn',  2, @HashStudent,  1, 0, NULL, NULL, NULL, NULL, @Now, NULL, 0, NULL, NULL);

    -- Lecturer and Student TPT rows
    INSERT INTO [Lecturer] (Id, LecturerCode, FacultyId)
    VALUES
    (@Lecturer1Id, N'DEMO-LEC-001', @FacultyCNTT),
    (@Lecturer2Id, N'DEMO-LEC-002', @FacultyKT);

    INSERT INTO [Student] (Id, StudentCode, StudentClassId, AcademicYear, Major)
    VALUES
    (@Student1Id, N'DEMO-STU-001', @ClassCNTT1, N'2025-2026', N'Information Technology'),
    (@Student2Id, N'DEMO-STU-002', @ClassCNTT2, N'2025-2026', N'Software Engineering'),
    (@Student3Id, N'DEMO-STU-003', @ClassKT1,   N'2025-2026', N'Accounting'),
    (@Student4Id, N'DEMO-STU-004', @ClassKTE1,  N'2025-2026', N'Business Economics');

    -- Courses (5)
    INSERT INTO [Course] (Id, Name, Code, Credits, LecturerId, SubjectArea, Semester, CreatedAt, UpdatedAt, IsDeleted)
    VALUES
    (@Course1, N'Database Fundamentals',    N'DEMO001', 3, @Lecturer1Id, N'Database',              N'HK2', @Now, NULL, 0),
    (@Course2, N'OOP with CSharp',          N'DEMO002', 3, @Lecturer1Id, N'Software Engineering',  N'HK2', @Now, NULL, 0),
    (@Course3, N'Advanced SQL',             N'DEMO003', 3, @Lecturer1Id, N'Database',              N'HK2', @Now, NULL, 0),
    (@Course4, N'Accounting Principles',    N'DEMO004', 3, @Lecturer2Id, N'Accounting',            N'HK2', @Now, NULL, 0),
    (@Course5, N'Business Statistics',      N'DEMO005', 3, @Lecturer2Id, N'Economics',             N'HK2', @Now, NULL, 0);

    -- Enrollments (7)
    INSERT INTO [Enrollment] (Id, StudentId, CourseId, Semester, FinalScore, Grade, GradePoint, EnrolledAt, IsDeleted)
    VALUES
    (@Enroll1, @Student1Id, @Course1, N'HK2-2025', 8.8, N'A', 4.0, @Now, 0),
    (@Enroll2, @Student1Id, @Course2, N'HK2-2025', 8.2, N'B', 3.5, @Now, 0),
    (@Enroll3, @Student2Id, @Course1, N'HK2-2025', 7.6, N'B', 3.2, @Now, 0),
    (@Enroll4, @Student2Id, @Course3, N'HK2-2025', 7.0, N'C', 2.8, @Now, 0),
    (@Enroll5, @Student3Id, @Course4, N'HK2-2025', 8.4, N'B', 3.6, @Now, 0),
    (@Enroll6, @Student4Id, @Course5, N'HK2-2025', 7.8, N'B', 3.3, @Now, 0),
    (@Enroll7, @Student4Id, @Course4, N'HK2-2025', 7.1, N'C', 2.9, @Now, 0);

    -- Metadata: Difficulty / Subject / Skill
    INSERT INTO [DifficultyLevels] (Id, Name, Weight, IsDeleted)
    VALUES
    (@DiffEasy,   N'DEMO-EASY',   1, 0),
    (@DiffMedium, N'DEMO-MEDIUM', 2, 0),
    (@DiffHard,   N'DEMO-HARD',   3, 0);

    INSERT INTO [Subjects] (Id, Name, Description, IsDeleted)
    VALUES
    (@SubjectDB,  N'DEMO-DATABASE',             N'Database subject for demo', 0),
    (@SubjectSE,  N'DEMO-SOFTWARE-ENGINEERING', N'SE subject for demo', 0),
    (@SubjectACC, N'DEMO-ACCOUNTING',           N'Accounting subject for demo', 0);

    INSERT INTO [Skills] (Id, Name, Description, IsDeleted)
    VALUES
    (@SkillSQL,    N'DEMO-SQL',    N'SQL querying and optimization', 0),
    (@SkillCode,   N'DEMO-CODING', N'Coding fundamentals', 0),
    (@SkillTheory, N'DEMO-THEORY', N'Theory and concepts', 0);

    -- QuestionBanks (3)
    INSERT INTO [QuestionBank] (Id, CourseId, Name, CreatedBy, CreatedAt, IsDeleted)
    VALUES
    (@QBank1, @Course1, N'DEMO Question Bank - DB',   N'lecturer1@test.edu.vn', @Now, 0),
    (@QBank2, @Course2, N'DEMO Question Bank - OOP',  N'lecturer1@test.edu.vn', @Now, 0),
    (@QBank3, @Course4, N'DEMO Question Bank - ACC',  N'lecturer2@test.edu.vn', @Now, 0);

    -- Questions (14)
    -- QType: MCQ=0, TrueFalse=1, Essay=2, Matching=3, DragDrop=4
    -- Status: Draft=0, Pending=1, Approved=2, Rejected=3
    INSERT INTO [Question]
    (
        Id, Content, Version, ParentQuestionId, Type, Status, EssayMinWords, MatchingPairs, DragDrop,
        SubjectId, DifficultyLevelId, SkillId, Tags, Media, QuestionBankId,
        CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, IsActive, IsDeleted, DeletedAt, DeletedBy
    )
    VALUES
    (@Q01, N'[DEMO] Which SQL clause is used to filter rows?', 1, NULL, 0, 2, NULL, NULL, NULL, @SubjectDB, @DiffEasy,   @SkillSQL,    N'["sql","basic"]',                 N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q02, N'[DEMO] Which join returns all rows from the left table?', 1, NULL, 0, 2, NULL, NULL, NULL, @SubjectDB, @DiffMedium, @SkillSQL,    N'["sql","join"]',                  N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q03, N'[DEMO] What does OOP stand for?', 1, NULL, 0, 2, NULL, NULL, NULL, @SubjectSE, @DiffEasy,   @SkillTheory, N'["oop","theory"]',                N'[]', @QBank2, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q04, N'[DEMO] Which access modifier allows access only inside class?', 1, NULL, 0, 2, NULL, NULL, NULL, @SubjectSE, @DiffMedium, @SkillCode,   N'["csharp","oop"]',                 N'[]', @QBank2, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q05, N'[DEMO] True/False: PRIMARY KEY can contain NULL values.', 1, NULL, 1, 2, NULL, NULL, NULL, @SubjectDB, @DiffEasy,   @SkillSQL,    N'["sql","constraint"]',            N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q06, N'[DEMO] True/False: Encapsulation is a core OOP principle.', 1, NULL, 1, 2, NULL, NULL, NULL, @SubjectSE, @DiffEasy,   @SkillTheory, N'["oop","encapsulation"]',         N'[]', @QBank2, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q07, N'[DEMO] Explain normalization and why it matters in relational design.', 1, NULL, 2, 2, 80, NULL, NULL, @SubjectDB, @DiffHard,   @SkillTheory, N'["database","essay"]',             N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q08, N'[DEMO] Explain inheritance with a practical C# example.', 1, NULL, 2, 2, 70, NULL, NULL, @SubjectSE, @DiffMedium, @SkillCode,   N'["oop","essay"]',                  N'[]', @QBank2, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q09, N'[DEMO] Match SQL terms with definitions.', 1, NULL, 3, 2, NULL, N'[{"L":"SELECT","R":"Retrieve data"},{"L":"UPDATE","R":"Modify existing rows"},{"L":"DELETE","R":"Remove rows"}]', NULL, @SubjectDB, @DiffMedium, @SkillSQL, N'["sql","matching"]', N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q10, N'[DEMO] Match accounting terms with meanings.', 1, NULL, 3, 2, NULL, N'[{"L":"Asset","R":"Resource owned by entity"},{"L":"Liability","R":"Present obligation"}]', NULL, @SubjectACC, @DiffMedium, @SkillTheory, N'["accounting","matching"]', N'[]', @QBank3, N'lecturer2@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q11, N'[DEMO] Drag and drop SQL clauses to correct order.', 1, NULL, 4, 2, NULL, NULL, N'{"Tokens":["WHERE","SELECT","ORDER BY","FROM"],"Slots":[{"Name":"1","Answer":"SELECT"},{"Name":"2","Answer":"FROM"},{"Name":"3","Answer":"WHERE"},{"Name":"4","Answer":"ORDER BY"}]}', @SubjectDB, @DiffHard, @SkillSQL, N'["sql","dragdrop"]', N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q12, N'[DEMO] Drag OOP concepts to proper definitions.', 1, NULL, 4, 2, NULL, NULL, N'{"Tokens":["Abstraction","Polymorphism","Encapsulation"],"Slots":[{"Name":"Hide implementation","Answer":"Abstraction"},{"Name":"Same interface different behavior","Answer":"Polymorphism"},{"Name":"Bundle data and methods","Answer":"Encapsulation"}]}', @SubjectSE, @DiffHard, @SkillTheory, N'["oop","dragdrop"]', N'[]', @QBank2, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q13, N'[DEMO] Which statement creates a table in SQL?', 1, NULL, 0, 2, NULL, NULL, NULL, @SubjectDB, @DiffEasy, @SkillSQL, N'["sql","ddl"]', N'[]', @QBank1, N'lecturer1@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL),
    (@Q14, N'[DEMO] Which report best shows company cash movement?', 1, NULL, 0, 2, NULL, NULL, NULL, @SubjectACC, @DiffEasy, @SkillTheory, N'["accounting","reporting"]', N'[]', @QBank3, N'lecturer2@test.edu.vn', @Now, NULL, NULL, 1, 0, NULL, NULL);

    -- Options (26 total)
    INSERT INTO [Options] (Id, QuestionId, Content, IsCorrect, IsDeleted)
    VALUES
    -- Q01 (4)
    (N'17100000000000000000000000000001', @Q01, N'WHERE', 1, 0),
    (N'17100000000000000000000000000002', @Q01, N'ORDER BY', 0, 0),
    (N'17100000000000000000000000000003', @Q01, N'GROUP BY', 0, 0),
    (N'17100000000000000000000000000004', @Q01, N'HAVING', 0, 0),

    -- Q02 (4)
    (N'17100000000000000000000000000005', @Q02, N'LEFT JOIN', 1, 0),
    (N'17100000000000000000000000000006', @Q02, N'INNER JOIN', 0, 0),
    (N'17100000000000000000000000000007', @Q02, N'RIGHT JOIN', 0, 0),
    (N'17100000000000000000000000000008', @Q02, N'CROSS JOIN', 0, 0),

    -- Q03 (4)
    (N'17100000000000000000000000000009', @Q03, N'Object-Oriented Programming', 1, 0),
    (N'1710000000000000000000000000000a', @Q03, N'Open Office Process', 0, 0),
    (N'1710000000000000000000000000000b', @Q03, N'Operation Output Procedure', 0, 0),
    (N'1710000000000000000000000000000c', @Q03, N'Object Option Pattern', 0, 0),

    -- Q04 (4)
    (N'1710000000000000000000000000000d', @Q04, N'private', 1, 0),
    (N'1710000000000000000000000000000e', @Q04, N'public', 0, 0),
    (N'1710000000000000000000000000000f', @Q04, N'protected internal', 0, 0),
    (N'17100000000000000000000000000010', @Q04, N'internal', 0, 0),

    -- Q05 (2)
    (N'17100000000000000000000000000011', @Q05, N'True', 0, 0),
    (N'17100000000000000000000000000012', @Q05, N'False', 1, 0),

    -- Q06 (2)
    (N'17100000000000000000000000000013', @Q06, N'True', 1, 0),
    (N'17100000000000000000000000000014', @Q06, N'False', 0, 0),

    -- Q13 (3)
    (N'17100000000000000000000000000015', @Q13, N'CREATE TABLE', 1, 0),
    (N'17100000000000000000000000000016', @Q13, N'MAKE TABLE', 0, 0),
    (N'17100000000000000000000000000017', @Q13, N'NEW TABLE', 0, 0),

    -- Q14 (3)
    (N'17100000000000000000000000000018', @Q14, N'Cash Flow Statement', 1, 0),
    (N'17100000000000000000000000000019', @Q14, N'Balance Sheet only', 0, 0),
    (N'1710000000000000000000000000001a', @Q14, N'Inventory Ledger', 0, 0);

    -- Tests (4)
    -- TestType: Test=0, Exam=1
    -- AssessmentType: Quiz=0, Midterm=1, Final=2, Assignment=3
    INSERT INTO [Test]
    (Id, AssessmentId, AssessmentType, CourseId, Title, Type, DurationMinutes, PassScore, ShuffleQuestions, ShuffleOptions, TotalMaxScore, IsPublished, IsArchived, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, PublishedAt, IsDeleted)
    VALUES
    (@Test1, NULL, 0, @Course1, N'DEMO - Database Quiz 1', 0, 30, 5, 1, 1, 10.00, 1, 0, N'lecturer1@test.edu.vn', @Now, NULL, NULL, @Now, 0),
    (@Test2, NULL, 1, @Course2, N'DEMO - Midterm OOP',     1, 45, 5, 1, 1, 10.00, 1, 0, N'lecturer1@test.edu.vn', @Now, NULL, NULL, @Now, 0),
    (@Test3, NULL, 2, @Course3, N'DEMO - Final DB',        1, 60, 5, 1, 1, 10.00, 1, 0, N'lecturer1@test.edu.vn', @Now, NULL, NULL, @Now, 0),
    (@Test4, NULL, 0, @Course4, N'DEMO - Accounting Test', 0, 25, 5, 1, 1, 10.00, 1, 0, N'lecturer2@test.edu.vn', @Now, NULL, NULL, @Now, 0);

    -- TestQuestions (13)
    INSERT INTO [TestQuestion] (TestId, QuestionId, [Order], Points)
    VALUES
    -- Test1
    (@Test1, @Q01, 1, 1.5),
    (@Test1, @Q02, 2, 1.5),
    (@Test1, @Q05, 3, 1.0),
    (@Test1, @Q07, 4, 3.0),

    -- Test2
    (@Test2, @Q03, 1, 1.5),
    (@Test2, @Q04, 2, 1.5),
    (@Test2, @Q06, 3, 1.0),
    (@Test2, @Q08, 4, 3.0),

    -- Test3
    (@Test3, @Q09, 1, 2.0),
    (@Test3, @Q11, 2, 2.5),
    (@Test3, @Q13, 3, 1.5),

    -- Test4
    (@Test4, @Q10, 1, 2.0),
    (@Test4, @Q14, 2, 2.0);

    -- Exam schedules (4)
    INSERT INTO [ExamSchedule] (Id, TestId, CourseId, Room, StartTime, EndTime, IsManuallyLocked, ExamType, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
    (@Schedule1, @Test1, @Course1, N'Lab-A1', DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 30, @Now)), 0, N'Quiz',   0, @Now, NULL),
    (@Schedule2, @Test2, @Course2, N'Lab-B2', DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, DATEADD(MINUTE, 45, @Now)), 0, N'Midterm',0, @Now, NULL),
    (@Schedule3, @Test3, @Course3, N'Hall-C3',DATEADD(DAY, -3, @Now), DATEADD(DAY, -3, DATEADD(MINUTE, 60, @Now)), 0, N'Final',  0, @Now, NULL),
    (@Schedule4, @Test4, @Course4, N'Room-D4',DATEADD(DAY,  1, @Now), DATEADD(DAY,  1, DATEADD(MINUTE, 25, @Now)), 0, N'Quiz',   0, @Now, NULL);

    -- Sessions (5)
    -- SessionStatus: NotStarted=0, InProgress=1, Submitted=2, AutoSubmitted=3, Cancelled=4, Violated=5, Graded=6
    INSERT INTO [Session]
    (Id, TestId, UserId, StartAt, EndAt, Status, LastActivityAt, ConsumedSeconds, TimerStartedAt, AutoScore, ManualScore, TotalScore, MaxScore, Percent, IsPassed, GradedAt, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
    (@Session1, @Test1, @Student1Id, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 28, @Now)), 6, DATEADD(DAY, -5, DATEADD(MINUTE, 28, @Now)), 1680, NULL, 6.5, 2.0, 8.5, 10.0, 85.0, 1, DATEADD(DAY, -5, DATEADD(MINUTE, 35, @Now)), 0, @Now, NULL),
    (@Session2, @Test1, @Student2Id, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 30, @Now)), 6, DATEADD(DAY, -5, DATEADD(MINUTE, 30, @Now)), 1800, NULL, 6.0, 1.0, 7.0, 10.0, 70.0, 1, DATEADD(DAY, -5, DATEADD(MINUTE, 36, @Now)), 0, @Now, NULL),
    (@Session3, @Test2, @Student1Id, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, DATEADD(MINUTE, 40, @Now)), 6, DATEADD(DAY, -4, DATEADD(MINUTE, 40, @Now)), 2400, NULL, 5.8, 1.5, 7.3, 10.0, 73.0, 1, DATEADD(DAY, -4, DATEADD(MINUTE, 50, @Now)), 0, @Now, NULL),
    (@Session4, @Test4, @Student3Id, DATEADD(DAY, -2, @Now), DATEADD(DAY, -2, DATEADD(MINUTE, 22, @Now)), 2, DATEADD(DAY, -2, DATEADD(MINUTE, 22, @Now)), 1320, NULL, 6.2, 0.0, 6.2, 10.0, 62.0, 1, NULL, 0, @Now, NULL),
    (@Session5, @Test4, @Student4Id, DATEADD(HOUR, -1, @Now), NULL, 1, DATEADD(MINUTE, -2, @Now), 820, DATEADD(MINUTE, -14, @Now), 2.5, 0.0, 2.5, 10.0, 25.0, 0, NULL, 0, @Now, NULL);

    -- Student answers (10)
    INSERT INTO [StudentAnswer] (Id, SessionId, QuestionId, SelectedOptionId, EssayAnswer, Comment, Score, GradedAt, AnsweredAt)
    VALUES
    (N'1e000000000000000000000000000001', @Session1, @Q01, N'17100000000000000000000000000001', NULL, NULL, 1.5, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 5, @Now))),
    (N'1e000000000000000000000000000002', @Session1, @Q02, N'17100000000000000000000000000005', NULL, NULL, 1.5, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 10, @Now))),
    (N'1e000000000000000000000000000003', @Session1, @Q05, N'17100000000000000000000000000012', NULL, NULL, 1.0, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 13, @Now))),
    (N'1e000000000000000000000000000004', @Session1, @Q07, NULL, N'Normalization reduces redundancy and protects data integrity.', N'Good explanation', 2.0, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 20, @Now))),

    (N'1e000000000000000000000000000005', @Session2, @Q01, N'17100000000000000000000000000003', NULL, NULL, 0.0, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 6, @Now))),
    (N'1e000000000000000000000000000006', @Session2, @Q02, N'17100000000000000000000000000005', NULL, NULL, 1.5, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 11, @Now))),
    (N'1e000000000000000000000000000007', @Session2, @Q05, N'17100000000000000000000000000011', NULL, NULL, 0.0, DATEADD(DAY, -5, @Now), DATEADD(DAY, -5, DATEADD(MINUTE, 14, @Now))),

    (N'1e000000000000000000000000000008', @Session3, @Q03, N'17100000000000000000000000000009', NULL, NULL, 1.5, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, DATEADD(MINUTE, 8, @Now))),
    (N'1e000000000000000000000000000009', @Session3, @Q04, N'1710000000000000000000000000000d', NULL, NULL, 1.5, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, DATEADD(MINUTE, 13, @Now))),
    (N'1e00000000000000000000000000000a', @Session3, @Q08, NULL, N'Inheritance allows derived classes to reuse base class behavior.', N'Clear and practical', 1.5, DATEADD(DAY, -4, @Now), DATEADD(DAY, -4, DATEADD(MINUTE, 30, @Now)));

    -- Results (4)
    INSERT INTO [Result] (Id, UserId, TestId, Score, MaxScore, SubmitTime, Status, SessionId, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
    (@Result1, @Student1Id, @Test1, 8.5, 10.0, DATEADD(DAY, -5, DATEADD(MINUTE, 28, @Now)), 6, @Session1, 0, @Now, NULL),
    (@Result2, @Student2Id, @Test1, 7.0, 10.0, DATEADD(DAY, -5, DATEADD(MINUTE, 30, @Now)), 6, @Session2, 0, @Now, NULL),
    (@Result3, @Student1Id, @Test2, 7.3, 10.0, DATEADD(DAY, -4, DATEADD(MINUTE, 40, @Now)), 6, @Session3, 0, @Now, NULL),
    (@Result4, @Student3Id, @Test4, 6.2, 10.0, DATEADD(DAY, -2, DATEADD(MINUTE, 22, @Now)), 2, @Session4, 0, @Now, NULL);

    -- Transcripts (4)
    INSERT INTO [Transcript]
    (Id, StudentId, GPA, TotalCredits, AcademicYear, YearEndGpa4, YearEndGpa10, YearEndTotalCreditsEarned, AcademicStatus, IsYearEndFinalized, IsYearEndLocked, YearEndFinalizedAt, YearEndFinalizedBy, CalculatedAt, IsDeleted, CreatedAt, UpdatedAt)
    VALUES
    (@Transcript1, @Student1Id, 3.60, 6, N'2025-2026', 3.60, 8.90, 6, N'Good',    0, 0, NULL, NULL, @Now, 0, @Now, NULL),
    (@Transcript2, @Student2Id, 3.00, 6, N'2025-2026', 3.00, 7.60, 6, N'Average', 0, 0, NULL, NULL, @Now, 0, @Now, NULL),
    (@Transcript3, @Student3Id, 3.20, 3, N'2025-2026', 3.20, 8.10, 3, N'Good',    0, 0, NULL, NULL, @Now, 0, @Now, NULL),
    (@Transcript4, @Student4Id, 3.10, 6, N'2025-2026', 3.10, 7.80, 6, N'Average', 0, 0, NULL, NULL, @Now, 0, @Now, NULL);

    -- Feedbacks (3)
    INSERT INTO [Feedback] (Id, SessionId, Content, Rating, CreatedAt, IsDeleted, UpdatedAt)
    VALUES
    (@Feedback1, @Session1, N'Test was clear and fair.', 5, @Now, 0, NULL),
    (@Feedback2, @Session2, N'Need better time warning in runner page.', 4, @Now, 0, NULL),
    (@Feedback3, @Session3, N'Essay grading was a bit strict.', 3, @Now, 0, NULL);

    -- System settings (1)
    IF EXISTS (SELECT 1 FROM [SystemSettings] WHERE Id = N'settings')
    BEGIN
        UPDATE [SystemSettings]
        SET SystemName = N'UniTestSystem Demo',
            LogoUrl = N'/uploads/logo/demo-logo.png',
            CurrentSemester = N'HK2',
            CurrentAcademicYear = N'2025-2026',
            WarningGpaThreshold = 2.00,
            FailGpaThreshold = 1.00,
            TreatOutstandingDebtAsFail = 1,
            UpdatedAt = @Now,
            UpdatedBy = N'admin@unitest.com'
        WHERE Id = N'settings';
    END
    ELSE
    BEGIN
        INSERT INTO [SystemSettings]
        (Id, SystemName, LogoUrl, CurrentSemester, CurrentAcademicYear, WarningGpaThreshold, FailGpaThreshold, TreatOutstandingDebtAsFail, UpdatedAt, UpdatedBy)
        VALUES
        (N'settings', N'UniTestSystem Demo', N'/uploads/logo/demo-logo.png', N'HK2', N'2025-2026', 2.00, 1.00, 1, @Now, N'admin@unitest.com');
    END;

    -- Audit entries (3)
    INSERT INTO [AuditEntries] ([At], [Actor], [Action], [EntityName], [EntityId], [Before], [After])
    VALUES
    (DATEADD(MINUTE, -90, @Now), N'admin@unitest.com',      N'DEMO_IMPORT_DATA', N'System', N'demo-seed', NULL, N'{"status":"success","rows":100}'),
    (DATEADD(MINUTE, -60, @Now), N'staff1@test.edu.vn',     N'DEMO_ASSIGN_TEST', N'Test',   @Test1,       NULL, N'{"assignedToClass":"DEMO-CNTT-K18A"}'),
    (DATEADD(MINUTE, -30, @Now), N'lecturer1@test.edu.vn',  N'DEMO_APPROVE_QUESTION', N'Question', @Q01, NULL, N'{"status":"Approved"}');

    COMMIT TRANSACTION;

    -----------------------------------------------------------------------------
    -- 3) Verification section
    -----------------------------------------------------------------------------

    SELECT N'Faculties' AS [Entity], COUNT(*) AS [Count] FROM [Faculty] WHERE Id IN (@FacultyCNTT, @FacultyKT, @FacultyKTE)
    UNION ALL
    SELECT N'StudentClasses', COUNT(*) FROM [StudentClass] WHERE Id IN (@ClassCNTT1, @ClassCNTT2, @ClassKT1, @ClassKTE1)
    UNION ALL
    SELECT N'Users', COUNT(*) FROM [User] WHERE Email IN (N'admin@unitest.com', N'staff1@test.edu.vn', N'lecturer1@test.edu.vn', N'lecturer2@test.edu.vn', N'student1@test.edu.vn', N'student2@test.edu.vn', N'student3@test.edu.vn', N'student4@test.edu.vn')
    UNION ALL
    SELECT N'Courses', COUNT(*) FROM [Course] WHERE Id IN (@Course1, @Course2, @Course3, @Course4, @Course5)
    UNION ALL
    SELECT N'Enrollments', COUNT(*) FROM [Enrollment] WHERE Id IN (@Enroll1, @Enroll2, @Enroll3, @Enroll4, @Enroll5, @Enroll6, @Enroll7)
    UNION ALL
    SELECT N'Questions', COUNT(*) FROM [Question] WHERE Id IN (@Q01, @Q02, @Q03, @Q04, @Q05, @Q06, @Q07, @Q08, @Q09, @Q10, @Q11, @Q12, @Q13, @Q14)
    UNION ALL
    SELECT N'Options', COUNT(*) FROM [Options] WHERE QuestionId IN (@Q01, @Q02, @Q03, @Q04, @Q05, @Q06, @Q13, @Q14)
    UNION ALL
    SELECT N'Tests', COUNT(*) FROM [Test] WHERE Id IN (@Test1, @Test2, @Test3, @Test4)
    UNION ALL
    SELECT N'TestQuestions', COUNT(*) FROM [TestQuestion] WHERE TestId IN (@Test1, @Test2, @Test3, @Test4)
    UNION ALL
    SELECT N'ExamSchedules', COUNT(*) FROM [ExamSchedule] WHERE Id IN (@Schedule1, @Schedule2, @Schedule3, @Schedule4)
    UNION ALL
    SELECT N'Sessions', COUNT(*) FROM [Session] WHERE Id IN (@Session1, @Session2, @Session3, @Session4, @Session5)
    UNION ALL
    SELECT N'StudentAnswers', COUNT(*) FROM [StudentAnswer] WHERE SessionId IN (@Session1, @Session2, @Session3)
    UNION ALL
    SELECT N'Results', COUNT(*) FROM [Result] WHERE Id IN (@Result1, @Result2, @Result3, @Result4)
    UNION ALL
    SELECT N'Transcripts', COUNT(*) FROM [Transcript] WHERE Id IN (@Transcript1, @Transcript2, @Transcript3, @Transcript4)
    UNION ALL
    SELECT N'Feedbacks', COUNT(*) FROM [Feedback] WHERE Id IN (@Feedback1, @Feedback2, @Feedback3)
    UNION ALL
    SELECT N'AuditEntries', COUNT(*) FROM [AuditEntries] WHERE [Action] LIKE N'DEMO%';

    SELECT
        u.Email,
        u.Role,
        u.IsActive,
        CASE WHEN s.Id IS NOT NULL THEN N'Student' WHEN l.Id IS NOT NULL THEN N'Lecturer' ELSE N'BaseUser' END AS ProfileType
    FROM [User] u
    LEFT JOIN [Student] s ON s.Id = u.Id
    LEFT JOIN [Lecturer] l ON l.Id = u.Id
    WHERE u.Email IN (N'admin@unitest.com', N'staff1@test.edu.vn', N'lecturer1@test.edu.vn', N'lecturer2@test.edu.vn', N'student1@test.edu.vn', N'student2@test.edu.vn', N'student3@test.edu.vn', N'student4@test.edu.vn')
    ORDER BY u.Email;

    SELECT TOP 20
        t.Title,
        c.Code AS CourseCode,
        q.Content,
        tq.[Order],
        tq.Points
    FROM [TestQuestion] tq
    INNER JOIN [Test] t ON t.Id = tq.TestId
    INNER JOIN [Course] c ON c.Id = t.CourseId
    INNER JOIN [Question] q ON q.Id = tq.QuestionId
    WHERE tq.TestId IN (@Test1, @Test2, @Test3, @Test4)
    ORDER BY t.Title, tq.[Order];

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
