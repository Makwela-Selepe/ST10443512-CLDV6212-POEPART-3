namespace ABCRetailers.Web.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public string CustomerName { get; set; } = string.Empty;

        public int CartItems { get; set; }
        public int TotalOrders { get; set; }

        public int PendingOrders { get; set; }
        public int ShippedOrders { get; set; }      // ✅ NEW
        public int DeliveredOrders { get; set; }
    }
}
