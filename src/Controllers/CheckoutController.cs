using DotnetEcommerce.Repository;
using Microsoft.AspNetCore.Mvc;

namespace DotnetEcommerce.Controllers;

/// <summary>
/// Checkout controller - clean business logic.
/// NO observability code here.
/// HTTP requests are automatically traced by OpenTelemetry ASP.NET Core instrumentation.
/// </summary>
public class CheckoutController : Controller
{
    private readonly ProductRepository _productRepo;
    private readonly OrderRepository _orderRepo;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        ProductRepository productRepo,
        OrderRepository orderRepo,
        ILogger<CheckoutController> logger)
    {
        _productRepo = productRepo;
        _orderRepo = orderRepo;
        _logger = logger;
    }

    [HttpPost("/checkout")]
    public async Task<IActionResult> Checkout(int product_id, int quantity)
    {
        try
        {
            // CPU simulation for profiling visibility
            SimulateCpuWork();

            var product = await _productRepo.GetByIdAsync(product_id);
            if (product == null)
            {
                return NotFound();
            }

            var total = product.Price * quantity;
            var orderId = await _orderRepo.CreateAsync(product_id, quantity, total);

            _logger.LogInformation(
                "Order created: id={OrderId}, product={ProductName}, total={Total}",
                orderId, product.Name, total);

            return Redirect($"/success?order_id={orderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing checkout");
            return StatusCode(500);
        }
    }

    [HttpGet("/success")]
    public async Task<IActionResult> Success(int order_id)
    {
        try
        {
            var order = await _orderRepo.GetByIdAsync(order_id);
            if (order == null)
            {
                return NotFound();
            }

            var product = await _productRepo.GetByIdAsync(order.ProductId);
            return View("Success", new { Order = order, Product = product });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading success page");
            return StatusCode(500);
        }
    }

    private void SimulateCpuWork()
    {
        long result = 0;
        for (int i = 0; i < 2000000; i++)
        {
            result += (long)i * i * i;
        }
    }
}
