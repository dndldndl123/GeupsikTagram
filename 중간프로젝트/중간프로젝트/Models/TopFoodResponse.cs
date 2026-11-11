using System;
using System.Collections.Generic;

namespace SchoolmealAPI.Models
{
    /// <summary>이달의 급식 순위 한 줄</summary>
    public class TopFoodResponse
    {
        /// <summary>순위 (좋아요 수가 같으면 같은 순위 : 1, 1, 3 …)</summary>
        public int Rank { get; set; }

        public int FoodId { get; set; }
        public DateTime MealDate { get; set; }
        /// <summary>끼니 : "조식" / "중식" / "석식"</summary>
        public string MealType { get; set; } = "";

        /// <summary>학교명</summary>
        public string SchoolName { get; set; } = "";

        /// <summary>칼로리 (DB에 저장된 문자열 그대로, 예: "812.3 Kcal")</summary>
        public string Calories { get; set; } = "";
        public List<string> MenuItems { get; set; } = new List<string>();
        public string MenuText { get; set; } = "";
        public int LikeCount { get; set; }
    }
}
