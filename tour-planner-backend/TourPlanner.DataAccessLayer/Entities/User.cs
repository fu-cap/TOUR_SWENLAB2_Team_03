using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.DataAccessLayer.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public required string username { get; set; }
        public required string password_hash { get; set; }
        public string email { get; set; } = "";
        public DateTime created_at { get; set; }
    }
    
}
