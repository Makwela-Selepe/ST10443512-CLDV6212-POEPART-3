// Models/ViewModels/EditUserViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Web.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [Required]
        public string Role { get; set; } = "Customer";

        public bool IsActive { get; set; } = true;
    }
}
