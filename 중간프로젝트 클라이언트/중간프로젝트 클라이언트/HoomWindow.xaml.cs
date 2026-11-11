using System;
using System.Linq;
using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 홈 창 (로그인 후 처음 뜨는 창)
    ///  - 오늘의 점심(중식) 메뉴 표시
    ///  - "인기 메뉴" 카드 → TopMenuWindow (이번 달 TOP 3)
    ///  - "전체 식단" 카드 → StudentWindow (월별 달력)
    /// </summary>
    public partial class HoomWindow : Window
    {
        private StudentWindow _calendar;
        public HoomWindow()
        {
            InitializeComponent();

            // XAML 에 Click 이 없어서 여기서 연결
            BtnPopularMeals.Click += BtnPopularMeals_Click;
            BtnAllMeals.Click += BtnAllMeals_Click;

            var user = AppSession.CurrentUser;
            Title = "급식타그램";                                   
            TxtHeaderSchool.Text = user.SchoolName;                
            TxtHeaderUser.Text = $"{user.Name} · {user.TypeName}";  

            Loaded += async (s, e) => await LoadTodayMenuAsync();
        }

        private async System.Threading.Tasks.Task LoadTodayMenuAsync()
        {
            var today = DateTime.Today;
            if (today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday)
            {
                TxtLunchEmpty.Text = "오늘은 주말이라 급식이 없어요 🍃";
                return;
            }

            try
            {
                var user = AppSession.CurrentUser;
                var lunchmeals = await ApiService.GetFoodsAsync(user.SchoolName, today.Year, today.Month, MealTypeHelper.Lunch);
                var dinnermeals = await ApiService.GetFoodsAsync(user.SchoolName, today.Year, today.Month, MealTypeHelper.Dinner);

                var lunch = lunchmeals.FirstOrDefault(m => m.MealDate.Date == today);
                var dinner = dinnermeals.FirstOrDefault(m => m.MealDate.Date == today);

                TxtLunchEmpty.Text = lunch != null && lunch.MenuItems.Count > 0
                    ? lunch.MenuText
                    : "오늘은 급식 정보가 없어요.";

                TxtDinnerEmpty.Text = dinner != null && dinner.MenuItems.Count > 0
                   ? dinner.MenuText
                   : "오늘은 급식 정보가 없어요.";
            }
            catch (Exception ex)
            {
                TxtLunchEmpty.Text = "급식 정보를 불러오지 못했어요.\n" + ex.Message;
                TxtDinnerEmpty.Text = "급식 정보를 불러오지 못했어요.\n" + ex.Message;
            }
        }

        private void BtnPopularMeals_Click(object sender, RoutedEventArgs e)
        {
            var today = DateTime.Today;
            new TopMenuWindow(today.Year, today.Month) { Owner = this }.ShowDialog();
        }

        private void BtnAllMeals_Click(object sender, RoutedEventArgs e)
        {
            if (_calendar != null && _calendar.IsLoaded)
            {
                _calendar.Activate();       // 이미 열려 있으면 앞으로
                return;
            }
            _calendar = new StudentWindow { Owner = this };
            _calendar.Show();
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (AlertWindow.Confirm("로그아웃할까요?", "로그아웃", okText: "로그아웃"))
                AppSession.LogoutToLogin();
        }
    }
}
