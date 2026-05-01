using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TechGearShop_V1.Services.Interfaces
{
    public interface IStockNotificationQueue
    {
        ValueTask QueueRestockNotificationAsync(int productId);
        ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
    }
}
