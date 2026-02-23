using System.ComponentModel.DataAnnotations;

namespace API.DTOs.Promotions
{
    public class AssignProductsDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "Au moins un produit doit être sélectionné")]
        public List<int> ProductIds { get; set; } = new();
    }
}
