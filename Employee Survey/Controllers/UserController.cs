using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Employee_Survey.Controllers;

public class UserController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    public IActionResult Tests()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    public IActionResult Surveys()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    public IActionResult TakeTest(string id)
    {
        ViewBag.TestId = id;
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    public IActionResult Results()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Roles = "User")]
    public IActionResult ResultDetail(string id)
    {
        ViewBag.ResultId = id;
        return View();
    }

    [HttpPost]
    public IActionResult Logout()
    {
        return SignOut("cookie");
    }
}
