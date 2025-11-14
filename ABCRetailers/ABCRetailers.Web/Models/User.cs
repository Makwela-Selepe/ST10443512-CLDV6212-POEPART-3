using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }
}