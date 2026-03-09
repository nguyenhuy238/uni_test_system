using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Application.Models
{
    public class ProfileViewModel
    {
        [Required, StringLength(200)]
        public string Id { get; set; } = "";

        [Display(Name = "Họ tên")]
        [Required, StringLength(200)]
        public string Name { get; set; } = "";

        [Display(Name = "Email (dùng để đăng nhập)")]
        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = "";

        [Display(Name = "Năm học")]
        [Required, StringLength(50)]
        public string AcademicYear { get; set; } = "1";

        [Display(Name = "Lớp")]
        [StringLength(100)]
        public string StudentClassId { get; set; } = "";

        [Display(Name = "Khoa")]
        [StringLength(100)]
        public string FacultyName { get; set; } = "";

        [Display(Name = "Chuyên ngành")]
        [StringLength(100)]
        public string Major { get; set; } = "";

        // Chỉ hiển thị đọc-only
        public string RoleName { get; set; } = "";

        public string? AvatarUrl { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public Microsoft.AspNetCore.Http.IFormFile? AvatarFile { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, StringLength(200)]
        public string UserId { get; set; } = "";

        [Display(Name = "Mật khẩu hiện tại")]
        [Required, DataType(DataType.Password)]
        public string OldPassword { get; set; } = "";

        [Display(Name = "Mật khẩu mới")]
        [Required, StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Display(Name = "Xác nhận mật khẩu mới")]
        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu chưa khớp.")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}
