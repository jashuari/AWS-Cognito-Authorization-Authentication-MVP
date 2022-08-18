using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace AWS.Cognito.Authorization.Authentication.MVP.Models
{
    public class LoginAwsModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}