// --- Models/Entrepreneur.cs ---
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    [Table("Entrepreneur")]
    public class Entrepreneur : Person
    {
        [Key]
        public int EntrepreneurId { get; set; }

        public ICollection<Restaurant> Restaurants { get; set; } = new List<Restaurant>();
    }
}