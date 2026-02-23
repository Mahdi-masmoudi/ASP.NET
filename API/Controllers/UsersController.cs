using API.DTOs.Users;
using API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Récupérer tous les utilisateurs (SuperAdmin/Admin)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var companies = await _unitOfWork.Companies.GetAllAsync();

            var dtos = users.Select(u => MapToDto(u, companies));
            return Ok(dtos);
        }

        /// <summary>
        /// Récupérer un utilisateur par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Utilisateur non trouvé." });

            var companies = await _unitOfWork.Companies.GetAllAsync();
            return Ok(MapToDto(user, companies));
        }

        // ==================== Mapping ====================

        private static UserDto MapToDto(Entities.Oltp.User user, IEnumerable<Entities.Oltp.Company> companies)
        {
            return new UserDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                DateOfBirth = user.DateOfBirth,
                ProfileImageUrl = user.ProfileImageUrl,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                CompanyId = user.CompanyId,
                CompanyName = user.CompanyId.HasValue
                    ? companies.FirstOrDefault(c => c.CompanyId == user.CompanyId)?.Name
                    : null
            };
        }
    }
}
