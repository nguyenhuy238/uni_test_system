namespace UniTestSystem.Domain
{
    public class Result
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string UserId { get; set; } = "";
        public virtual User User { get; set; } = null!;

        public string TestId { get; set; } = "";
        public virtual Test Test { get; set; } = null!;

        public decimal Score { get; set; } = 0;
        public decimal MaxScore { get; set; } = 10;
        public DateTime SubmitTime { get; set; } = DateTime.UtcNow;
        public SessionStatus Status { get; set; } = SessionStatus.Submitted;

        public string SessionId { get; set; } = ""; // Link to Session
        public virtual Session Session { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public interface IResult
    {
        bool IsFailure { get; }
        int StatusCode { get; }
        string? Error { get; }
        IReadOnlyCollection<string> Errors { get; }
    }

    public sealed class Result<T> : IResult
    {
        private Result(bool isSuccess, T? value, string? error, IReadOnlyCollection<string> errors, int statusCode)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            Errors = errors;
            StatusCode = statusCode;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public int StatusCode { get; }
        public string? Error { get; }
        public IReadOnlyCollection<string> Errors { get; }

        public static Result<T> Success(T value)
        {
            return new Result<T>(
                isSuccess: true,
                value: value,
                error: null,
                errors: Array.Empty<string>(),
                statusCode: 200);
        }

        public static Result<T> Failure(
            string error,
            int statusCode = 400,
            IReadOnlyCollection<string>? errors = null)
        {
            var finalErrors = errors is { Count: > 0 }
                ? errors
                : new[] { error };

            return new Result<T>(
                isSuccess: false,
                value: default,
                error: error,
                errors: finalErrors,
                statusCode: statusCode);
        }
    }
}
