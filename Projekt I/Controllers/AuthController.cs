using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Projekt_I.Controllers
{
   [Route("/[controller]")]
   [ApiController]
    public class AuthController : ControllerBase
    {

        [HttpGet(nameof(Login))]
        public async Task<ActionResult> Login()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/",
            };

            properties.Items["prompt"] = "login";

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet(nameof(Logout))]
        public async Task<ActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the home page or another desired page after logout
            return Redirect("/");
        }
    }
}
