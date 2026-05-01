using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QaController : ControllerBase
    {
        private readonly IProductQuestionService _qaService;

        public QaController(IProductQuestionService qaService)
        {
            _qaService = qaService;
        }

        /// <summary>
        /// GET /api/qa/{productId}?take=8&skip=0
        /// Endpoint public — ai cũng có thể đọc lịch sử câu hỏi.
        /// </summary>
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetHistory(int productId, int take = 8, int skip = 0)
        {
            var data = await _qaService.GetHistoryAsync(productId, take, skip);
            var total = await _qaService.GetTotalCountAsync(productId);
            return Ok(new { success = true, data, total });
        }
    }
}
