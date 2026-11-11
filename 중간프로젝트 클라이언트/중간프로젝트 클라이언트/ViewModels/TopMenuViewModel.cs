using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>순위 카드 한 개 (TopMenuWindow 의 First / Second / Third)</summary>
    public class TopMenuItem
    {
        public int Rank { get; set; }
        public List<string> MenuItems { get; set; } = new List<string>();
        public string MenuSummary => string.Join(" · ", MenuItems.Take(3));
        public int LikeCount { get; set; }
        public string DateText { get; set; } = "";        // "11월 5일 (목) · 중식"

        public static TopMenuItem From(MealInfo m) => new TopMenuItem
        {
            Rank = m.Rank,
            MenuItems = m.MenuItems,
            LikeCount = m.LikeCount,
            DateText = $"{FormatHelper.ShortDate(m.MealDate)} · {m.MealType}"
        };
    }

    /// <summary>
    /// 이달의 급식(TopMenuWindow) 화면 데이터
    /// XAML 바인딩 : TitleText, First, Second, Third  (없으면 null → 카드 숨김)
    /// </summary>
    public class TopMenuViewModel : ObservableObject
    {
        private readonly int _year;
        private readonly int _month;
        private readonly string _mealType;
        private TopMenuItem _first, _second, _third;

        /// <param name="mealType">null 이면 조식·중식·석식 통합 순위</param>
        public TopMenuViewModel(int year, int month, string mealType = null)
        {
            _year = year;
            _month = month;
            _mealType = mealType;
        }

        public string TitleText => $"{_month}월 이달의 급식";

        public TopMenuItem First { get { return _first; } private set { SetProperty(ref _first, value); } }
        public TopMenuItem Second { get { return _second; } private set { SetProperty(ref _second, value); } }
        public TopMenuItem Third { get { return _third; } private set { SetProperty(ref _third, value); } }

        public async Task LoadAsync()
        {
            try
            {
                var user = AppSession.CurrentUser;
                var top = await ApiService.GetTopFoodsAsync(user.SchoolName, _year, _month, 3, _mealType);

                First = top.Count > 0 ? TopMenuItem.From(top[0]) : null;
                Second = top.Count > 1 ? TopMenuItem.From(top[1]) : null;
                Third = top.Count > 2 ? TopMenuItem.From(top[2]) : null;
            }
            catch (Exception ex)
            {
                AlertWindow.Show("순위를 불러오지 못했어요.\n" + ex.Message, "급식타그램",
                                AlertWindow.AlertType.Error);
            }
        }
    }
}
