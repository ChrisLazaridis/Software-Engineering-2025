// --- Models/Admin.cs ---
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SoftEng2025.Models.Common;

namespace SoftEng2025.Models
{
    [Table("Admin")]
    public class Admin : Person
    {
        [Key]
        public int AdminId { get; set; }
    }
}