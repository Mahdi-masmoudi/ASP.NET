using API.DTOs.Auth;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Inscription d'un nouvel utilisateur (rôle User uniquement)
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Les utilisateurs publics ne peuvent s'inscrire qu'en tant que "User"
            registerDto.Role = "User";

            var result = await _authService.RegisterAsync(registerDto);

            if (result == null)
                return BadRequest(new { message = "Un utilisateur avec cet email existe déjà." });

            return Ok(result);
        }

        /// <summary>
        /// Connexion d'un utilisateur existant
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
                return Unauthorized(new { message = "Email ou mot de passe incorrect." });

            return Ok(result);
        }
    }
}
