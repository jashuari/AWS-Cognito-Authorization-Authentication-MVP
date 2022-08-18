using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace AWS.Cognito.Authorization.Authentication.MVP.Models
{
    //Class for User
    public class User: IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }

 
        public User() 
        {
            IsActive = true;
        }
    }
}
