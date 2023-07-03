using Microsoft.AspNetCore.Mvc;
using CodeBaseOne.Models.Dto;
using CodeBaseOne.Models.ServiceResponse;// log refresh.Message;
using CodeBaseOne.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace JwtAPI.Controllers
{
    /// <summary>
    /// ToDo
    /// </summary>
    [Route("api/auth")]
    [ApiController]
    [ExcludeFromCodeCoverage]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<AuthController> _logger;
        private readonly IHttpContextAccessor _accessor;
        private readonly string? _clientIp;

        /// <summary>
        /// ToDo
        /// </summary>
        public AuthController(IAuthRepository authRepository, ILogger<AuthController> logger, IHttpContextAccessor accessor)
        {
            _authRepository = authRepository;
            _logger = logger;
            _accessor = accessor;
            _clientIp = _accessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }


        /// <summary>
        /// user registers with email address and password
        /// </summary>
        /// <param name="userLoginDto">request object - dto</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/auth/register
        ///     {
        ///        "email": "test01@example.com,
        ///        "password": "@dequatelyStr0ng1",
        ///     }
        ///
        /// </remarks>
        /// <response code="200">User created - new user id is returned</response>
        /// <response code="400">Bad request - reason will be logged but not returned</response>
        /// <response code="500">Exception - reason will be logged but not returned</response>
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] UserLoginDto userLoginDto)
        {
            // not wrapped in try-catch because Exception Middleware will catch log and handle exceptions

            ServiceResponse register = await _authRepository.Register(userLoginDto) ??
                throw new ArgumentNullException("auth controller: user registration returned null " + "ip: " + _clientIp + " " + userLoginDto.Email + " " + DateTime.Now);
                // exception handling middleware will catch and log this - returns 500 with no details to client

            if (!register.Success)
            {
                _logger.LogInformation(register.Message);
                _logger.LogInformation("auth controller: user registration failed " + "ip: " + _clientIp + " " + userLoginDto.Email + " " + DateTime.Now);
                return BadRequest();
            }

            return register.User == null ? BadRequest() : Ok(register.User.Id);
        }


        /// <summary>
        /// user logs in with registered email address and password
        /// </summary>
        /// <param name="userLoginDto">request object - dto</param>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/auth/login 
        ///     {
        ///        "email": "test01@example.com,
        ///        "password": "@dequatelyStr0ng1",
        ///     }
        ///
        /// </remarks>
        /// <response code="200">JWT returned + sets HttpOnly cookie containing refresh token</response>
        /// <response code="401">Unauthorized - reason will be logged but not returned</response>
        /// <response code="500">Exception - reason will be logged but not returned</response>
        [HttpPost("login")]
        public async Task<ActionResult<string?>> Login([FromBody] UserLoginDto userLoginDto)
        {
            // not wrapped in try-catch because Exception Middleware will catch log and handle exceptions

            ServiceResponse login = await _authRepository.Login(userLoginDto) ??
                throw new ArgumentNullException("auth controller: login - returned null " + "ip: " + _clientIp + " " + userLoginDto.Email + " " + DateTime.Now);
                // exception handling middleware will catch and log this - returns 500 with no details to client

            if (!login.Success)
            { 
                // the reason login failed is logged - but the client is not told
                _logger.LogInformation(login.Message);
                _logger.LogInformation("auth controller: login failed " + "ip: " + _clientIp + " " + userLoginDto.Email + " " + DateTime.Now);
                return Unauthorized();
            }

            var refreshToken = login.RefreshToken ??
                throw new ArgumentNullException("auth controller: login failed - refresh token null " + "ip: " + _clientIp + " " + userLoginDto.Email + " " + DateTime.Now);
                // exception handling middleware will catch and log this - return 500 with no details to client

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = refreshToken.Expires
            };

            if (refreshToken.Token == null)
            {
                // the reason login failed is logged - but the client is not told why
                _logger.LogInformation(login.Message);
                _logger.LogInformation("auth controller: refresh token is null " + "ip: " + _clientIp + " " + userLoginDto.Email + " " + DateTime.Now);
                return Unauthorized();
            }

            Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOptions);
            _logger.LogInformation("User Id#" + login?.User?.Id + " logged in " + "ip: " + _clientIp + " "+ DateTime.Now);

            return login?.Token;
        }

        /// <summary>
        /// uses HttpOnly cookie which contains refresh token
        /// </summary>
        /// <response code="200">JWT returned + sets HttpOnly cookie containing new refresh token</response>
        /// <response code="401">Unauthorized - reason will be logged but not returned</response>
        /// <response code="500">Exception - reason will be logged but not returned</response>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<string?>> RefreshToken()
        {
            // not wrapped in try-catch because Exception Middleware will catch log and handle exceptions

            var refreshToken = Request.Cookies["refreshToken"] ??
                throw new ArgumentNullException("auth controller: refresh - invalid refresh token" + "ip: " + _clientIp + " " + DateTime.Now);
                // exception handling middleware will catch and log this - returns 500 with no details to client


            ServiceResponse refresh = await _authRepository.RefreshToken(refreshToken) ??
                throw new ArgumentNullException("auth controller: refresh - returned null " + "ip: " + _clientIp + " " + DateTime.Now);
                // exception handling middleware will catch and log this - returns 500 with no details to client

            if (!refresh.Success)
            {
                // the reason refresh failed is logged - but the client is not told why
                _logger.LogInformation(refresh.Message);
                _logger.LogInformation("auth controller: refresh failed " + "ip: " + _clientIp + " " + DateTime.Now);
                return Unauthorized();
            }

            if(refresh.RefreshToken?.Token != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = refresh.RefreshToken.Expires,
                };
                Response.Cookies.Append("refreshToken", refresh.RefreshToken.Token, cookieOptions);
            }
            
            return refresh.Token;
        }
    }
}
