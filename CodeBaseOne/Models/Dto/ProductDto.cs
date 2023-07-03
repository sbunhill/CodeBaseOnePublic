using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Models.Dto
{
#pragma warning disable CS1591
    [ExcludeFromCodeCoverage]
    public class ProductDto
    {
        // using basic data annotations here - only as POC
        public int? Id { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "should not exceed 50 char")]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Brand { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Value must be greater than zero.")]
        public int Size { get; set; }
        [Range(0.01, float.MaxValue, ErrorMessage = "Value must be greater than zero.")]
        public float Price { get; set; }
        [Required]
        public string Note { get; set; } = string.Empty;
    }
}
