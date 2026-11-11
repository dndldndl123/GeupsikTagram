using System;
using System.Collections.Generic;

namespace 중간프로젝트_클라이언트
{
    // =====================================================================
    //  서버 API 응답과 똑같은 모양의 클래스들
    //  (나중에 서버 JSON 을 그대로 이 클래스로 받으면 됨)
    // =====================================================================

    /// <summary>사용자 정보  ←  서버 PersonResponse</summary>
    public class UserInfo
    {
        public string PersonId { get; set; } = "";
        public string Name { get; set; } = "";
        public string SchoolName { get; set; } = "";
        public int Type { get; set; }                     // 1:학생, 2:학부모, 3:영양사
        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case 1: return "학생";
                    case 2: return "학부모";
                    case 3: return "영양사";
                    default: return "기타";
                }
            }
        }
    }

    /// <summary>회원가입 입력값  →  서버 SignUpRequest</summary>
    public class SignUpData
    {
        public string PersonId { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public string SchoolName { get; set; } = "";
        public int Type { get; set; } = 1;
    }

    /// <summary>한 끼 급식  ←  서버 FoodResponse / TopFoodResponse</summary>
    public class MealInfo
    {
        public int FoodId { get; set; }
        public DateTime MealDate { get; set; }
        public string MealType { get; set; } = "";       // 조식 / 중식 / 석식
        public string SchoolName { get; set; } = "";
        public string Calories { get; set; } = "";       // "812.3 Kcal"
        public List<string> MenuItems { get; set; } = new List<string>();
        public string MenuText => string.Join("\n", MenuItems);
        public int LikeCount { get; set; }
        public int Rank { get; set; }                    // TOP 3 에서만 사용
    }

    /// <summary>"이름 : 값" 한 줄  ←  서버 InfoItem (영양정보 / 원산지)</summary>
    public class InfoItem
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Unit { get; set; } = "";
        public bool IsDomestic => Value != null && Value.Contains("국내산");   // 원산지 강조용
    }

    /// <summary>한 끼 상세  ←  서버 FoodDetailResponse (GET /api/food/{foodId})</summary>
    public class FoodDetailInfo
    {
        public int FoodId { get; set; }
        public string SchoolName { get; set; } = "";
        public string MealType { get; set; } = "";
        public DateTime MealDate { get; set; }
        public int? MealCount { get; set; }
        public List<string> MenuItems { get; set; } = new List<string>();
        public string Calories { get; set; } = "";
        public List<InfoItem> Nutrition { get; set; } = new List<InfoItem>();
        public List<InfoItem> Origins { get; set; } = new List<InfoItem>();
        public int LikeCount { get; set; }
    }

    /// <summary>코멘트  ←  서버 CommentResponse</summary>
    public class CommentInfo
    {
        public int CommentId { get; set; }
        public int FoodId { get; set; }
        public string PersonId { get; set; } = "";
        public string Writer { get; set; } = "";
        public int Type { get; set; }
        public string Role { get; set; } = "";           // 학생 / 학부모 / 영양사
        public string Content { get; set; } = "";
        public DateTime? CreatedAt { get; set; }         // 서버 테이블에 시간 칸이 생기면 사용

        // 학교 전체 코멘트(영양사 화면)에서만 채워짐
        public DateTime? MealDate { get; set; }
        public string MealType { get; set; } = "";
    }

    /// <summary>한 달 좋아요 순위 한 줄  ←  서버 RankingResponse (GET /api/food/ranking)</summary>
    public class RankingInfo
    {
        public int Rank { get; set; }
        public int FoodId { get; set; }
        public DateTime MealDate { get; set; }
        public string MealType { get; set; } = "";
        public List<string> MenuItems { get; set; } = new List<string>();
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
    }
}
