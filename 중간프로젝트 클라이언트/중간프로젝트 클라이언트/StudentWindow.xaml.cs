using System.Windows;
using System.Windows.Input;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 월별 급식 달력 창
    ///  - 화면 데이터와 버튼 동작은 StudentViewModel 이 담당
    ///  - 칸 클릭 → 학생·영양사: MenuDetailWindow / 학부모: ParentMealDetailWindow
    ///  - 이달의 급식 → TopMenuWindow (학부모는 버튼 숨김)
    /// </summary>
    public partial class StudentWindow : Window
    {
        private readonly StudentViewModel _vm;

        public StudentWindow()
        {
            InitializeComponent();

            _vm = new StudentViewModel();
            DataContext = _vm;

            // 학부모는 상세 정보만 보면 되므로 "이달의 급식" 버튼 숨김
            if (AppSession.IsParent)
                BtnTopMenu.Visibility = Visibility.Collapsed;

            Loaded += async (s, e) => await _vm.LoadAsync();
        }

        // 이달의 급식 (지금 보고 있는 달 기준, 세 끼니 통합 순위)
        private void TopMenuButton_Click(object sender, RoutedEventArgs e)
        {
            new TopMenuWindow(_vm.Year, _vm.Month) { Owner = this }.ShowDialog();
        }

        // 날짜 칸 클릭 → 메뉴 상세 (급식이 있는 이번 달 날짜만)
        private async void DayCell_Click(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var day = element != null ? element.DataContext as DayItem : null;
            if (day == null || !day.IsCurrentMonth || !day.HasMeal) return;

            // 학부모 → 칼로리·영양·원산지 상세 (보기 전용)
            if (AppSession.IsParent)
            {
                new ParentMealDetailWindow(day) { Owner = this }.ShowDialog();
                return;
            }

            // 학생·영양사 → 좋아요·코멘트 창
            var win = new MenuDetailWindow(day) { Owner = this };
            win.ShowDialog();

            // 좋아요/코멘트가 바뀌었으면 달력의 ♥ 숫자 갱신
            if (win.Changed) await _vm.LoadAsync();
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (AlertWindow.Confirm("로그아웃할까요?", "로그아웃", okText: "로그아웃"))
                AppSession.LogoutToLogin();
        }
    }
}
