using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    
}
