using API.Data;
using API.Entities.Oltp;
using API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories.Implementations
{
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(OltpDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .Include(p => p.Promotion)
                .Include(p => p.Company)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsWithCategoryAsync()
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Promotion)
                .Include(p => p.Company)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithCategoryAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Category)
                .Include(p => p.Promotion)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string keyword)
        {
            return await _dbSet
                .Where(p => p.Name.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)))
                .Include(p => p.Category)
                .Include(p => p.Promotion)
                .Include(p => p.Company)
                .ToListAsync();
        }
    }
}
