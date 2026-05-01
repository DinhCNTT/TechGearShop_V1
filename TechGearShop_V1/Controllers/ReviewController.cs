using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TechGearShop_V1.Services.Interfaces;

namespace TechGearShop_V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> GetReviews(int productId, int page = 1, int pageSize = 5)
        {
            var data = await _reviewService.GetReviewsPaginatedAsync(productId, page, pageSize);
            return Ok(new { success = true, data = data });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddReview([FromForm] int productId, [FromForm] int rating, [FromForm] string content)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để đánh giá." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
            }

            var result = await _reviewService.AddReviewAsync(userId, productId, rating, content);
            
            if (result.Success)
                return Ok(new { success = true, message = result.Message });
            else
                return BadRequest(new { success = false, message = result.Message });
        }

        [HttpGet("my/{productId}")]
        public async Task<IActionResult> GetMyReview(int productId)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Ok(new { success = true, data = (object)null });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Ok(new { success = true, data = (object)null });

            var review = await _reviewService.GetMyReviewAsync(userId, productId);
            return Ok(new { success = true, data = review });
        }
    }
}
