using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IProcessOrderService _processOrderService;

        public OrdersController(IProcessOrderService productService)
        {
            _processOrderService =  productService;
        }
        
        // In order to maintain backward compatibility
        // Contarct and signature dont change

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            try
            {
                var orderResponse = _processOrderService.OrderProcessor(orderId);

                return Ok(new ProcessOrderResponse(orderResponse.Id)); // 200
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            
        }
    }
}
