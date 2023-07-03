using AutoMapper;
using Microsoft.EntityFrameworkCore;
using CodeBaseOne.EfCore;
using CodeBaseOne.Models.Dto;
using CodeBaseOne.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Services.Concrete
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public class ProductRepository : IProductRepository
    {
        private readonly EF_DataContext _context;
        private readonly IMapper _mapper;

        /// <inheritdoc />
        public ProductRepository(EF_DataContext context, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public bool DeleteProduct(ProductDto product)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool DeleteProduct(int id)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<ProductDto?> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id) ?? throw new ArgumentNullException(nameof(id));
                var returnProductDto = _mapper.Map<ProductDto>(product);
                return returnProductDto = _mapper.Map<ProductDto>(product);
            } 
            catch(Exception ex)
            {
                throw new ArgumentException("product service: GetProduct exception", ex);
            }
        }

        /// <inheritdoc />
        public async Task<List<ProductDto>?> GetProducts()
        {
            try
            {
                var products = await _context.Products.OrderBy(p => p.Id).ToListAsync() ?? throw new ArgumentNullException();
                return products.Select(product => _mapper.Map<ProductDto>(product)).ToList();
            }
            catch (Exception ex)
            {
                throw new ArgumentException("product service: GetProducts exception", ex);
            }
        }

        /// <inheritdoc />
        public async Task<Product?> AddProduct(ProductDto productDto)
        {
            if (productDto == null) throw new ArgumentNullException();

            try
            {
                var productToAdd = _mapper.Map<Product>(productDto);
                var newProduct = await _context.Products.AddAsync(productToAdd);
                return newProduct.Entity;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("product service: AddProduct exception", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> Save()
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                int recordsSaved;

                try
                {
                    recordsSaved = await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    //log ex.InnerException; - i.e. if a db level constraint is violated - e.g. unique field
                    return false;
                }

                transaction.Commit();
                // should auto-rollback if commit transaction fails or is disposed of

                return recordsSaved >= 0;
            }

            catch (Exception ex)
            {
                throw new ArgumentException("product service: Save exception", ex);
            }
        }

        /// <inheritdoc />
        public bool UpdateProduct(ProductDto product)
        {
            throw new NotImplementedException();
        }
    }
}
