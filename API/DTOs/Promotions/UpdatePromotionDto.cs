using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Promotions
{
    public class UpdatePromotionDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Le pourcentage doit être entre 0 et 100")]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
