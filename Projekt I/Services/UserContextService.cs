using System.Security.Claims;

namespace Projekt_I.Services
{
    public class UserContextService
    {

        public bool IsUserAuthenticated { get; private set; }
        public string GivenName { get; private set; }
        public string Surname { get; private set; }
        public string Avatar { get; private set; }
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            Initialize();
        }


        public void Initialize()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Check if the user is authenticated
            IsUserAuthenticated = httpContext?.User.Identity.IsAuthenticated ?? false;

            // Retrieve user information if authenticated
            if (IsUserAuthenticated)
            {
                GivenName = httpContext.User.FindFirst(ClaimTypes.GivenName)?.Value ?? httpContext.User.Identity.Name;
                Surname = httpContext.User.FindFirst(ClaimTypes.Surname)?.Value ?? "";
                Avatar = httpContext.User.FindFirst("picture")?.Value ?? "";
            }
            else
            {
                // Set default values or clear data if not authenticated
                GivenName = "";
                Surname = "";
                Avatar = "";
            }
        }

        public ClaimsPrincipal GetUser()
        {
            return _httpContextAccessor.HttpContext.User;
        }

        public string GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId;
        }


        public string GetUserPicture()
        {
            return _httpContextAccessor.HttpContext.User.FindFirstValue("picture");
        }
    }
}
