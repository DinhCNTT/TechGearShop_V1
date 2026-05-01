using TechGearShop_V1.Models.DTOs;

namespace TechGearShop_V1.Services.Interfaces
{
    /// <summary>
    /// Abstraction cho In-Memory Order Queue — dùng System.Threading.Channels bên dưới.
    /// Producer (Controller) gọi WriteAsync, Consumer (BackgroundService) gọi ReadAllAsync.
    /// </summary>
    public interface IOrderChannel
    {
        /// <summary>Đẩy một đơn hàng vào cuối hàng đợi (non-blocking với bounded capacity).</summary>
        ValueTask WriteAsync(OrderRequestDto request, CancellationToken ct = default);

        /// <summary>Lấy toàn bộ phần tử trong hàng đợi ra (stream — dùng trong BackgroundService).</summary>
        IAsyncEnumerable<OrderRequestDto> ReadAllAsync(CancellationToken ct = default);
    }
}
