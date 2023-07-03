using CodeBaseOne.EfCore;
using CodeBaseOne.Models.Dto;

namespace CodeBaseOne.Services.Interfaces
{
    /// <summary>
    /// todo
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// todo
        /// </summary>
        Task<List<ProductDto>?> GetProducts();
        /// <summary>
        /// todo
        /// </summary>
        Task<ProductDto?> GetProduct(int id);
        /// <summary>
        /// todo
        /// </summary>
        Task<Product?> AddProduct(ProductDto productModel);
        /// <summary>
        /// todo
        /// </summary>
        bool DeleteProduct(ProductDto product);
        /// <summary>
        /// todo
        /// </summary>
        bool DeleteProduct(int id);
        /// <summary>
        /// todo
        /// </summary>
        bool UpdateProduct(ProductDto product);
        /// <summary>
        /// todo
        /// </summary>
        Task<bool> Save();
    }
}
