using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace AWS.Cognito.Authorization.Authentication.MVP.Models
{
    /// The RoleEdit class is used to represent the Role and the details of the Users in the Identity System.
    public class RoleEdit
    {
        public IdentityRole Role { get; set; }
        public IEnumerable<User> Members { get; set; }
        public IEnumerable<User> NonMembers { get; set; }
    }
}