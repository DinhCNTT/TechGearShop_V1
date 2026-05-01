using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TechGearShop_V1.Models.DTOs;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    /// <summary>
    /// In-Memory Order Channel sử dụng System.Threading.Channels.
    ///
    /// Cấu hình:
    ///   - BoundedCapacity = 10_000: Tối đa 10.000 đơn hàng đang chờ trong RAM.
    ///     Nếu đầy, caller sẽ bị hold lại (backpressure) thay vì bị mất.
    ///   - SingleReader = true: Chỉ có 1 BackgroundService đọc → tối ưu lock overhead.
    ///   - SingleWriter = false: Nhiều HTTP request có thể ghi đồng thời.
    ///   - FullMode = Wait: Nếu queue đầy, WriteAsync sẽ await cho đến khi có chỗ trống.
    /// </summary>
    public sealed class OrderChannel : IOrderChannel
    {
        private readonly Channel<OrderRequestDto> _channel;

        public OrderChannel()
        {
            var options = new BoundedChannelOptions(10_000)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode     = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<OrderRequestDto>(options);
        }

        /// <inheritdoc/>
        public ValueTask WriteAsync(OrderRequestDto request, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(request, ct);

        /// <inheritdoc/>
        public async IAsyncEnumerable<OrderRequestDto> ReadAllAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var item in _channel.Reader.ReadAllAsync(ct))
            {
                yield return item;
            }
        }
    }
}
