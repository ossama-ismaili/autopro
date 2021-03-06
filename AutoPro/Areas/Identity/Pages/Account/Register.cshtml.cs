using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using AutoPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using AutoPro.Helpers;
using System.Linq;

namespace AutoPro.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AutoProContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            AutoProContext context)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]

            public string Nom { get; set; }
            [Required]
            public string Prenom { get; set; }
            [Required]
            [Phone]
            public string Tele { get; set; }
            public string Ville { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
            public string agencename { get; set; }
            public bool IsAdmin { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (ModelState.IsValid)
            {
                // creating an owner 

                var proprietaire = new Proprietaire() { IsAgence = false, };
                if (Input.agencename != null)
                {
                    proprietaire.IsAgence = true;
                    proprietaire.NomAgence = Input.agencename;
                }
                var locataire = new Locataire() { };
                _context.locataires.Add(locataire);
                _context.proprietaires.Add(proprietaire);
                _context.SaveChanges();
                var proprietaireID = _context.proprietaires.Max(p => p.Id);
                var LocatairID = _context.locataires.Max(l => l.Id);

                var user = new ApplicationUser { UserName = Input.Nom, Email = Input.Email, PhoneNumber = Input.Tele, Nom = Input.Nom,
                    Prenom = Input.Prenom, Ville = Input.Ville,ProprietaireId=proprietaireID ,LocataireId=LocatairID };
               

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    //Create roles if not exists
                    if (!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    }
                    if (!await _roleManager.RoleExistsAsync("Executive"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Executive"));
                    }
                    //Assign user to a role as per the chekbox selection
                    if (Input.IsAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "Executive");
                    }
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { userId = user.Id, code = code },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                    if (User.IsInRole(Roles.Admin))
                    {
                        return RedirectToAction("Index");
                    }
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);

                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
