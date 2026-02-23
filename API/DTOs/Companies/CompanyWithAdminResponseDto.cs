namespace API.DTOs.Companies
{
    public class CompanyWithAdminResponseDto
    {
        // ----- Société -----
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

        // ----- Admin -----
        public int AdminUserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string EmailAdmin { get; set; } = string.Empty;
        public string? PhoneNumberAdmin { get; set; }
        public string? AddressAdmin { get; set; }
        public string? CityAdmin { get; set; }
        public string Role { get; set; } = "Admin";
        public DateTime CreatedAtAdmin { get; set; }
    }
}

