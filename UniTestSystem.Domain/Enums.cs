namespace UniTestSystem.Domain
{
    public enum Role { Admin, Lecturer, Student, Staff }
    public enum QType { MCQ, TrueFalse, Essay, Matching, DragDrop }
    public enum QuestionStatus { Draft, Pending, Approved, Rejected }
    public enum TestType { Test, Exam }
    public enum AssessmentType { Quiz, Midterm, Final, Assignment }
    public enum SessionStatus { NotStarted, InProgress, Submitted, AutoSubmitted, Cancelled, Violated, Graded }
}
