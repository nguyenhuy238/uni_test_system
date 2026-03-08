using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain;

public class Question
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required, MinLength(5)]
    public string Content { get; set; } = "";

    public int Version { get; set; } = 1;
    public string? ParentQuestionId { get; set; }
    public virtual Question? ParentQuestion { get; set; }

    public QType Type { get; set; } = QType.MCQ;
    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;

    public virtual ICollection<Option> Options { get; set; } = new List<Option>();

    public virtual ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();

    public int? EssayMinWords { get; set; }

    public List<MatchPair>? MatchingPairs { get; set; } = new List<MatchPair>();

    public virtual DragDropConfig? DragDrop { get; set; }

    [Required] 
    public string SubjectId { get; set; } = "";
    public virtual Subject? Subject { get; set; }

    [Required] 
    public string DifficultyLevelId { get; set; } = "";
    public virtual DifficultyLevel? DifficultyLevel { get; set; }

    public string? SkillId { get; set; }
    public virtual Skill? Skill { get; set; }
    public List<string> Tags { get; set; } = new List<string>();

    public List<MediaFile> Media { get; set; } = new List<MediaFile>();

    public string? QuestionBankId { get; set; }
    public virtual QuestionBank? QuestionBank { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

public record MatchPair(string L, string R);

public class DragDropConfig
{
    public List<string> Tokens { get; set; } = new();
    public List<DragSlot> Slots { get; set; } = new();
}
public record DragSlot(string Name, string Answer);

public class MediaFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string? Caption { get; set; }
}
