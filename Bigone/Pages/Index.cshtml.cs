
    using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

public class IndexModel : PageModel
{
    public bool LoginFailed { get; set; }

    public void OnGet()
    {
        // Reset LoginFailed on GET request to avoid showing previous errors
        LoginFailed = false; // Ensure this is reset on each GET request
    }

    public async Task<IActionResult> OnPostAsync(string username, string password, string logout)
    {
        // Handle logout
        if (logout == "true")
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index"); // Redirect to login page after logout
        }

        // Handle login
        if (username == "admin" && password == "password") // Change these credentials as needed
        {
            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true // Set to true if you want the user to stay logged in
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToPage("/Home"); // Redirect to home after successful login
        }

        LoginFailed = true; // Set flag for failed login attempt
        return Page(); // Return to same page with error message
    }
}
