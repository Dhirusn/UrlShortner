using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models.ViewModels
{
    public class LoginViewModel
    {
        public string? ReturnUrl { get; set; } = "/";
        [Required]
        public string UserNameOrEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
