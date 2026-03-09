namespace UniTestSystem.AdminApp.Models
{
    public enum Role
    {
        Admin,
        Lecturer,
        Student
    }

    public enum QuestionType
    {
        MCQ,
        TrueFalse,
        Essay,
        Matching,
        DragDrop
    }

    public enum TestType
    {
        Test,
        Exam
    }

    public enum SessionStatus
    {
        Draft,
        Submitted,
        Graded
    }
}
