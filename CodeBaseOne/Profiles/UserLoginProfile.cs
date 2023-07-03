using AutoMapper;
using CodeBaseOne.Models.Dto;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Profiles
{
    /// <summary>
    /// automapper profile
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class UserLoginProfile : Profile
    {
        /// <summary>
        /// automapper - user entity to dto and vice versa
        /// </summary>
        public UserLoginProfile()
        {
            CreateMap<EfCore.User, UserLoginDto>();
            CreateMap<UserLoginDto, EfCore.User>();
        }
    }
}
