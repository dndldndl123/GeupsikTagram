using System;
using System.Collections.Generic;

namespace SchoolmealAPI.Models
{
    /// <summary>"이름 : 값" 한 줄 (영양정보 / 원산지)</summary>
    public class InfoItem
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Unit { get; set; } = "";   // 영양정보만 (g, mg, R.E …)
    }

    /// <summary>[GET] /api/food/{foodId} — 한 끼의 모든 정보 (학부모 상세 화면용)</summary>
    public class FoodDetailResponse
    {
        public int FoodId { get; set; }
        public string SchoolName { get; set; } = "";
        public string MealType { get; set; } = "";
        public DateTime MealDate { get; set; }
        public int? MealCount { get; set; }                       // 급식 인원수

        public List<string> MenuItems { get; set; } = new List<string>();
        public string Calories { get; set; } = "";                // "812.3 Kcal"

        public List<InfoItem> Nutrition { get; set; } = new List<InfoItem>();   // 영양정보
        public List<InfoItem> Origins { get; set; } = new List<InfoItem>();     // 원산지

        public int LikeCount { get; set; }

        // 원본 문자열도 같이 (파싱이 마음에 안 들 때 클라이언트에서 직접 쓸 수 있게)
        public string FoodName { get; set; } = "";
        public string NutritionInfo { get; set; } = "";
        public string OriginInfo { get; set; } = "";
    }
}
