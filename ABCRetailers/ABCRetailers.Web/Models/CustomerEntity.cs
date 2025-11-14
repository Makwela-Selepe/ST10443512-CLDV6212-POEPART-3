using System;

namespace ABCRetailers.Web.Models
{
    public class CustomerEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DeliveryAddress { get; set; }
    }
}

