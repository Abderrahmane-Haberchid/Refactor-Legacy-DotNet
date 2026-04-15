using Refacto.DotNet.Controllers.Dtos.Product;

namespace Refacto.DotNet.Controllers.Services;

public interface IProcessOrderService
{
    public Task<ProcessOrderResponse> OrderProcessor(long orderId, CancellationToken ct = default);
}