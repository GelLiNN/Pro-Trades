using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using PT.Middleware;
using PT.Models.RequestModels;
using PT.Services;
using System.Net.Mime;

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
        public IActionResult RegisterAccount(RegistrationRequest req)
        {
            try
            {
                string userId = Guid.NewGuid().ToString();
                if (req.AccessCode == Program.Config.GetValue<string>("AlphaAccessCode"))
                {
                    //string date = DateTime.Now.AddDays(100).ToString("mm/dd/yyyy");
                    string date = "12/12/2024";
                    string encryptedPass = LicenseGenerator.CreateEncryptedKey(req.Username, req.Password, date);
                    User newUser = new User
                    {
                        UserId = userId,
                        UserTypeId = 0,
                        Username = req.Username,
                        Password = encryptedPass
                    };
                    _ptContext.User.Add(newUser);
                    int result = _ptContext.SaveChanges();
                }
                return new ContentResult
                {
                    Content = userId,
                    StatusCode = 200,
                };
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

        [HttpPost("api/auth/Login")]
        public IActionResult Login(LoginRequest req)
        {
            try
            {
                User? user = _ptContext.User
                    .Where(x => x.Username == req.Username)
                    .FirstOrDefault();
                if (user != null)
                {
                    string encryptedPassFromDb = user.Password;
                    string usernameInput = req.Username;
                    string passwordInput = req.Password;
                    DecryptedTokenItems decryptedItems = SecurityHelper.DecryptUserToken(encryptedPassFromDb);
                    if (decryptedItems.Username == usernameInput && decryptedItems.Password == passwordInput)
                    {
                        // Decrypted password and username match, proceed to send back session token
                        Response.Headers.Add(Constants.AUTH_HEADER, encryptedPassFromDb);
                        SessionResponse response = new SessionResponse
                        {
                            UserId = user.UserId,
                            UserTypeId = user.UserTypeId,
                            Username = user.Username,
                            Email = user.Email
                        };
                        return Ok(response);
                    }
                    else
                    {
                        // User not authenticated
                        return BadRequest("Auth Error: User not authenticated");
                    }
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

        [HttpPost("api/auth/CheckSession")]
        public IActionResult CheckSession()
        {
            try
            {
                // Obtain token header, decrypt, then query database
                Request.Headers.TryGetValue(Constants.AUTH_HEADER, out StringValues value);
                string token = value.ToString();
                DecryptedTokenItems decryptedItems = SecurityHelper.DecryptUserToken(token);
                User? user = _ptContext.User
                    .Where(x => x.Username == decryptedItems.Username)
                    .FirstOrDefault();

                if (user != null && user.Password == token)
                {
                    // Decrypted password and username match, proceed to send back session token
                    Response.Headers.Add(Constants.AUTH_HEADER, user.Password);
                    SessionResponse response = new SessionResponse
                    {
                        UserId = user.UserId,
                        UserTypeId = user.UserTypeId,
                        Username = user.Username,
                        Email = user.Email
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
