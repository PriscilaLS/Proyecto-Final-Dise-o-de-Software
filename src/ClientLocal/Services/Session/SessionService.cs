namespace ClientLocal.Services.Session
{
    public class SessionService
    {
        public string? JwtToken { get; private set; }
        public int? UserId { get; private set; }
        public string? UserName { get; private set; }
        public string? UserRole { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(JwtToken);

        public void SetSession(string token, int userId, string userName, string userRole)
        {
            JwtToken = token;
            UserId = userId;
            UserName = userName;
            UserRole = userRole;
        }

        public void Clear()
        {
            JwtToken = null;
            UserId = null;
            UserName = null;
            UserRole = null;
        }
    }
}