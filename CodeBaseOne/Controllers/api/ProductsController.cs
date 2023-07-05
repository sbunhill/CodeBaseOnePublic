using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodeBaseOne.Models.Dto;
using CodeBaseOne.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Controllers.api
{
    /// <summary>
    /// ToDo
    /// </summary>
    [Authorize(Roles = "Admin")] // can test / demo this by changing the hard coded role value where the token is generated
    [Route("api/product")]
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productsRepository;
        private readonly IMapper _mapper;
        private readonly IAuthRepository _authRepository;

        /// <summary>
        /// This relates to Products entity - for demo purposes only
        /// </summary>
        public ProductsController(IProductRepository productsRepository, IMapper mapper, IAuthRepository authRepository)
        {
            _authRepository = authRepository; 
            _productsRepository = productsRepository;
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }


        /// <summary>
        /// authenticated and authorized user - get a product by id
        /// </summary>
        /// <response code="200">JSON object returned</response>
        /// <response code="401">Unauthorized - reason logged but not returned</response>
        /// <response code="403">Forbidden - valid credentials but insufficient privileges</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Exception - reason logged but not returned</response>
        [HttpGet]
        [Route("getproduct/{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _productsRepository.GetProduct(id);
            return product == null ? NotFound() : product;
        }

        /// <summary>
        /// authenticated and authorized user get all products
        /// </summary>
        /// <response code="200">JSON array returned - empty [] if none found</response>
        /// <response code="401">Unauthorized - reason logged but not returned</response>
        /// <response code="403">Forbidden - valid credentials but insufficient privileges</response>
        /// <response code="500">Exception - reason logged but not returned</response>
        [HttpGet]
        [Route("getproducts/")]
        public async Task<ActionResult<List<ProductDto>>> GetProducts()
        {
            // Here is a POC - for getting the User Id from the Claims in the token.
            // Since in many cases we would typically want the user Id as a starting point.
            // And we may want to directly access other Claims.
            string? accessToken = await HttpContext.GetTokenAsync("access_token");
            string UserId = _authRepository.GetUserIdFromToken(accessToken) ??
                throw new ArgumentNullException("products controller: getproducts - invalid refresh token");
                // exception handling middleware will catch and log this - returns 500 with no details to client

            var products = await _productsRepository.GetProducts();
            return products == null ? NotFound() : products;
        }

        /// <summary>
        /// authenticated and authorized user adds new product to db
        /// </summary>
        /// <param name="productDto">request object - dto</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/product/add
        ///     {
        ///        "name": "Type 1227",
        ///        "brand": "Anglepoise",
        ///        "size": 1,
        ///        "price": 210.00,
        ///        "note": "something"
        ///     }
        ///
        /// </remarks>
        /// <response code="200">JSON object returned: new product including id</response>
        /// <response code="400">Bad Request - context save returned zero - e.g. - name is not unique - or dto data annotations violations</response>
        /// <response code="401">Unauthorized - reason will be logged but not returned</response>
        /// <response code="403">Forbidden - valid credentials but insufficient privileges</response>
        /// <response code="500">Exception - reason will be logged but not returned</response>
        [HttpPost]
        [Route("add/")]
        public async Task<ActionResult<ProductDto>> AddProduct([FromBody] ProductDto productDto)
        {
            var addProduct = await _productsRepository.AddProduct(productDto);
            if(addProduct != null)
            {
                var savedProduct = await _productsRepository.Save();
                return savedProduct == true ? _mapper.Map<ProductDto>(addProduct) : BadRequest("check input data");
            }
            else
            {
                return BadRequest("logged");
            }
        }

    }
}
