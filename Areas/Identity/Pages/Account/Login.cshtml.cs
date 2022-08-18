using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Cognito.Authorization.Authentication.MVP.Data;
using AWS.Cognito.Authorization.Authentication.MVP.Models;


namespace AWS.Cognito.Authorization.Authentication.MVP.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly AmazonCognitoIdentityProviderClient _client;
        private readonly IConfiguration _configuration;
        private RoleManager<IdentityRole> _roleManager;

        private readonly string _clientId;
        private readonly string _poolId;
        private readonly string _awsSecretKey;
        private readonly string _awsAccessKey;
        private readonly CognitoUserPool _userPool;
        private readonly ApplicationDbContext _context;

        public LoginModel(SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleMgr,
            ILogger<LoginModel> logger,
            UserManager<User> userManager, IConfiguration configuration, ApplicationDbContext context)
        {
            _roleManager = roleMgr;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
            _awsSecretKey = _configuration["AWS:AwsSecretKey"];
            _awsAccessKey = _configuration["AWS:AwsAccessKey"];
            _clientId = _configuration["AWS:ClientId"];
            _poolId = _configuration["AWS:UserPoolId"];
            _client = new AmazonCognitoIdentityProviderClient(_awsAccessKey, _awsSecretKey, RegionEndpoint.EUCentral1);
            _userPool = new CognitoUserPool(poolID: _poolId, clientID: _clientId, provider: _client);

        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public class CognitoRole
        {
            public CognitoRole(string name, string description, int precedence, string roleArn = null)
            {
                Name = name;
                Description = description;
                Precedence = precedence;
                RoleArn = roleArn;
            }

            public string Name { get; }

            public string Description { get; set; }

            public int Precedence { get; set; }

            public string RoleArn { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {

            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                CancellationToken cancellationToken;
                cancellationToken.ThrowIfCancellationRequested();

                // GET USER 
                var user = _userManager.FindByEmailAsync(Input.Email).Result;

                // GET ROLES FOR USER FROM DB
                var roles = await _userManager.GetRolesAsync(user);

                if (user != null && user.IsActive == true)
                {
                    if (result.Succeeded)
                    {
                        // GET AWS USER
                        AdminGetUserResponse getUser = new AdminGetUserResponse();
                        try
                        {
                            getUser = await _client.AdminGetUserAsync(new AdminGetUserRequest()
                            {
                                Username = user.UserName,
                                UserPoolId = _userPool.PoolID
                            }).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            getUser.Enabled = false;
                        }

                        if (!getUser.Enabled)
                        {
                            // AWS SIGNUP
                            var signUpRequest = new SignUpRequest()
                            {
                                ClientId = _clientId,
                                Password = Input.Password,
                                Username = user.Email,
                            };

                            var attributes = new List<AttributeType>()
                            {
                                new AttributeType(){Name="email",Value=user.Email},
                                new AttributeType(){Name="name",Value=user.FirstName},
                                new AttributeType(){Name="family_name",Value=user.LastName},
                            };


                            foreach (var attribute in attributes)
                            {
                                signUpRequest.UserAttributes.Add(attribute);
                            }

                            await _client.SignUpAsync(signUpRequest);

                            // AWS CONFIRM ACCOUNT
                            var adminConfirmSignUp = new AdminConfirmSignUpRequest()
                            {
                                UserPoolId = _userPool.PoolID,
                                Username = user.Email
                            };

                            await _client.AdminConfirmSignUpAsync(adminConfirmSignUp);

                        }
                          
                        int i = 0;

                        // GET AWS GROUPS - CREATE AWS GROUPS - ASSIGN USER TO THE GROUP
                        foreach (var role in roles)
                        {
                            
                            GetGroupResponse getGroup = new GetGroupResponse();
                            try
                            {
                                // CHECK IF ROLES EXIST IN AWS GROUPS
                                getGroup = await _client.GetGroupAsync(new GetGroupRequest()
                                {
                                    GroupName = role,
                                    UserPoolId = _userPool.PoolID
                                }, cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception)
                            {
                                getGroup = null;
                            }

                            // CREATE GROUP
                            if (getGroup == null)
                            {
                                await _client.CreateGroupAsync(new CreateGroupRequest()
                                {
                                    GroupName = role,
                                    UserPoolId = _userPool.PoolID
                                }).ConfigureAwait(false);

                                // ADD USER TO THE GROUP
                                await _client.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest()
                                {
                                    GroupName = role,
                                    Username = user.Email,
                                    UserPoolId = _userPool.PoolID
                                }).ConfigureAwait(false);
                            }

                            // CHECK IS USER ASSIGNED TO THE GROUP
                            AdminListGroupsForUserResponse adminListGroupsForUser = await _client.AdminListGroupsForUserAsync(new AdminListGroupsForUserRequest()
                            {
                                Username = user.UserName,
                                UserPoolId = _userPool.PoolID

                            }).ConfigureAwait(false);

                            foreach (var group in adminListGroupsForUser.Groups)
                            {
                                if (group.GroupName != role)
                                {
                                    // ADD USER TO THE GROUP
                                    await _client.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest()
                                    {
                                        GroupName = role,
                                        Username = user.Email,
                                        UserPoolId = _userPool.PoolID
                                    }).ConfigureAwait(false);
                                }
                            }

                            if (adminListGroupsForUser.Groups.Count == 0 ) {
                                
                                // ADD USER TO THE GROUP
                                await _client.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest()
                                {
                                    GroupName = role,
                                    Username = user.Email,
                                    UserPoolId = _userPool.PoolID
                                }).ConfigureAwait(false);
                            }
                            
                        }
                        _logger.LogInformation("User logged in.");
                        return LocalRedirect(returnUrl);
                    }
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("./Lockout");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }

            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
