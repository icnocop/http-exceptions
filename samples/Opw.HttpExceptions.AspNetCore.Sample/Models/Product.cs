using System.ComponentModel.DataAnnotations;

namespace Opw.HttpExceptions.AspNetCore.Sample.Models
{
    public class Product
    {
        [Required]
        public string Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }
    }
}
