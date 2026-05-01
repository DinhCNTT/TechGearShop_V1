using System;
using System.Collections.Generic;

namespace TechGearShop_V1.Models.DTOs
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }

    public class PaginatedReviewDto
    {
        public List<ReviewDto> Reviews { get; set; } = new List<ReviewDto>();
        public int TotalReviews { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public decimal AverageRating { get; set; }
        public int[] RatingDistribution { get; set; } = new int[5]; // [1 star count, 2 star count, ...]
    }
}
