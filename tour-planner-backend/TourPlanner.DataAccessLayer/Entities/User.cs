using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public string Email { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
    
}
