using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Companies
{
    public class CompanyDto
    {
        public int CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UserCount { get; set; }
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
    }
}
