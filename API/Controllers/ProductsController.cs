using API.DTOs.Products;
using API.Entities.Oltp;
using API.Repositories.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;

        public ProductsController(IUnitOfWork unitOfWork, IImageService imageService)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
        }

        /// <summary>
        /// Récupérer tous les produits
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var products = await _unitOfWork.Products.GetProductsWithCategoryAsync();
            var dtos = products.Select(MapToDto);
            return Ok(dtos);
        }

        /// <summary>
        /// Récupérer un produit par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetById(int id)
        {
            var product = await _unitOfWork.Products.GetProductWithCategoryAsync(id);
            if (product == null)
                return NotFound(new { message = "Produit non trouvé." });

            return Ok(MapToDto(product));
        }

        /// <summary>
        /// Récupérer les produits par catégorie
        /// </summary>
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetByCategory(int categoryId)
        {
            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
            return Ok(products.Select(MapToDto));
        }

        /// <summary>
        /// Rechercher des produits par mot-clé
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> Search([FromQuery] string keyword)
        {
            var products = await _unitOfWork.Products.SearchProductsAsync(keyword);
            return Ok(products.Select(MapToDto));
        }

        /// <summary>
        /// Créer un nouveau produit (Admin uniquement - affecté à sa société)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> Create([FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyId = GetCompanyId();
            if (companyId == null)
                return BadRequest(new { message = "Vous devez être associé à une société pour créer un produit." });

            var imageUrl = await _imageService.SaveImageAsync(dto.Image, "products");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                ImageUrl = imageUrl,
                CategoryId = dto.CategoryId,
                CompanyId = companyId.Value,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Products.GetProductWithCategoryAsync(product.ProductId);
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, MapToDto(created!));
        }

        /// <summary>
        /// Mettre à jour un produit (Admin uniquement - même société)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> Update(int id, [FromForm] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Produit non trouvé." });

            var companyId = GetCompanyId();
            if (companyId == null || product.CompanyId != companyId.Value)
                return Forbid();

            // Si une nouvelle image est envoyée, supprimer l'ancienne
            if (dto.Image != null)
            {
                _imageService.DeleteImage(product.ImageUrl);
                product.ImageUrl = await _imageService.SaveImageAsync(dto.Image, "products");
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.Products.GetProductWithCategoryAsync(id);
            return Ok(MapToDto(updated!));
        }

        /// <summary>
        /// Supprimer un produit (Admin uniquement - même société)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Produit non trouvé." });

            var companyId = GetCompanyId();
            if (companyId == null || product.CompanyId != companyId.Value)
                return Forbid();

            _imageService.DeleteImage(product.ImageUrl);
            _unitOfWork.Products.Delete(product);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // ==================== Helpers ====================

        private int? GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return companyIdClaim != null ? int.Parse(companyIdClaim) : null;
        }

        private static ProductDto MapToDto(Product product)
        {
            var hasPromo = product.Promotion != null && product.Promotion.IsActive
                && product.Promotion.StartDate <= DateTime.UtcNow
                && product.Promotion.EndDate >= DateTime.UtcNow;

            return new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                CreatedAt = product.CreatedAt,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                PromotionId = product.PromotionId,
                PromotionName = hasPromo ? product.Promotion!.Name : null,
                DiscountPercentage = hasPromo ? product.Promotion!.DiscountPercentage : null,
                DiscountedPrice = hasPromo ? product.Price - (product.Price * product.Promotion!.DiscountPercentage / 100) : null,
                CompanyId = product.CompanyId,
                CompanyName = product.Company?.Name ?? string.Empty
            };
        }
    }
}
