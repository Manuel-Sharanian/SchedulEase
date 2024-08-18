#nullable disable
using System;
using System.Text;
using System.Threading.Tasks;
using BeautySalon.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace BeautySalon.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ConfirmEmailModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Օգտատերը ID-ով '{userId}' չի գտնվել:");
            }
            // Pending-ը հլը գնում ա քանի չենք հաստատել
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                var pendingRegistration = await _context.PendingRegistrations
                    .FirstOrDefaultAsync(pr => pr.Email == user.Email);
                if (pendingRegistration != null)
                {
                    _context.PendingRegistrations.Remove(pendingRegistration);
                    await _context.SaveChangesAsync();
                }

                StatusMessage = "Շնորհակալություն էլ. փոստը հաստատելու համար: Այժմ դուք կարող եք մուտք գործել ձեր հաշիվ:";
            }
            else
            {
                StatusMessage = "Սխալ էլ. փոստը հաստատելիս: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return Page();
        }
    }
}
