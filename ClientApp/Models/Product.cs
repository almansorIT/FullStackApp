namespace ClientApp.Models
{
    // Lightweight model matching the server API response.
    // Keep as a simple POCO for serialization/deserialization.
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Stock { get; set; }
    }
}
