using Refacto.DotNet.Controllers.Dtos.Response;

namespace Refacto.DotNet.Controllers.Services;

public interface IProcessOrderService
{
    public Task<ProcessOrderResponse> OrderProcessorAsync(long orderId, CancellationToken ct = default);
}