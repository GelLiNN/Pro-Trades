using Newtonsoft.Json;

namespace PT.Models.RequestModels
{
    /*
     * Authorization Models
     */
    public class RegistrationRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("accessCode")]
        public string AccessCode { get; set; }
    }

    public class LoginRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    public class SessionResponse
    {
        [JsonProperty("authToken")]
        public string AuthToken { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("userTypeId")]
        public int UserTypeId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("isLoggedIn")]
        public bool IsLoggedIn { get; set; }
    }

    public class DecryptedTokenItems
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime ExpireDate { get; set; }
        public bool DateValid { get; set; }
    }
}
