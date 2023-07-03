using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CodeBaseOne.EfCore;
using CodeBaseOne.Models.Dto;
using CodeBaseOne.Models.ServiceResponse;
using CodeBaseOne.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CodeBaseOne.Services.Concrete
{
    /// <inheritdoc />
    public class AuthRepository : IAuthRepository
    {
        /// <inheritdoc />
        private readonly EF_DataContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly int _refreshTokenLifeTimeMins;
        private readonly int _authTokenLifeTimeMins;

        /// <inheritdoc />
        public AuthRepository(EF_DataContext context, IMapper mapper, IConfiguration configuration, TokenValidationParameters tokenValidationParameters)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            _configuration = configuration;
            _tokenValidationParameters = tokenValidationParameters;
            _refreshTokenLifeTimeMins = 10080; // hardcoded for now - but we could get from settings
            _authTokenLifeTimeMins = 60; // hardcoded for now - but we could get from settings
        }


        /// <inheritdoc />
        public async Task<ServiceResponse> Register(UserLoginDto userDto)

        {
            ServiceResponse response = new ServiceResponse();

            // note - in a deployment there would be an extra series of steps requiring the user to verify the email address as follows
            // we initial create the User with a Provisional status
            // we send a unique code / link based on a uuid
            // an end point accepts that uuid and the user account status is set to Verified
            // additionally however - we likely need that uuid to only be valid for x time - in which case a token might be used as an alternative
            // either way - if the time period has expired then the use will need to re-verify - first entering again their email address

            try
            {
                if (userDto == null) throw new ArgumentNullException("null object");

                if (!IsValidMailAddressFormat(userDto.Email))
                {
                    response.Success = false;
                    response.Message = response.Message = "auth service: register - invalid email address"; // logged - never returned to client
                }

                else if (await UserExists(userDto.Email))
                {
                    response.Success = false;
                    response.Message = response.Message = "auth service: register - email already exists"; // logged - never returned to client
                }

                // 8 + chars 1 + number 1 + upper 1 + lower 1 + special char
                else if (!IsValidPasswordStrength(userDto.Password))
                {
                    response.Success = false;
                    response.Message = response.Message = "auth service: register - password strength"; // logged - never returned to client
                }

                else
                {
                    CreatePasswordHash(userDto.Password, out byte[] passwordHash, out byte[] passwordSalt);
                    var userToAdd = _mapper.Map<User>(userDto);
                    userToAdd.PasswordHash = passwordHash;
                    userToAdd.PasswordSalt = passwordSalt;
                    var newUser = await _context.Users.AddAsync(userToAdd);
                    var saveUser = await _context.SaveChangesAsync();
                    if (saveUser > 0)
                    {
                        response.Success = true;
                        response.User = newUser.Entity;
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = response.Message = "auth service: register - db save failed"; // logged - never returned to client
                    }
                }

                return response;
            }

            catch (Exception ex)
            {
                // middleware will catch and log this - not returned to client
                throw new ArgumentException("auth service: register exception", ex);
            }
        }


        /// <inheritdoc />
        public async Task<ServiceResponse> Login(UserLoginDto userLoginDto)
 
        {
            ServiceResponse response = new ServiceResponse();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userLoginDto.Email.ToLower());
                
                if (user == null)
                {
                    response.Success = false;
                    response.Message = "auth service: login - user does not exist"; // logged - never returned to client
                }

                else if (!VerifyPasswordHash(userLoginDto.Password, user.PasswordHash, user.PasswordSalt))
                {
                    response.Success = false;
                    response.Message = "auth service: login - invalid password"; // logged - never returned to client
                }
                else
                {
                    response.Success = true;
                    response.Token = CreateToken(user, _authTokenLifeTimeMins, TokenType.Auth);
                    response.RefreshToken = await GenerateRefreshTokenObject(user);
                    response.User = user;
                }

            }
            catch (Exception ex)
            {
                // middleware will catch and log this - not returned to client
                throw new Exception("auth service: login exception");
            }

            return response;
        }

        /// <inheritdoc />
        public async Task<ServiceResponse> RefreshToken(string refreshToken)
        {
            ServiceResponse response = new ServiceResponse();

            try        
            {
                var validatedToken = GetPrincipalFromToken(refreshToken);
                if (validatedToken == null)
                {
                    response.Success = false;
                    response.Message = "auth service: refresh token -      invalid token from cookie"; // logged - never returned to client
                    return response;
                }
                var expiryDateUnixFromCookie = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDateTimeUtcFromCookie = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddSeconds(expiryDateUnixFromCookie);

                var userIdFromCookie = validatedToken?.Claims?.Where(c => c.Type == ClaimTypes.NameIdentifier)
                   .Select(c => c.Value).SingleOrDefault() ??
                   throw new ArgumentNullException("auth service: refresh token - User Id not returned from refresh cookie");
                   // exception handling middleware will catch and log this - returns 500 with no details to client

                var storedRefreshToken = await _context.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.Token == refreshToken);
                if (storedRefreshToken == null)
                {
                    response.Success = false;
                    response.Message = "auth service: refresh token - null token"; // logged - never returned to client
                    return response;
                }

                if (NormalizeDateTime(storedRefreshToken.Expires) != NormalizeDateTime(expiryDateTimeUtcFromCookie))
                {
                    response.Success = false;
                    response.Message = "auth service: refresh token expiry time does not match db record"; // logged - never returned to client
                    return response;
                }

                if (Int32.Parse(userIdFromCookie) != storedRefreshToken.User.Id)
                {
                    response.Success = false;
                    response.Message = "auth service: refresh token user does not match db record"; // logged - never returned to client
                    return response;
                }

                if ((DateTime.UtcNow > storedRefreshToken.Expires) ||
                    (storedRefreshToken.Invalidated == true) ||
                    (storedRefreshToken.Used == true))
                {
                    response.Success = false;
                    response.Message = "auth service: refresh token - invalid token"; // logged - never returned to client
                    return response;
                }

                storedRefreshToken.Used = true;
                _context.RefreshTokens.Update(storedRefreshToken);
                await _context.SaveChangesAsync();

                // ToDo(SS) - should I be using claims here?
                var user = storedRefreshToken.User;

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "auth service: refresh token - null user"; // logged - never returned to client
                    return response;
                }

                response.Success = true;
                response.User = user;
                response.RefreshToken = await GenerateRefreshTokenObject(user);
                response.Token = CreateToken(user, _authTokenLifeTimeMins, TokenType.Auth); // short lived auth token 
                return response;
            }
            catch (Exception ex)
            {
                // middleware will catch and log this - not returned to client
                throw new Exception("auth service: refresh exception", ex);
            }
        }

        /// <inheritdoc />
        public string? GetUserIdFromToken(string accessToken)
        {
            var principal = GetPrincipalFromToken(accessToken);
            return principal?.Claims?.Where(c => c.Type == ClaimTypes.NameIdentifier)
                .Select(c => c.Value).SingleOrDefault();
        }


        private async Task<bool> UserExists(string emailAddress)
        {
            try
            {
                if(await _context.Users.AnyAsync(u => u.Email.ToLower() == emailAddress.ToLower()))
                {
                    return true;
                }
            } 
            catch(Exception ex)
            {
                // log exception
            }
            return false;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        // out keyword means that we do not have to return anything - the hash & salt are already availble in Register method
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }

        private bool IsValidMailAddressFormat(string emailAddress)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(emailAddress))
                    return false;

                var emailValidation = new EmailAddressAttribute();
                var emailInitalFormatCheck = MailAddress.TryCreate(emailAddress, out _);

                if (emailInitalFormatCheck)
                {
                    // noting here the issues around using Regex to validate email format
                    var mailformat = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
                    return mailformat.IsMatch(emailAddress);
                } else
                {
                    return false;
                }
            }
            catch(Exception ex)
            {
                // middleware will catch and log this - not returned to client
                throw new ArgumentException("auth service IsValidMailAddressFormat exception", ex);
            }
        }

        private bool IsValidPasswordStrength(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                    return false;
                // 8+ chars 1+ number 1+ upper 1+ lower 1+ special char
                var passwordRules = new Regex(@"^(?=.*[0-9])(?=.*[A-Z])(?=.*[a-z])(?=.*[!@#$&*]).{8,}$");
                return passwordRules.IsMatch(password);
            }
            catch(Exception ex)
            {
                // middleware will catch and log this - not returned to client
                throw new ArgumentException("auth service IsValidMailAddressFormat exception", ex);
            }
        }


        private string CreateToken(User user, int minutes, TokenType tokenType)
        {
            List<Claim> claims = new List<Claim>();

            // Refresh token should not include Claims - it should not be possible to use Refresh as Auth
            if (tokenType == TokenType.Auth)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, "Admin")); // hardcoded for now - but this will come from the User table
            } else if (tokenType == TokenType.Refresh)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            }

            var appSettingsToken = _configuration.GetSection("AppSettings:Token").Value;
            if (appSettingsToken == null) { throw new Exception("AppSettings token is null"); }
            SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(appSettingsToken));
            SigningCredentials creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(minutes),
                SigningCredentials = creds
            };

            JwtSecurityTokenHandler tokenhandler = new JwtSecurityTokenHandler();
            SecurityToken token = tokenhandler.CreateToken(tokenDescriptor); 

            return tokenhandler.WriteToken(token);
        }

        private async Task<RefreshToken> GenerateRefreshTokenObject(User user)
        {
            var refreshToken = new RefreshToken
            {
                User = user,
                Token = CreateToken(user, _refreshTokenLifeTimeMins, TokenType.Refresh),
                //Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddMinutes(_refreshTokenLifeTimeMins),
                Created = DateTime.UtcNow
            };
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        private ClaimsPrincipal? GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
                if (!IsJwtWithValidSecurityAlgorith(validatedToken))
                {
                    return null;
                }
                return principal;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorith(SecurityToken validatedToken)
        {
            if (validatedToken is JwtSecurityToken jwtSecurityToken)
            {
                return jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }


        private DateTime NormalizeDateTime(DateTime dateTime)
        {
            // we don't want milliseconds - since these will be zero in the cookie
            return new DateTime(year: dateTime.Year, month: dateTime.Month, day: dateTime.Day, hour: dateTime.Hour, minute: dateTime.Minute, second: dateTime.Second, kind: DateTimeKind.Utc);
        }
      
    }

    enum TokenType
    {
        Refresh,
        Auth
    }
}
