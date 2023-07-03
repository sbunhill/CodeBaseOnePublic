using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.EfCore
#pragma warning disable CS1591
{
    [Table("user")]
    [ExcludeFromCodeCoverage]
    public class User
    {
        [Key, Required]
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
    }
}
