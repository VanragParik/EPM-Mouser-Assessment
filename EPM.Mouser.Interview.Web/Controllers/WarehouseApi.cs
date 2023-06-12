using EPM.Mouser.Interview.Data;
using EPM.Mouser.Interview.Models;
using Microsoft.AspNetCore.Mvc;

namespace EPM.Mouser.Interview.Web.Controllers
{
    [ApiController]
    [Route("api/warehouse")]
    public class WarehouseApi : Controller
    {
        private readonly IWarehouseRepository _repository;

        public WarehouseApi(IWarehouseRepository repository)
        {
            _repository = repository;
        }
        /*
         *  Action: GET
         *  Url: api/warehouse/id
         *  This action should return a single product for an Id
         */
        [HttpGet]
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(long id)
        {
            var product = await _repository.Get(id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        /*
         *  Action: GET
         *  Url: api/warehouse
         *  This action should return a collection of products in stock
         *  In stock means In Stock Quantity is greater than zero and In Stock Quantity is greater than the Reserved Quantity
         */
        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetPublicInStockProducts()
        {
            var products = await _repository.Query(p => p.InStockQuantity > 0 && p.InStockQuantity > p.ReservedQuantity);
            return Ok(products);
        }


        /*
         *  Action: GET
         *  Url: api/warehouse/order
         *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
         *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
         *       {
         *           "id": 1,
         *           "quantity": 1
         *       }
         *
         *  This action should increase the Reserved Quantity for the product requested by the amount requested
         *
         *  This action should return failure (success = false) when:
         *     - ErrorReason.NotEnoughQuantity when: The quantity being requested would increase the Reserved Quantity to be greater than the In Stock Quantity.
         *     - ErrorReason.QuantityInvalid when: A negative number was requested
         *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpPost("order")]
        public async Task<ActionResult<UpdateResponse>> OrderItem([FromBody] UpdateQuantityRequest request)
        {
            var product = await _repository.Get(request.Id);

            if (product == null)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.InvalidRequest, Success = false });
            }

            if (request.Quantity < 0)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.QuantityInvalid, Success = false });
            }

            if (product.ReservedQuantity + request.Quantity > product.InStockQuantity)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.NotEnoughQuantity, Success = false });
            }

            product.ReservedQuantity += request.Quantity;

            await _repository.UpdateQuantities(product);

            return Ok(new UpdateResponse { Success = true });
        }

        /*
         *  Url: api/warehouse/ship
         *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
         *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
         *       {
         *           "id": 1,
         *           "quantity": 1
         *       }
         *
         *
         *  This action should:
         *     - decrease the Reserved Quantity for the product requested by the amount requested to a minimum of zero.
         *     - decrease the In Stock Quantity for the product requested by the amount requested
         *
         *  This action should return failure (success = false) when:
         *     - ErrorReason.NotEnoughQuantity when: The quantity being requested would cause the In Stock Quantity to go below zero.
         *     - ErrorReason.QuantityInvalid when: A negative number was requested
         *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpPost("ship")]
        public async Task<ActionResult<UpdateResponse>> ShipItem([FromBody] UpdateQuantityRequest request)
        {
            var product = await _repository.Get(request.Id);

            if (product == null)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.InvalidRequest, Success = false });
            }

            if (request.Quantity < 0)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.QuantityInvalid, Success = false });
            }

            if (request.Quantity > product.ReservedQuantity)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.NotEnoughQuantity, Success = false });
            }

            product.ReservedQuantity -= request.Quantity;
            product.InStockQuantity -= request.Quantity;

            if (product.InStockQuantity < 0)
            {
                product.InStockQuantity = 0;
            }

            await _repository.UpdateQuantities(product);

            return Ok(new UpdateResponse { Success = true });
        }

        /*
        *  Url: api/warehouse/restock
        *  This action should return a EPM.Mouser.Interview.Models.UpdateResponse
        *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.UpdateQuantityRequest in JSON format in the body of the request
        *       {
        *           "id": 1,
        *           "quantity": 1
        *       }
        *
        *
        *  This action should:
        *     - increase the In Stock Quantity for the product requested by the amount requested
        *
        *  This action should return failure (success = false) when:
        *     - ErrorReason.QuantityInvalid when: A negative number was requested
        *     - ErrorReason.InvalidRequest when: A product for the id does not exist
        */
        [HttpPost("restock")]
        public async Task<ActionResult<UpdateResponse>> RestockItem([FromBody] UpdateQuantityRequest request)
        {
            var product = await _repository.Get(request.Id);

            if (product == null)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.InvalidRequest, Success = false });
            }

            if (request.Quantity < 0)
            {
                return BadRequest(new UpdateResponse { ErrorReason = ErrorReason.QuantityInvalid, Success = false });
            }

            product.InStockQuantity += request.Quantity;

            await _repository.UpdateQuantities(product);

            return Ok(new UpdateResponse { Success = true });
        }

        /*
        *  Url: api/warehouse/add
        *  This action should return a EPM.Mouser.Interview.Models.CreateResponse<EPM.Mouser.Interview.Models.Product>
        *  This action should have handle an input parameter of EPM.Mouser.Interview.Models.Product in JSON format in the body of the request
        *       {
        *           "id": 1,
        *           "inStockQuantity": 1,
        *           "reservedQuantity": 1,
        *           "name": "product name"
        *       }
        *
        *
        *  This action should:
        *     - create a new product with:
        *          - The requested name - But forced to be unique - see below
        *          - The requested In Stock Quantity
        *          - The Reserved Quantity should be zero
        *
        *       UNIQUE Name requirements
        *          - No two products can have the same name
        *          - Names should have no leading or trailing whitespace before checking for uniqueness
        *          - If a new name is not unique then append "(x)" to the name [like windows file system does, where x is the next avaiable number]
        *
        *
        *  This action should return failure (success = false) and an empty Model property when:
        *     - ErrorReason.QuantityInvalid when: A negative number was requested for the In Stock Quantity
        *     - ErrorReason.InvalidRequest when: A blank or empty name is requested
        */
        [HttpPost("add")]
        public async Task<ActionResult<CreateResponse<Product>>> AddNewProduct([FromBody] Product newProduct)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(newProduct.Name))
            {
                return BadRequest(new CreateResponse<Product> { ErrorReason = ErrorReason.InvalidRequest, Success = false, Model = null });
            }

            if (newProduct.InStockQuantity < 0)
            {
                return BadRequest(new CreateResponse<Product> { ErrorReason = ErrorReason.QuantityInvalid, Success = false, Model = null });
            }

            // Check for uniqueness of the product name
            string uniqueName = await GetUniqueProductName(newProduct.Name);

            // Create the new product with unique name and default reserved quantity
            var createdProduct = await _repository.Insert(new Product
            {
                Name = uniqueName,
                InStockQuantity = newProduct.InStockQuantity,
                ReservedQuantity = 0
            });

            return Ok(new CreateResponse<Product> { Success = true, Model = createdProduct });
        }

        private async Task<string> GetUniqueProductName(string name)
        {
            string uniqueName = name.Trim();
            int counter = 1;

            while (await IsProductNameTaken(uniqueName))
            {
                uniqueName = $"{name.Trim()} ({counter++})";
            }

            return uniqueName;
        }

        private async Task<bool> IsProductNameTaken(string name)
        {
            var products = await _repository.List();
            return products.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
