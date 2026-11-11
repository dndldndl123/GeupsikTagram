using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 영양사 대시보드 (영양사 로그인 시 바로 뜨는 창)
    ///  - 왼쪽 : 한 달 급식 좋아요 순위
    ///  - 오른쪽 : 코멘트 (순위에서 하나를 누르면 그 급식 코멘트만)
    ///  - 화면 데이터와 버튼 동작은 NutritionistViewModel 이 담당
    /// </summary>
    public partial class NutritionistWindow : Window
    {
        public NutritionistWindow()
        {
            InitializeComponent();

            var vm = new NutritionistViewModel();
            DataContext = vm;
            Title = $"급식타그램 - 영양사 ({vm.SchoolName})";

            Loaded += async (s, e) => await vm.LoadAsync();
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (AlertWindow.Confirm("로그아웃할까요?", "로그아웃", okText: "로그아웃"))
                AppSession.LogoutToLogin();
        }
    }
}
