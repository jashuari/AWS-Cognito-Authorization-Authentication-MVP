using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using AWS.Cognito.Authorization.Authentication.MVP.Models;

namespace AWS.Cognito.Authorization.Authentication.MVP.Controllers
{
    [AllowAnonymous]
    public class AwsController : Controller
    { 
        private readonly AmazonCognitoIdentityProviderClient _client;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;
        private readonly string _clientId;
        private readonly string _poolId;
        private readonly string _awsSecretKey;
        private readonly string _awsAccessKey;
        private readonly CognitoUserPool _userPool;

        public AwsController(IConfiguration configuration, SignInManager<User> signInManager)
        {
            _configuration = configuration;
            _awsSecretKey = _configuration["AWS:AwsSecretKey"];
            _awsAccessKey = _configuration["AWS:AwsAccessKey"];
            _clientId = _configuration["AWS:ClientId"];
            _poolId = _configuration["AWS:UserPoolId"];
            _client = new AmazonCognitoIdentityProviderClient(_awsAccessKey, _awsSecretKey, RegionEndpoint.EUCentral1);
            _userPool = new CognitoUserPool(poolID: _poolId, clientID: _clientId, provider: _client);
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginAwsModel model)
        {
            try
            {
                var user = new CognitoUser(userID: model.Email, clientID: _clientId, pool: _userPool, provider: _client, username: model.Email);

                AuthFlowResponse contextResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest()
                {
                    Password = model.Password
                }).ConfigureAwait(false);

                var userResponse = await _client.AdminGetUserAsync(new AdminGetUserRequest()
                {
                    Username = user.UserID,
                    UserPoolId = _userPool.PoolID
                }).ConfigureAwait(false);
                 
                Response.Cookies.Append("X-Access-Token", contextResponse.AuthenticationResult.IdToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });


                return LocalRedirect("/home/admin");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
         
        public IActionResult SignOut()
        {
            var callbackUrl = Url.Page("/", pageHandler: null, values: null, protocol: Request.Scheme);
            _signInManager.SignOutAsync();
            // Clear the existing external cookie to ensure a clean login process
            HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme
            );
        }
    }
}
