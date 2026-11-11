using System;
using System.Collections.Generic;

namespace SchoolmealAPI.Models
{
    // 기존 FoodRequest.cs 파일 안에 있던 FoodResponse 를 이 파일로 교체하세요.
    public class FoodResponse
    {
        public int FoodId { get; set; }
        public DateTime MealDate { get; set; }
        /// <summary>끼니 : "조식" / "중식" / "석식"</summary>
        public string MealType { get; set; } = "";

        /// <summary>학교명</summary>
        public string SchoolName { get; set; } = "";

        /// <summary>칼로리 (DB에 저장된 문자열 그대로, 예: "812.3 Kcal")</summary>
        public string Calories { get; set; } = "";

        /// <summary>DB에 저장된 원본 문자열 (그대로 유지)</summary>
        public string? FoodName { get; set; }

        /// <summary>음식 이름만 하나씩 나눈 목록 ["현미밥", "김치찌개", ...]</summary>
        public List<string> MenuItems { get; set; } = new List<string>();

        /// <summary>한 줄에 하나씩 이어 붙인 문자열 "현미밥\n김치찌개\n..." (TextBlock에 바로 사용)</summary>
        public string MenuText { get; set; } = "";

        public int LikeCount { get; set; }
    }
}
