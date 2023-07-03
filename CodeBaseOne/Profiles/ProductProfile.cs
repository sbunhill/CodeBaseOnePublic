using AutoMapper;
using CodeBaseOne.Models.Dto;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Profiles
{
    /// <summary>
    /// automapper profile
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ProductProfile : Profile
    {
        /// <summary>
        /// automapper - product entity to dto and vice versa
        /// </summary>
        public ProductProfile()
        {
            CreateMap<EfCore.Product, ProductDto>();
            CreateMap<ProductDto, EfCore.Product>();
        }
    }
}
