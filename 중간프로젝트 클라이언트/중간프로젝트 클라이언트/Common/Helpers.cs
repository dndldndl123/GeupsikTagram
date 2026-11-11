using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace 중간프로젝트_클라이언트
{
    /// <summary>끼니 이름 변환</summary>
    public static class MealTypeHelper
    {
        public const string Breakfast = "조식";
        public const string Lunch = "중식";
        public const string Dinner = "석식";

        /// <summary>StudentWindow 버튼의 CommandParameter("Breakfast"/"Lunch"/"Dinner") → "조식"/"중식"/"석식"</summary>
        public static string FromCommand(object parameter)
        {
            switch (parameter as string)
            {
                case "Breakfast": return Breakfast;
                case "Dinner": return Dinner;
                default: return Lunch;
            }
        }

        public static string Emoji(string mealType)
        {
            switch (mealType)
            {
                case Breakfast: return "🌅";
                case Dinner: return "🌙";
                default: return "☀️";
            }
        }
    }

    /// <summary>날짜·칼로리 표시용</summary>
    public static class FormatHelper
    {
        public static readonly CultureInfo Korean = new CultureInfo("ko-KR");

        /// <summary>11월 5일 (목)</summary>
        public static string ShortDate(DateTime d) => d.ToString("M월 d일 (ddd)", Korean);

        /// <summary>2026년 11월 5일 (목)</summary>
        public static string LongDate(DateTime d) => d.ToString("yyyy년 M월 d일 (ddd)", Korean);

        /// <summary>"812.3 Kcal" → "812.3"  (XAML 에서 " kcal" 을 붙이기 때문에 숫자만)</summary>
        public static string CaloriesNumber(string calories)
        {
            if (string.IsNullOrWhiteSpace(calories)) return "";
            var m = Regex.Match(calories, @"\d+(\.\d+)?");
            return m.Success ? m.Value : calories;
        }

        /// <summary>방금 전 / 5분 전 / 3시간 전 / 어제 / 11월 2일</summary>
        public static string TimeAgo(DateTime? time)
        {
            if (time == null) return "";
            var diff = DateTime.Now - time.Value;
            if (diff.TotalMinutes < 1) return "방금 전";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}분 전";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}시간 전";
            if (diff.TotalDays < 2) return "어제";
            return time.Value.ToString("M월 d일", Korean);
        }
    }
}
