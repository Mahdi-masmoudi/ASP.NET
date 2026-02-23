using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Companies
{
    public class CreateCompanyDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        // Admin user to create for this company
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string AdminEmail { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? AdminPhoneNumber { get; set; }

        [StringLength(300)]
        public string? AdminAddress { get; set; }

        [StringLength(100)]
        public string? AdminCity { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
