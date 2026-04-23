using System.ComponentModel.DataAnnotations;

namespace TechGearShop_V1.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
        
        /// <summary>Return URL to redirect after successful login</summary>
        public string? ReturnUrl { get; set; }
    }
}
