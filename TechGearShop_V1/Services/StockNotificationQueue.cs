using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Services
{
    public class StockNotificationQueue : IStockNotificationQueue
    {
        private readonly Channel<int> _queue;

        public StockNotificationQueue()
        {
            // Bounded channel to prevent out of memory issues, 
            // drops oldest if queue is full (though rare for this scenario)
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _queue = Channel.CreateBounded<int>(options);
        }

        public async ValueTask QueueRestockNotificationAsync(int productId)
        {
            await _queue.Writer.WriteAsync(productId);
        }

        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            var productId = await _queue.Reader.ReadAsync(cancellationToken);
            return productId;
        }
    }
}
