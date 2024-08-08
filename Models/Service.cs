using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace BeautySalon.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Ծառայություն")]
        public string? ServiceName { get; set; }

        [Required]
        [Display(Name = "Արժեք")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Many-to-one relationship
        public ICollection<Appointment>? Appointments { get; set; }
    }
}