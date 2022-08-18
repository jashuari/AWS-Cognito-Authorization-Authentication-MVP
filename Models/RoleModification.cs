using System.ComponentModel.DataAnnotations;

namespace Identity.Models
{
    /// The RoleModification class code represents the changes that will be done to a role.
    public class RoleModification
    {

        [Required]
        public string RoleName { get; set; }
        public string RoleId { get; set; }
        public string[] AddIds { get; set; }
        public string[] DeleteIds { get; set; }
    }
}