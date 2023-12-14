﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PT.Middleware;
using PT.Models.RequestModels;

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

                // TODO: add more comprehensive validation results
                bool isValid =
                    !String.IsNullOrEmpty(req.AccessCode)
                    && !String.IsNullOrEmpty(req.Username)
                    && !String.IsNullOrEmpty(req.Email)
                    && !String.IsNullOrEmpty(req.Password)
                    && req.AccessCode == Program.Config.GetValue<string>("AlphaAccessCode");

                if (isValid)
                {
                    //string date = DateTime.Now.AddDays(100).ToString("mm/dd/yyyy");
                    string date = "12/12/2024";
                    string encryptedPass =
                        LicenseGenerator.CreateEncryptedKey(req.Username, req.Password, date);

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

                    return Ok(userId);
                }
                else
                {
                    return BadRequest("Auth Error: Failed to provide valid inputs");
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

        [HttpPost("api/auth/Login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            try
            {
                User? user = _ptContext.User
                    .Where(x => x.Username == req.Username && !x.IsLoggedIn)
                    .FirstOrDefault();

                if (user != null)
                {
                    string encryptedPassFromDb = user.Password;
                    string usernameInput = req.Username;
                    string passwordInput = req.Password;

                    DecryptedTokenItems decryptedItems =
                        SecurityHelper.DecryptUserToken(encryptedPassFromDb);

                    if (decryptedItems.Username == usernameInput
                        && decryptedItems.Password == passwordInput)
                    {
                        // Decrypted password and username match, so mark User as logged in
                        user.IsLoggedIn = true;
                        _ptContext.Update(user);
                        int result = _ptContext.SaveChanges();

                        // proceed to send back session token
                        // Response.Headers.Add(Constants.AUTH_HEADER, encryptedPassFromDb);
                        SessionResponse response = new SessionResponse
                        {
                            AuthToken = encryptedPassFromDb,
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

        [HttpPost("api/auth/Logout")]
        public IActionResult Logout()
        {
            try
            {
                // Obtain token header, decrypt, then query database
                Request.Headers.TryGetValue(Constants.AUTH_HEADER, out StringValues value);
                string token = value.ToString();
                DecryptedTokenItems decryptedItems = SecurityHelper.DecryptUserToken(token);
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

        [HttpPost("api/auth/CheckSession")]
        public IActionResult CheckSession()
        {
            try
            {
                // Obtain token header, decrypt, then query database
                Request.Headers.TryGetValue(Constants.AUTH_HEADER, out StringValues value);
                string token = value.ToString();
                DecryptedTokenItems decryptedItems = SecurityHelper.DecryptUserToken(token);
                User? user = _ptContext
                    .User
                    .Where(x => x.Username == decryptedItems.Username && x.IsLoggedIn)
                    .FirstOrDefault();

                if (user != null && user.Password == token)
                {
                    // Decrypted password and username match, proceed to send back session token
                    // Response.Headers.Add(Constants.AUTH_HEADER, user.Password);
                    SessionResponse response = new SessionResponse
                    {
                        AuthToken = user.Password,
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