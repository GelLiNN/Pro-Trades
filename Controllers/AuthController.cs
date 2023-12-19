using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PT.Middleware;
using PT.Models.RequestModels;
using SendGrid;

namespace PT.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private PTContext _ptContext;

        public AuthController(ILogger<HomeController> logger, PTContext ptContext)
        {
            _logger = logger;
            _ptContext = ptContext;
        }

        [HttpPost("api/auth/Register")]
        public IActionResult RegisterAccount([FromBody] RegistrationRequest req)
        {
            try
            {
                string userId = Guid.NewGuid().ToString();

                // First check for existing user
                var existingUser = _ptContext.User
                    .Where(x => x.Email == req.Email || x.Username == req.Username)
                    .FirstOrDefault();
                if (existingUser != null)
                {
                    return BadRequest("Auth Error: User already exists with passed username and/or email");
                }

                // TODO: add more comprehensive validation results
                bool isValid =
                    !String.IsNullOrEmpty(req.AccessCode)
                    && !String.IsNullOrEmpty(req.Username)
                    && !String.IsNullOrEmpty(req.Email)
                    && !String.IsNullOrEmpty(req.Password)
                    && req.AccessCode == Program.Config.GetValue<string>("AlphaAccessCode");

                if (isValid)
                {
                    // Create new user and add to the DB
                    //string date = DateTime.Now.AddDays(100).ToString("mm/dd/yyyy");
                    string date = Constants.PASSWORD_EXP_DATE;
                    string encryptedPass =
                        EncryptionHelper.CreateEncryptedKey(req.Username, req.Password, date, true);

                    User newUser = new User
                    {
                        UserId = userId,
                        UserTypeId = 0,
                        Username = req.Username,
                        Password = encryptedPass,
                        Email = req.Email,
                        IsLoggedIn = false
                    };
                    _ptContext.User.Add(newUser);
                    int result = _ptContext.SaveChanges();

                    // Login and return a new SessionResponse
                    LoginRequest loginRequest = new LoginRequest
                    {
                        Username = req.Username,
                        Password = req.Password,
                    };
                    SessionResponse response = LoginHelper(loginRequest, newUser);
                    if (!String.IsNullOrEmpty(response.AuthToken))
                    {
                        return Ok(response);
                    }
                    else
                    {
                        // User not authenticated
                        return BadRequest("Auth Register Error: Could not login after registering");
                    }
                }
                else
                {
                    return BadRequest("Auth Register Error: Failed to provide valid inputs");
                }
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    Content = "Auth RegisterException: " + ex.Message,
                    StatusCode = 500,
                };
            }
        }

        [HttpPost("api/auth/Login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            try
            {
                User? user = _ptContext.User
                    .Where(x => x.Username == req.Username)
                    .FirstOrDefault();

                if (user != null && !String.IsNullOrEmpty(req.Password))
                {
                    SessionResponse loginResponse = LoginHelper(req, user);
                    if (!String.IsNullOrEmpty(loginResponse.AuthToken))
                    {
                        return Ok(loginResponse);
                    }
                    else
                    {
                        // User not authenticated
                        return BadRequest("Auth Login Error: User credentials not authenticated");
                    }
                }
                else
                {
                    // User not found in db
                    return BadRequest("Auth Login Error: User not found with passed username");
                }
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    Content = "Auth Login Exception: " + ex.Message,
                    StatusCode = 500,
                };
            }
        }

        private SessionResponse LoginHelper(LoginRequest req, User user)
        {
            SessionResponse response = new SessionResponse();
            string encryptedPassFromDb = user.Password;
            string usernameInput = req.Username;
            string passwordInput = req.Password;

            DecryptedTokenItems decryptedItems =
                DecryptionHelper.DecryptUserToken(encryptedPassFromDb, true);

            if (decryptedItems.Username == usernameInput
                && decryptedItems.Password == passwordInput)
            {
                // Decrypted password and username match, so mark User as logged in
                user.IsLoggedIn = true;
                _ptContext.Update(user);
                int result = _ptContext.SaveChanges();

                // proceed to send back session token
                // Response.Headers.Add(Constants.AUTH_HEADER, encryptedPassFromDb);
                string date = Constants.SESSION_EXP_DATE;
                string sessionToken =
                    EncryptionHelper.CreateEncryptedKey(usernameInput, passwordInput, date, false);

                response = new SessionResponse
                {
                    AuthToken = sessionToken,
                    UserId = user.UserId,
                    UserTypeId = user.UserTypeId,
                    Username = user.Username,
                    Email = user.Email,
                    IsLoggedIn = user.IsLoggedIn
                };
            }
            return response;
        }

        [HttpPost("api/auth/Logout")]
        public IActionResult Logout()
        {
            try
            {
                // Obtain token header, decrypt, then query database
                Request.Headers.TryGetValue(Constants.AUTH_HEADER, out StringValues value);
                string token = value.ToString();
                DecryptedTokenItems decryptedItems = DecryptionHelper.DecryptUserToken(token, false);
                User? user = _ptContext
                    .User
                    .Where(x => x.Username == decryptedItems.Username && x.IsLoggedIn)
                    .FirstOrDefault();

                if (user != null && decryptedItems.Username == user.Username)
                {
                    // Decrypted password and username match, so mark User as logged in
                    user.IsLoggedIn = false;
                    _ptContext.Update(user);
                    int result = _ptContext.SaveChanges();

                    // proceed to send back session response without token
                    SessionResponse response = new SessionResponse
                    {
                        AuthToken = string.Empty,
                        UserId = user.UserId,
                        UserTypeId = user.UserTypeId,
                        Username = user.Username,
                        Email = user.Email,
                        IsLoggedIn = false
                    };
                    return Ok(response);
                }
                else
                {
                    // User not found in db
                    return BadRequest("Auth Error: User not found");
                }
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    Content = "Auth Exception: " + ex.Message,
                    StatusCode = 500,
                };
            }
        }

        [HttpPost("api/auth/RecoverPassword")]
        public IActionResult RecoverPassword()
        {
            return Ok();
        }

        [HttpPost("api/auth/ResetPassword")]
        public IActionResult ResetPassword()
        {
            return Ok();
        }

        [HttpGet("api/auth/CheckSession")]
        public IActionResult CheckSession()
        {
            try
            {
                // Obtain token header, decrypt, then query database
                Request.Headers.TryGetValue(Constants.AUTH_HEADER, out StringValues value);
                string token = value.ToString();
                DecryptedTokenItems decryptedItems = DecryptionHelper.DecryptUserToken(token, false);
                User? user = _ptContext
                    .User
                    .Where(x => x.Username == decryptedItems.Username && x.IsLoggedIn)
                    .FirstOrDefault();

                if (user != null)
                {
                    // Decrypted password and username match, proceed to send back session token
                    // Response.Headers.Add(Constants.AUTH_HEADER, user.Password);
                    SessionResponse response = new SessionResponse
                    {
                        AuthToken = token,
                        UserId = user.UserId,
                        UserTypeId = user.UserTypeId,
                        Username = user.Username,
                        Email = user.Email,
                        IsLoggedIn = user.IsLoggedIn
                    };
                    return Ok(response);
                }
                else
                {
                    // User not authenticated
                    return BadRequest("Auth Error: unable to authenticate session token");
                }
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    Content = "Auth Exception: " + ex.Message,
                    StatusCode = 500,
                };
            }
        }
    }
}
