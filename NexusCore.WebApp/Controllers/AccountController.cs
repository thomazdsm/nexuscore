using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusCore.Application.Interfaces;
using NexusCore.Domain.Entities;
using NexusCore.Infra.Data.Context;
using NexusCore.WebApp.Models.Account;

namespace NexusCore.WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
        }

        // --- LOGIN ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity is { IsAuthenticated: true }) return RedirectToAction("Dashboard");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Action("Dashboard", "Account");
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded) return LocalRedirect(returnUrl);
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            }
            return View(model);
        }

        // --- LOGOUT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // --- REGISTER ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();

        // --- REGISTER ---
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuário {Email} criado. Enviando e-mail de confirmação.", model.Email);

                    // Cria o perfil do usuário
                    var userProfile = new UserProfile { ApplicationUserId = user.Id, FirstName = model.FirstName, LastName = model.LastName };
                    _context.UserProfiles.Add(userProfile);
                    await _context.SaveChangesAsync();

                    // Gera o token de confirmação e o link de callback
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code }, Request.Scheme);

                    // Envia o e-mail
                    await _emailSender.SendEmailAsync(model.Email, "Confirme seu E-mail - Nexus Core",
                        $"Bem-vindo ao Nexus Core! Por favor, confirme sua conta <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

                    // Redireciona para uma página de confirmação
                    return RedirectToAction("RegisterConfirmation");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterConfirmation() => View();

        // --- EMAIL CONFIRMATION ---
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Não foi possível encontrar o usuário com ID '{userId}'.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Seu e-mail foi confirmado com sucesso! Você já pode fazer login.";
            }
            else
            {
                TempData["ErrorMessage"] = "Erro ao confirmar seu e-mail.";
            }

            return RedirectToAction("Login");
        }

        // --- FORGOT PASSWORD ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                //if (user != null && await _userManager.IsEmailConfirmedAsync(user))
                if (user != null)
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.Action("ResetPassword", "Account", new { code, email = user.Email }, Request.Scheme);
                    await _emailSender.SendEmailAsync(model.Email, "Redefinição de Senha - Nexus Core", $"Por favor, redefina sua senha <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");
                }
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        // --- RESET PASSWORD ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? code = null, string? email = null)
        {
            if (code == null || email == null) return BadRequest("Um código e e-mail devem ser fornecidos.");
            var model = new ResetPasswordViewModel { Code = code, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (result.Succeeded) return RedirectToAction("ResetPasswordConfirmation");
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();

        // --- DASHBOARD ---
        [Authorize]
        public IActionResult Dashboard() => View();

        // --- PROFILE ---
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
            if (userProfile == null)
            {
                userProfile = new UserProfile { ApplicationUserId = user.Id };
                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();
            }

            var model = new ProfileViewModel { Email = user.Email, FirstName = userProfile.FirstName, LastName = userProfile.LastName };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var userProfile = await _context.UserProfiles.SingleOrDefaultAsync(p => p.ApplicationUserId == user.Id);
            if (userProfile == null) return NotFound();

            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;

            try
            {
                _context.UserProfiles.Update(userProfile);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Seu perfil foi atualizado com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar perfil do usuário {UserId}", user.Id);
                TempData["StatusMessage"] = "Erro: Não foi possível salvar seu perfil.";
            }

            return RedirectToAction("Profile");
        }
    }
}