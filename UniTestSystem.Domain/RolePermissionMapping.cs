using System;
using System.Collections.Generic;

namespace UniTestSystem.Domain
{
    public class RolePermissionMapping
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public Role Role { get; set; }
        public List<string> Permissions { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }

    public static class PermissionCodes
    {
        // System Management
        public const string Users_Manage = "Users.Manage";
        public const string Roles_Assign = "Roles.Assign";
        public const string Audit_View = "Audit.View";
        public const string Settings_Edit = "Settings.Edit";
        public const string System_Backup = "System.Backup";
        public const string System_Jwt_Edit = "System.Jwt.Edit";

        // Academic Management
        public const string Org_View = "Org.View";
        public const string Org_Manage = "Org.Manage";
        public const string Courses_Manage = "Courses.Manage";
        public const string Lecturer_Assign = "Lecturer.Assign";
        public const string Semester_Manage = "Semester.Manage";
        public const string Enrollment_Manage = "Enrollment.Manage";

        public const string Question_View = "Question.View";
        public const string Question_Create = "Question.Create";
        public const string Question_Edit = "Question.Edit";
        public const string Question_Delete = "Question.Delete";
        public const string Question_Approve = "Question.Approve";
        public const string Question_Categorize = "Question.Categorize";

        // Exam Management
        public const string Tests_View = "Tests.View";
        public const string Tests_Create = "Tests.Create";
        public const string Tests_Publish = "Tests.Publish";
        public const string Tests_Submit = "Tests.Submit";
        public const string Exam_Schedule = "Exam.Schedule";
        public const string Exam_Lock = "Exam.Lock";
        public const string Exam_Session_Reset = "Exam.Session.Reset";

        // Grading & Transcripts
        public const string Grading_Manual = "Grading.Manual";
        public const string Grading_Review = "Grading.Review";
        public const string Transcript_View = "Transcript.View";
        public const string Transcript_Manage = "Transcript.Manage";
        public const string Transcript_Lock = "Transcript.Lock";

        // Reporting & Analytics
        public const string Reports_View = "Reports.View";
        public const string Reports_Export = "Reports.Export";
        public const string Analytics_GPA = "Analytics.GPA";
        public const string Analytics_Difficulty = "Analytics.Difficulty";

        public static readonly string[] All =
        {
            Users_Manage, Roles_Assign, Audit_View, Settings_Edit, System_Backup, System_Jwt_Edit,
            Org_View, Org_Manage, Courses_Manage, Lecturer_Assign, Semester_Manage, Enrollment_Manage,
            Question_View, Question_Create, Question_Edit, Question_Delete, Question_Approve, Question_Categorize,
            Tests_View, Tests_Create, Tests_Publish, Tests_Submit, Exam_Schedule, Exam_Lock, Exam_Session_Reset,
            Grading_Manual, Grading_Review, Transcript_View, Transcript_Manage, Transcript_Lock,
            Reports_View, Reports_Export, Analytics_GPA, Analytics_Difficulty
        };
    }
}
