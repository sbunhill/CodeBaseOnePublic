using CodeBaseOne.Services.Concrete;
using AutoMapper;
using CodeBaseOne.EfCore;
using CodeBaseOne.Models.Dto;
using CodeBaseOne.Profiles;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CodeBaseTests
{
    public class AuthTest : IDisposable
    {
        // Testing the public methods - i.e. the class interfaces which in turn call the private methods
        // Could test private methods via reflection - but this will generate code smells.

        // Here using an in memory db since the best advise including MS seems to advise against mocking DBContext
        // So arguably these are integration rather unit tests?

        // Recreate the in-memory db for each test - or else the tests are effectively sequential.

        private IConfigurationRoot _configuration;
        private Mapper _mapper;
        private UserLoginDto _validRegisteredUser;
        private UserLoginDto _invalidEmailFormat;
        private UserLoginDto _invalidPasswordStrength;
        private UserLoginDto _unregisteredUser;
        private UserLoginDto _wrongPassword;
        private TokenValidationParameters _tokenValidationParameters;
        private string _fakeToken;
       // private RefreshToken _dummyRefreshToken;


        public AuthTest() {
            var config = new Dictionary<string, string>
            {
                {"AppSettings:Token", "6eLG7GjEI9m4PXse/gCdBMX8z7XAU/kV9Ysi2uUUbSMq4TKu+6QzovkBuq6G7zi8C0DD4793E8C47EC9BD8208394B997B85D5294127E325F919989B9E3B6C5566B7}$tP#6Bdh0rDH9.BVh]f;*[9DIP!3f!'#(oWj`ymSr23=%jR~wg6D^>oT-(}TjL6QgByApq4ZqPkhQBn7dRpK2N7euhPmMhNEaB5KFWav7dJU9zP8hdEz6jIngVqfT6eLG7GjEI9m4PXse/gCdBMX8z7XAU/kV9Ysi2uUUbSMq4TKu+6QzovkBuq6G7zi8C0DD4793E8C47EC9BD8208394B997B85D5294127E325F919989B9E3B6C5566B7}$tP#6Bdh0rDH9.BVh]f;*[9DIP!3f!'#(oWj`ymSr23=%jR~wg6D^>oT-(}TjL6QgByApq4ZqPkhQBn7dRpK2N7euhPmMhNEaB5KFWav7dJU9zP8hdEz6jIngVqfT6eLG7GjEI9m4PXse/gCdBMX8z7XAU/kV9Ysi2uUUbSMq4TKu+6QzovkBuq6G7zi8C0DD4793E8C47EC9BD8208394B997B85D5294127E325F919989B9E3B6C5566B7}$tP#6Bdh0rDH9.BVh]f;*[9DIP!3f!'#(oWj`ymSr23=%jR~wg6D^>oT-(}TjL6QgByApq4ZqPkhQBn7dRpK2N7euhPmMhNEaB5KFWav7dJU9zP8hdEz6jIngVqfT6eLG7GjEI9m4PXse/gCdBMX8z7XAU/kV9Ysi2uUUbSMq4TKu+6QzovkBuq6G7zi8C0DD4793E8C47EC9BD8208394B997B85D5294127E325F919989B9E3B6C5566B7}$tP#6Bdh0rDH9.BVh]f;*[9DIP!3f!'#(oWj`ymSr23=%jR~wg6D^>oT-(}TjL6QgByApq4ZqPkhQBn7dRpK2N7euhPmMhNEaB5KFWav7dJU9zP8hdEz6jIngVqfT"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(config)
                .Build();

            // NOT mocking AutoMapper
            // https://stackoverflow.com/a/58892231/4144622
            var profile = new UserLoginProfile();
            var profileConfiguration = new MapperConfiguration(cfg => cfg.AddProfile(profile));
            _mapper = new Mapper(profileConfiguration);

            _validRegisteredUser = new UserLoginDto()
            {
                Email = "test1@test.com",
                Password = "TokenHamzx81@"
            };

            _invalidEmailFormat = new UserLoginDto()
            {
                Email = "test1",
                Password = "TokenHamzx81@"
            };

            _invalidPasswordStrength = new UserLoginDto()
            {
                Email = "test1",
                Password = "1234"
            };

            _unregisteredUser = new UserLoginDto()
            {
                Email = "test1",
                Password = "1234"
            };

            _wrongPassword = new UserLoginDto()
            {
                Email = "test1@test.com",
                Password = "77kenHamzx81@"
            };

            _fakeToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxMCIsInJvbGUiOiJBZG1pbiIsIm5iZiI6MTY4ODM3MDAyMiwiZXhwIjoxNjg4MzczNjIyLCJpYXQiOjE2ODgzNzAwMjJ9.3r0pRfsWKLoCwB7_9XKshJBxEOOP-yeaeJDiyMpIvm5bvLbzn9KTwJ7Ks-dV13lzGuCqKo_HwK2M1UJ-Cx5XXX";

            var appSettingsToken = _configuration.GetSection("AppSettings:Token").Value;

            _tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s: appSettingsToken)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(1) // expiry time variability issue
            };
        }


        public void Dispose()
        {
            // something
        }


        [Fact]
        public async Task Register_ShouldPass()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EF_DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            var context = new EF_DataContext(options);
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            // Act
            var authRegister = await authRepository.Register(_validRegisteredUser);
            await context.Database.EnsureDeletedAsync();

            // Assert
            Assert.True(authRegister.Success);
        }


        [Fact]
        public async Task Register_Null_User_ShouldFail()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EF_DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            var context = new EF_DataContext(options);
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            // Act
            Func<Task> authRegister = () => authRepository.Register(null);
            await context.Database.EnsureDeletedAsync();

            // Assert
            var exception = await Assert.ThrowsAsync<System.ArgumentException>(authRegister);
        }


        [Fact]
        public async Task Register_Invalid_Email_Format_ShouldFail()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EF_DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            var context = new EF_DataContext(options);
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            // Act
            var authRegister = await authRepository.Register(_invalidEmailFormat);
            await context.Database.EnsureDeletedAsync();

            // Assert
            Assert.False(authRegister.Success);
        }

        [Fact]
        public async Task Register_Invalid_Password_Strength_Should_Fail()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EF_DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            var context = new EF_DataContext(options);
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            // Act
            var authRegister = await authRepository.Register(_invalidPasswordStrength);
            await context.Database.EnsureDeletedAsync();

            // Assert
            Assert.False(authRegister.Success);
        }


        [Fact]
        public async Task Register_Existing_User_Should_Fail()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<EF_DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;

            var context = new EF_DataContext(options);
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            // Act
            var authRegister = await authRepository.Register(_validRegisteredUser);
            var authRegisterSameUser = await authRepository.Register(_validRegisteredUser);
            await context.Database.EnsureDeletedAsync();

            // Assert
            Assert.True(authRegister.Success);
            Assert.False(authRegisterSameUser.Success);
        }


        [Fact]
        public async Task Auth_Login_ShouldPass()
        {
            // Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var refreshTokensBeforeList = await context.RefreshTokens.ToListAsync();
            var refreshTokensBeforeCount = refreshTokensBeforeList.Count;
            var authLogin = await authRepository.Login(_validRegisteredUser);
            var userFromToken = authRepository.GetUserIdFromToken(authLogin.Token);
            var refreshTokensAfterList = await context.RefreshTokens.ToListAsync();
            var refreshTokensAfterCount = refreshTokensAfterList.Count;
            var refreshTokenFromDB = await context.RefreshTokens.FirstOrDefaultAsync();
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.True(authLogin.Success);
            Assert.NotNull(authLogin.User);
            Assert.Equal(1, authLogin.User.Id);
            Assert.Equal("test1@test.com", authLogin.User.Email);
            Assert.Equal("1", userFromToken);
            Assert.Equal(0, refreshTokensBeforeCount); // that 0 Refresh tokens were initially stored in the in-memory db
            Assert.Equal(1, refreshTokensAfterCount);  // that 1 Refresh token is now stored in the in-memory db
            Assert.Equal(1, refreshTokenFromDB?.User.Id); // that the stored Refresh token matches our user Id
            Assert.Equal(authLogin.RefreshToken, refreshTokenFromDB); // moot
            Assert.NotNull(authLogin.Token);
            Assert.IsType<string>(authLogin.Token);
        }


        [Fact]
        public async Task Auth_Login_Unregistered_User_ShouldFail()
        {
            // Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authLogin = await authRepository.Login(_unregisteredUser);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.False(authLogin.Success);

        }

        [Fact]
        public async Task Auth_Login_Null_User_ShouldFail()
        {
            // Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            // Act
            Func<Task> authLogin = () => authRepository.Login(null);
            await context.Database.EnsureDeletedAsync();

            // Assert
            var exception = await Assert.ThrowsAsync<System.Exception>(authLogin);

        }


        [Fact]
        public async Task Auth_Login_Wrong_Password_ShouldFail()
        {
            // Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authLogin = await authRepository.Login(_wrongPassword);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.False(authLogin.Success);

        }


        [Fact]
        public async Task Auth_Refresh_ShouldPass()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authLogin = await authRepository.Login(_validRegisteredUser);
            var authRefresh = await authRepository.RefreshToken(authLogin.RefreshToken.Token);
            var refreshTokensFromDB = await context.RefreshTokens.ToListAsync();
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.True(authRefresh.Success);
            Assert.NotNull(authRefresh.User);
            Assert.Equal(1, authRefresh.User.Id);
            Assert.Equal("test1@test.com", authRefresh.User.Email);
            Assert.Equal(2, refreshTokensFromDB.Count); // there should be 2 refresh tokens in db
            Assert.True(refreshTokensFromDB[0].Used); // first token generated has been used
            Assert.False(refreshTokensFromDB[1].Used); // second token generated has not been used
        }


        [Fact]
        public async Task Auth_Refresh_Null_RefreshToken_ShouldFail()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authRefresh = await authRepository.RefreshToken(null);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.False(authRefresh.Success);
        }

        [Fact]
        public async Task Auth_Refresh_Invalid_RefreshToken_ShouldFail()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authRefresh = await authRepository.RefreshToken(_fakeToken);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.False(authRefresh.Success);
        }


        [Fact]
        public async Task Auth_Refresh_Stored_Refresh_Token_Null_ShouldFail()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authLogin = await authRepository.Login(_validRegisteredUser);
            var refreshTokenToDelete = context.RefreshTokens.FirstOrDefault(a => a.Id == authLogin.RefreshToken.Id);
            var deleteRefreshTokenFromInMemoryDB = context.RefreshTokens.Remove(refreshTokenToDelete);
            context.SaveChanges();

            var authRefresh = await authRepository.RefreshToken(authLogin.RefreshToken.Token);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.False(authRefresh.Success);
        }


        [Fact]
        public async Task Auth_Refresh_Stored_Refresh_Token_DateTime_Does_Not_Match_ShouldFail()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authLogin = await authRepository.Login(_validRegisteredUser);
            var refreshTokenToAlter = context.RefreshTokens.FirstOrDefault(a => a.Id == authLogin.RefreshToken.Id);
            refreshTokenToAlter.Expires = DateTime.UtcNow.AddYears(-1);
            context.SaveChanges();

            var authRefresh = await authRepository.RefreshToken(authLogin.RefreshToken.Token);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.False(authRefresh.Success);
        }



        [Fact]
        public async Task Auth_GetUserIdFromToken_ShouldPass()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            var authLogin = await authRepository.Login(_validRegisteredUser);
            string? getUserFromId = authRepository.GetUserIdFromToken(authLogin.Token);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.Equal("1", getUserFromId);

        }


        [Fact]
        public async Task Auth_GetUserIdFromToken_Fake_Token_ShouldFail()
        {
            //Arrange
            var context = await CreatePopulatedInMemoryContext();
            var authRepository = new AuthRepository(context, _mapper, _configuration, _tokenValidationParameters);

            //Act
            string? getUserFromId = authRepository.GetUserIdFromToken(_fakeToken);
            await context.Database.EnsureDeletedAsync();

            //Assert
            Assert.DoesNotMatch("1", getUserFromId);

        }


        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        // out keyword means that we do not have to return anything
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private async Task<EF_DataContext?> CreatePopulatedInMemoryContext()
        {
            CreatePasswordHash("TokenHamzx81@", out byte[] passwordHash, out byte[] passwordSalt);
            var options = new DbContextOptionsBuilder<EF_DataContext>()
                .UseInMemoryDatabase(databaseName: "TestDB")
                .Options;
            var context = new EF_DataContext(options);
            context.Users.Add(new User { Id = 1, Email = "test1@test.com", PasswordHash = passwordHash, PasswordSalt = passwordSalt });
            await context.SaveChangesAsync();
            return context;
        }

    }
}
