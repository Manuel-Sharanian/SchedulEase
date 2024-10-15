#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

using BeautySalon.Data;
using BeautySalon.Models;
using BeautySalon.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;



namespace BeautySalon.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher<IdentityUser> _passwordHasher;

        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailService emailService,
            RoleManager<IdentityRole> roleManager,
            IPasswordHasher<IdentityUser> passwordHasher)
        {
            _context = context;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
            _roleManager = roleManager;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>

        public class InputModel
        {
            [Required(ErrorMessage = "Անունը պարտադիր է")]
            [Display(Name = "Full Name")]
            [RegularExpression(@"^[a-zA-ZԱ-Ֆա-ֆ]+(([',. -][a-zA-ZԱ-Ֆա-ֆ ])?[a-zA-ZԱ-Ֆա-ֆ]*)*$", ErrorMessage = "Անուն Ազգանունը կարող է պարունակել միայն տառեր և բացատներ")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Էլ․ փոստը պարտադիր է")]
            [EmailAddress(ErrorMessage = "Անվավեր էլ․ փոստի հասցե")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Գաղտնաբառը պարտադիր է")]
            [StringLength(100, ErrorMessage = "{0}-ը պետք է լինի առնվազն {2} և առավելագույնը {1} նիշ երկարությամբ:", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Գաղտնաբառ")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Հաստատել գաղտնաբառը")]
            [Compare("Password", ErrorMessage = "Գաղտնաբառերը չեն համընկնում.")]
            public string ConfirmPassword { get; set; }
        }



        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var tempUser = new IdentityUser { UserName = Input.Email };
                var hashedPassword = _passwordHasher.HashPassword(tempUser, Input.Password);

                var pendingRegistration = new PendingRegistration
                {
                    Email = Input.Email,
                    FullName = Input.FullName,
                    PasswordHash = hashedPassword,
                    RegistrationDate = DateTime.UtcNow
                };

                _context.PendingRegistrations.Add(pendingRegistration);
                await _context.SaveChangesAsync();

                // Օգտագործում ենք առկա SendAdminNotificationAsync մեթոդը
                await _emailService.SendAdminNotificationAsync(Input.Email, Input.FullName);

                //        // Հաղորդագրություն օգտատիրոջը
                TempData["Message"] = "Ձեր գրանցման հայտը ստացվել է: Խնդրում ենք սպասել հաստատման:";
                return RedirectToPage();
            }

            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}