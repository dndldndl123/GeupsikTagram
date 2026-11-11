using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 학부모용 급식 상세 창 (보기 전용)
    ///  메뉴 이름 / 칼로리 / 급식 인원 / 영양정보 / 원산지
    /// </summary>
    public partial class ParentMealDetailWindow : Window
    {
        /// <summary>디자이너 미리보기용</summary>
        public ParentMealDetailWindow()
        {
            InitializeComponent();
        }

        public ParentMealDetailWindow(DayItem day) : this()
        {
            var vm = new ParentMealDetailViewModel(day);
            DataContext = vm;
            Title = $"급식 상세 - {vm.TitleText}";
            Loaded += async (s, e) => await vm.LoadAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
