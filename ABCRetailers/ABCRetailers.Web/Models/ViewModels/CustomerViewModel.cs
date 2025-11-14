using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Web.Models.ViewModels
{
    public class CustomerViewModel
    {
        public string? Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        [Display(Name = "Delivery address")]
        public string? DeliveryAddress { get; set; }
    }
}
