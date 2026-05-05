using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DonationApp.Models;

namespace DonationApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public IActionResult Register() => View();

    [HttpGet]
    public IActionResult TestRegister()
    {
        return Content("Controller is reachable");
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        Console.WriteLine("POST HIT");
        Console.WriteLine($"ModelState valid: {ModelState.IsValid}");

        foreach (var key in ModelState.Keys)
        {
            var state = ModelState[key];
            foreach (var error in state.Errors)
                Console.WriteLine($"Field: {key} Error: {error.ErrorMessage}");
        }

        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            NamaDepan = model.NamaDepan,
            NamaBelakang = model.NamaBelakang,
            Email = model.Email,
            UserName = model.Email,
            NomorTelepon = model.NomorTelepon,
            Alamat = model.Alamat
        };

        Console.WriteLine("Creating user...");
        var result = await _userManager.CreateAsync(user, model.Password);
        Console.WriteLine($"Result: {result.Succeeded}");

        foreach (var error in result.Errors)
            Console.WriteLine($"Identity error: {error.Description}");

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);

        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError(string.Empty, "Email atau password salah.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}