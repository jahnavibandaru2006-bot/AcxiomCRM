using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class UserManagementViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "SalesExecutive";
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "SalesExecutive";

        public bool IsActive { get; set; }
    }
}
