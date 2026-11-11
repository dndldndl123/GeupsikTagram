using System;
using System.Collections.Generic;

namespace SchoolmealAPI.Models
{
    /// <summary>[GET] /api/food/ranking — 한 달 전체 급식 좋아요 순위 (영양사 화면)</summary>
    public class RankingResponse
    {
        public int Rank { get; set; }                 // 동점이면 같은 순위 (1, 1, 3 …)
        public int FoodId { get; set; }
        public DateTime MealDate { get; set; }
        public string MealType { get; set; } = "";
        public List<string> MenuItems { get; set; } = new List<string>();
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
    }

    /// <summary>[GET] /api/comment?schoolName=&year=&month= — 학교 한 달 코멘트 (어느 급식인지 포함)</summary>
    public class SchoolCommentResponse : CommentResponse
    {
        public DateTime MealDate { get; set; }
        public string MealType { get; set; } = "";
    }
}
