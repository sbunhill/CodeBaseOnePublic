using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Models.Dto
#pragma warning disable CS1591
{
    [ExcludeFromCodeCoverage]
    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
