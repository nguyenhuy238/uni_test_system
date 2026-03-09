using System.Text.RegularExpressions;

namespace UniTestSystem.Application
{
    public static class AuthValidation
    {
        public static bool IsUniversityEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            // Matches @*.edu.vn (e.g., student@hcmut.edu.vn, faculty@tdtu.edu.vn)
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.edu\.vn$", RegexOptions.IgnoreCase);
        }

        public static bool IsStrongPassword(string password, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Password cannot be empty.";
                return false;
            }

            if (password.Length < 8)
            {
                errorMessage = "Password must be at least 8 characters long.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errorMessage = "Password must contain at least one number.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
            {
                errorMessage = "Password must contain at least one special character.";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
}
