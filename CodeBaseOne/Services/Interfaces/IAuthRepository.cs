using CodeBaseOne.Models.Dto;
using CodeBaseOne.Models.ServiceResponse;

namespace CodeBaseOne.Services.Interfaces
{
    /// <summary>
    /// auth service layer
    /// </summary>
    public interface IAuthRepository
    {
        /// <summary>
        /// register a user with unique email and strong password
        /// password should be hashed
        /// </summary>
        Task<ServiceResponse> Register(UserLoginDto user);
        /// <summary>
        /// login service layer method
        /// user logs in with email and password
        /// password hashes compared
        /// </summary>
        Task<ServiceResponse> Login(UserLoginDto user);
        /// <summary>
        /// generate and return new JWT and new refresh token using existing refresh token
        /// existing refresh token should first be validated
        /// </summary>
        Task<ServiceResponse> RefreshToken(string refreshToken);
        /// <summary>
        /// return user id from token claims
        /// </summary>
        string? GetUserIdFromToken(string token);
    }
}
