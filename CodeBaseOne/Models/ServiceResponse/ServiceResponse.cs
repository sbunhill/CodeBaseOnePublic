using CodeBaseOne.EfCore;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Models.ServiceResponse
{
    /// <summary>
    /// service layer response class
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ServiceResponse
    {
        #pragma warning disable CS1591
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; } = null;
        public string Token { get; set; } = string.Empty;
        public RefreshToken? RefreshToken { get; set; } = null;
    }
}
