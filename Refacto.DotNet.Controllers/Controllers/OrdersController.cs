using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Response;
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
        
        /*
         * In order to maintain backward compatibility
         * Contract and signature remain the same
         */
        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(long orderId)
        {
            try
            {
                var orderResponse = await _processOrderService.OrderProcessorAsync(orderId);

                return Ok(new ProcessOrderResponse(orderResponse.id)); // 200
            }
            catch (Exception e)
            {
                return BadRequest(e.Message); // 400
            }
            
        }
    }
}
