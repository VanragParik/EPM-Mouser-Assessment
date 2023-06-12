using EPM.Mouser.Interview.Data;
using Microsoft.AspNetCore.Mvc;

namespace EPM.Mouser.Interview.Web.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly IWarehouseRepository _repository;

        public HomeController(IWarehouseRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var products = _repository.List().Result;

            return View(products);
        }
        [HttpGet("ProductDetail/{id}")]
        public async Task<IActionResult> ProductDetail(long id)
        {
            // Retrieve the product details based on the provided id
            var product = await _repository.Get(id);

            if (product == null)
            {
                // If the product is not found, you can handle the appropriate response (e.g., return a not found view)
                return NotFound();
            }

            // Pass the product details to the view for display
            return View("ProductDetail", product);
        }
    }
}
