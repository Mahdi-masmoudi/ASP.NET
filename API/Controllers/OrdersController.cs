using API.DTOs.Orders;
using API.Entities.Oltp;
using API.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrdersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Récupérer toutes les commandes (Admin uniquement)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
        {
            var orders = await _unitOfWork.Orders.GetOrdersWithDetailsAsync();
            return Ok(orders.Select(MapToDto));
        }

        /// <summary>
        /// Récupérer une commande par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetById(int id)
        {
            var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
            if (order == null)
                return NotFound(new { message = "Commande non trouvée." });

            // Vérifier que l'utilisateur accède à sa propre commande (sauf Admin)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (order.UserId != userId && userRole != "Admin" && userRole != "SuperAdmin")
                return Forbid();

            return Ok(MapToDto(order));
        }

        /// <summary>
        /// Récupérer les commandes de l'utilisateur connecté
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var orders = await _unitOfWork.Orders.GetOrdersByUserAsync(userId);
            return Ok(orders.Select(MapToDto));
        }

        /// <summary>
        /// Créer une nouvelle commande
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = dto.ShippingAddress,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            };

            decimal totalAmount = 0;

            foreach (var item in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                    return BadRequest(new { message = $"Produit avec l'ID {item.ProductId} non trouvé." });

                if (product.StockQuantity < item.Quantity)
                    return BadRequest(new { message = $"Stock insuffisant pour le produit '{product.Name}'." });

                var subtotal = product.Price * item.Quantity;
                totalAmount += subtotal;

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    Subtotal = subtotal
                });

                // Mettre à jour le stock
                product.StockQuantity -= item.Quantity;
                _unitOfWork.Products.Update(product);
            }

            order.TotalAmount = totalAmount;

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Orders.GetOrderWithDetailsAsync(order.OrderId);
            return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, MapToDto(created!));
        }

        // ==================== Mapping ====================

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                ShippingAddress = order.ShippingAddress,
                UserId = order.UserId,
                UserFullName = order.User?.FullName ?? string.Empty,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Subtotal = oi.Subtotal
                }).ToList()
            };
        }
    }
}
