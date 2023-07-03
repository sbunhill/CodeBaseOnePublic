using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.EfCore
#pragma warning disable CS1591
{
    [Table("refresh_token")]
    [ExcludeFromCodeCoverage]
    public class RefreshToken
    {
        [Key, Required]
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
        public bool Invalidated { get; set; } = false;
        public bool Used { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
