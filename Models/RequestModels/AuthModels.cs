namespace PT.Models.RequestModels
{
    /*
     * Authorization Models
     */
    public class RegistrationRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string AccessCode { get; set; }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SessionResponse
    {
        public string UserId { get; set; }
        public int UserTypeId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class DecryptedTokenItems
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime ExpireDate { get; set; }
        public bool DateValid { get; set; }
    }
}
