using Microsoft.AspNetCore.Mvc;

namespace AWS.Cognito.Authorization.Authentication.MVP.Controllers
{

    public class AccountController : Controller
    {
        public IActionResult Register()
        {
            return View();
        }
          
    }
}