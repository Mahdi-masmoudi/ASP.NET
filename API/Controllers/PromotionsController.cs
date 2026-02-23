using API.DTOs.Promotions;
using API.DTOs.Products;
using API.Entities.Oltp;
using API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class PromotionsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public PromotionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Récupérer toutes les promotions
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<PromotionDto>>> GetAll()
        {
            var promotions = await _unitOfWork.Promotions.GetAllAsync();
            var products = await _unitOfWork.Products.GetAllAsync();

            var dtos = promotions.Select(p => new PromotionDto
            {
                PromotionId = p.PromotionId,
                Name = p.Name,
                Description = p.Description,
                DiscountPercentage = p.DiscountPercentage,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                ProductCount = products.Count(pr => pr.PromotionId == p.PromotionId)
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Récupérer une promotion par ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<PromotionDto>> GetById(int id)
        {
            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion non trouvée." });

            var products = await _unitOfWork.Products.FindAsync(p => p.PromotionId == id);

            return Ok(new PromotionDto
            {
                PromotionId = promotion.PromotionId,
                Name = promotion.Name,
                Description = promotion.Description,
                DiscountPercentage = promotion.DiscountPercentage,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive,
                CreatedAt = promotion.CreatedAt,
                ProductCount = products.Count()
            });
        }

        /// <summary>
        /// Récupérer les produits d'une promotion
        /// </summary>
        [HttpGet("{id}/products")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetPromotionProducts(int id)
        {
            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion non trouvée." });

            var products = await _unitOfWork.Products.GetProductsWithCategoryAsync();
            var promoProducts = products.Where(p => p.PromotionId == id);

            var dtos = promoProducts.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                PromotionId = p.PromotionId,
                PromotionName = promotion.Name,
                DiscountPercentage = promotion.DiscountPercentage,
                DiscountedPrice = p.Price - (p.Price * promotion.DiscountPercentage / 100),
                CompanyId = p.CompanyId,
                CompanyName = p.Company?.Name ?? string.Empty
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Créer une promotion
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PromotionDto>> Create([FromBody] CreatePromotionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.EndDate <= dto.StartDate)
                return BadRequest(new { message = "La date de fin doit être postérieure à la date de début." });

            var promotion = new Promotion
            {
                Name = dto.Name,
                Description = dto.Description,
                DiscountPercentage = dto.DiscountPercentage,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Promotions.AddAsync(promotion);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = promotion.PromotionId }, new PromotionDto
            {
                PromotionId = promotion.PromotionId,
                Name = promotion.Name,
                Description = promotion.Description,
                DiscountPercentage = promotion.DiscountPercentage,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive,
                CreatedAt = promotion.CreatedAt,
                ProductCount = 0
            });
        }

        /// <summary>
        /// Mettre à jour une promotion
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PromotionDto>> Update(int id, [FromBody] UpdatePromotionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion non trouvée." });

            if (dto.EndDate <= dto.StartDate)
                return BadRequest(new { message = "La date de fin doit être postérieure à la date de début." });

            promotion.Name = dto.Name;
            promotion.Description = dto.Description;
            promotion.DiscountPercentage = dto.DiscountPercentage;
            promotion.StartDate = dto.StartDate;
            promotion.EndDate = dto.EndDate;
            promotion.IsActive = dto.IsActive;

            _unitOfWork.Promotions.Update(promotion);
            await _unitOfWork.SaveChangesAsync();

            var products = await _unitOfWork.Products.FindAsync(p => p.PromotionId == id);

            return Ok(new PromotionDto
            {
                PromotionId = promotion.PromotionId,
                Name = promotion.Name,
                Description = promotion.Description,
                DiscountPercentage = promotion.DiscountPercentage,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive,
                CreatedAt = promotion.CreatedAt,
                ProductCount = products.Count()
            });
        }

        /// <summary>
        /// Supprimer une promotion
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion non trouvée." });

            // Retirer la promotion de tous les produits associés
            var products = await _unitOfWork.Products.FindAsync(p => p.PromotionId == id);
            foreach (var product in products)
            {
                product.PromotionId = null;
                _unitOfWork.Products.Update(product);
            }

            _unitOfWork.Promotions.Delete(promotion);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Affecter des produits à une promotion
        /// </summary>
        [HttpPost("{id}/assign-products")]
        public async Task<ActionResult> AssignProducts(int id, [FromBody] AssignProductsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion non trouvée." });

            foreach (var productId in dto.ProductIds)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product == null)
                    return BadRequest(new { message = $"Produit avec l'ID {productId} non trouvé." });

                product.PromotionId = id;
                _unitOfWork.Products.Update(product);
            }

            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = $"{dto.ProductIds.Count} produit(s) affecté(s) à la promotion '{promotion.Name}'." });
        }

        /// <summary>
        /// Retirer des produits d'une promotion
        /// </summary>
        [HttpPost("{id}/remove-products")]
        public async Task<ActionResult> RemoveProducts(int id, [FromBody] AssignProductsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
            if (promotion == null)
                return NotFound(new { message = "Promotion non trouvée." });

            foreach (var productId in dto.ProductIds)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product != null && product.PromotionId == id)
                {
                    product.PromotionId = null;
                    _unitOfWork.Products.Update(product);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = $"{dto.ProductIds.Count} produit(s) retiré(s) de la promotion '{promotion.Name}'." });
        }
    }
}
