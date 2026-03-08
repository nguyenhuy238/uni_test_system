using UniTestSystem.Domain;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IEmailSender _email;
        public NotificationService(IEmailSender email) => _email = email;

        public async Task NotifyAssignmentsAsync(
            Test test,
            IEnumerable<AssignmentNotifyTarget> targets,
            DateTime startAtUtc,
            DateTime endAtUtc)
        {
            var msg = $"You have been assigned to test: {test.Title}. " +
                      $"Starts: {startAtUtc:yyyy-MM-dd HH:mm} UTC, Ends: {endAtUtc:yyyy-MM-dd HH:mm} UTC.";
            
            foreach (var target in targets)
            {
                await _email.SendAsync(target.User.Email, "New Test Assignment", msg);
            }
        }
    }
}
