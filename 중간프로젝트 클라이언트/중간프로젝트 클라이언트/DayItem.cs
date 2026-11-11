using System;
using System.Collections.Generic;

namespace 중간프로젝트_클라이언트
{
    /// <summary>달력(StudentWindow) 칸 하나</summary>
    public class DayItem
    {
        // ===== 화면에 바인딩되는 값 (StudentWindow.xaml 의 DayCellTemplate) =====
        public int Day { get; set; }
        public string Menu { get; set; } = "";            // 메뉴 한 줄씩 ("현미밥\n김치찌개\n...")
        public string Calories { get; set; } = "";        // 숫자만 ("812.3") → XAML 이 " kcal" 붙임
        public int Likes { get; set; }
        public bool HasMeal { get; set; }
        public bool IsToday { get; set; }
        public bool IsCurrentMonth { get; set; }
        public DayOfWeek DayOfWeek { get; set; }

        // ===== 상세 창(MenuDetailWindow)으로 넘길 값 =====
        public DateTime Date { get; set; }
        public int FoodId { get; set; }
        public string MealType { get; set; } = "";
        public List<string> MenuItems { get; set; } = new List<string>();
    }
}
