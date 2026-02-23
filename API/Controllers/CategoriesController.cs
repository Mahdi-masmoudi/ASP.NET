using API.DTOs.Categories;
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
    public class CategoriesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;

        public CategoriesController(IUnitOfWork unitOfWork, IImageService imageService)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
        }

        /// <summary>
        /// Récupérer toutes les catégories
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var products = await _unitOfWork.Products.GetAllAsync();
            var companies = await _unitOfWork.Companies.GetAllAsync();

            var dtos = categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                CompanyId = c.CompanyId,
                CompanyName = companies.FirstOrDefault(co => co.CompanyId == c.CompanyId)?.Name ?? string.Empty,
                ProductCount = products.Count(p => p.CategoryId == c.CategoryId)
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Récupérer une catégorie par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { message = "Catégorie non trouvée." });

            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(id);
            var company = await _unitOfWork.Companies.GetByIdAsync(category.CompanyId);

            return Ok(new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                CompanyId = category.CompanyId,
                CompanyName = company?.Name ?? string.Empty,
                ProductCount = products.Count()
            });
        }

        /// <summary>
        /// Créer une catégorie (Admin uniquement - affectée à sa société)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryDto>> Create([FromForm] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyId = GetCompanyId();
            if (companyId == null)
                return BadRequest(new { message = "Vous devez être associé à une société pour créer une catégorie." });

            var imageUrl = await _imageService.SaveImageAsync(dto.Image, "categories");

            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = imageUrl,
                CompanyId = companyId.Value
            };

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var company = await _unitOfWork.Companies.GetByIdAsync(companyId.Value);

            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                CompanyId = category.CompanyId,
                CompanyName = company?.Name ?? string.Empty,
                ProductCount = 0
            });
        }

        /// <summary>
        /// Mettre à jour une catégorie (Admin uniquement - même société)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CategoryDto>> Update(int id, [FromForm] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { message = "Catégorie non trouvée." });

            var companyId = GetCompanyId();
            if (companyId == null || category.CompanyId != companyId.Value)
                return Forbid();

            // Si une nouvelle image est envoyée, supprimer l'ancienne
            if (dto.Image != null)
            {
                _imageService.DeleteImage(category.ImageUrl);
                category.ImageUrl = await _imageService.SaveImageAsync(dto.Image, "categories");
            }

            category.Name = dto.Name;
            category.Description = dto.Description;

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var company = await _unitOfWork.Companies.GetByIdAsync(companyId.Value);
            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(id);

            return Ok(new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                CompanyId = category.CompanyId,
                CompanyName = company?.Name ?? string.Empty,
                ProductCount = products.Count()
            });
        }

        /// <summary>
        /// Supprimer une catégorie (Admin uniquement - même société)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                return NotFound(new { message = "Catégorie non trouvée." });

            var companyId = GetCompanyId();
            if (companyId == null || category.CompanyId != companyId.Value)
                return Forbid();

            _imageService.DeleteImage(category.ImageUrl);
            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        // ==================== Helpers ====================

        private int? GetCompanyId()
        {
            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            return companyIdClaim != null ? int.Parse(companyIdClaim) : null;
        }
    }
}
