using Authaz.Argon2;
using System.Text;

namespace TourPlanner.BusinessLayer.Utils
{
    public static class HashUtil
    {
        public static string HashPassword(string password)
        {
            string hashed = Argon2Phc.HashToString(Encoding.UTF8.GetBytes(password));
            return hashed;
        }

        public static bool CheckPassword(string hashed_password, string password)
        {
            if (string.IsNullOrEmpty(hashed_password) || !hashed_password.StartsWith("$"))
            {
                return false;
            }

            try
            {
                return Argon2Phc.Verify(Encoding.UTF8.GetBytes(password), hashed_password);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}