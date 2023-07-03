using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.EfCore
#pragma warning disable CS1591
{
    [Table("product")]
    [Index(nameof(Name), IsUnique = true)] // name must be unique
    [ExcludeFromCodeCoverage]
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand {get; set; } = string.Empty;
        public int Size { get; set; } 
        public float Price { get; set; }
        public string Note { get; set; } = string.Empty;

    }
}
