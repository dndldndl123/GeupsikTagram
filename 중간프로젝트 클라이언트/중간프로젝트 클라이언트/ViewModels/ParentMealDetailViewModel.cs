using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>메뉴 한 줄 (번호 + 이름)</summary>
    public class MenuLine
    {
        public int No { get; set; }
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// 학부모용 급식 상세 창(ParentMealDetailWindow) 화면 데이터
    /// 메뉴 / 칼로리 / 급식 인원 / 영양정보 / 원산지  (보기 전용)
    /// </summary>
    public class ParentMealDetailViewModel : ObservableObject
    {
        private readonly int _foodId;
        private bool _isLoading = true;
        private string _caloriesNumber = "";
        private string _mealCountText = "";
        private List<MenuLine> _menuLines = new List<MenuLine>();
        private List<InfoItem> _nutrition = new List<InfoItem>();
        private List<InfoItem> _origins = new List<InfoItem>();

        public ParentMealDetailViewModel(DayItem day)
        {
            _foodId = day.FoodId;

            // 서버 응답 오기 전에도 달력에 있던 값으로 먼저 보여줌
            TitleText = $"{FormatHelper.ShortDate(day.Date)} {day.MealType}";
            MealTypeText = $"{MealTypeHelper.Emoji(day.MealType)} {day.MealType}";
            SchoolName = AppSession.CurrentUser.SchoolName;
            CaloriesNumber = day.Calories;
            MenuLines = ToLines(day.MenuItems);
        }

        // ===== 바인딩 속성 =====
        public string TitleText { get; }          // 11월 5일 (목) 중식
        public string MealTypeText { get; }       // ☀️ 중식
        public string SchoolName { get; }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); }
        }

        public string CaloriesNumber
        {
            get { return _caloriesNumber; }
            private set { SetProperty(ref _caloriesNumber, value); }
        }

        /// <summary>"812명" (정보 없으면 "-")</summary>
        public string MealCountText
        {
            get { return _mealCountText; }
            private set { SetProperty(ref _mealCountText, value); }
        }

        public List<MenuLine> MenuLines
        {
            get { return _menuLines; }
            private set { SetProperty(ref _menuLines, value); }
        }

        public List<InfoItem> Nutrition
        {
            get { return _nutrition; }
            private set { SetProperty(ref _nutrition, value); OnPropertyChanged(nameof(HasNutrition)); }
        }

        public List<InfoItem> Origins
        {
            get { return _origins; }
            private set { SetProperty(ref _origins, value); OnPropertyChanged(nameof(HasOrigins)); }
        }

        public bool HasNutrition => _nutrition.Count > 0;
        public bool HasOrigins => _origins.Count > 0;

        // ===== 동작 =====
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var d = await ApiService.GetFoodDetailAsync(_foodId);
                if (d == null)
                {
                    AlertWindow.Show("급식 정보를 찾을 수 없어요.", "급식타그램",
                                    AlertWindow.AlertType.Error);
                    return;
                }

                CaloriesNumber = FormatHelper.CaloriesNumber(d.Calories);
                MealCountText = d.MealCount.HasValue ? $"{d.MealCount.Value:N0}명" : "-";
                if (d.MenuItems.Count > 0) MenuLines = ToLines(d.MenuItems);
                Nutrition = d.Nutrition ?? new List<InfoItem>();
                Origins = d.Origins ?? new List<InfoItem>();
            }
            catch (Exception ex)
            {
                AlertWindow.Show("급식 상세를 불러오지 못했어요.\n" + ex.Message, "급식타그램",
                                AlertWindow.AlertType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static List<MenuLine> ToLines(List<string> items)
        {
            return (items ?? new List<string>())
                .Select((name, i) => new MenuLine { No = i + 1, Name = name })
                .ToList();
        }
    }
}
