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
        
        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(long orderId, CancellationToken token)
        {
            try
            {
                var orderResponse = await _processOrderService.OrderProcessorAsync(orderId, token);
                return Ok(new ProcessOrderResponse(orderResponse.id));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message); 
            }
            
        }
    }
}
