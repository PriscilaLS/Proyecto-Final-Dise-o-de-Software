namespace ClientLocal.Services.Session
{
    public class SessionService
    {
        public string? JwtToken { get; private set; }
        public string? UserRole { get; private set; }
        public string? UserEmail { get; private set; }

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(JwtToken);

        public void SetSession(string token, string? role, string? email)
        {
            JwtToken = token;
            UserRole = role;
            UserEmail = email;
        }

        public void Clear()
        {
            JwtToken = null;
            UserRole = null;
            UserEmail = null;
        }
    }
}