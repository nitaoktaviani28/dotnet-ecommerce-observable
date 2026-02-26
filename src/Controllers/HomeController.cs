using DotnetEcommerce.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEcommerce.Controllers;

/// <summary>
/// Home controller - clean business logic.
/// NO observability code here.
/// HTTP requests are automatically traced by OpenTelemetry ASP.NET Core instrumentation.
/// </summary>
public class HomeController : Controller
{
    private readonly ProductRepository _productRepo;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ProductRepository productRepo, ILogger<HomeController> logger)
    {
        _productRepo = productRepo;
        _logger = logger;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var products = await _productRepo.GetAllAsync();
            return View("Index", products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading products");
            return StatusCode(500);
        }
    }
}
