using System;
using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 이달의 급식 창 (좋아요 TOP 3)
    ///  - new TopMenuWindow(연, 월) 로 띄우면 그 달 순위를 불러옴
    /// </summary>
    public partial class TopMenuWindow : Window
    {
        /// <summary>이번 달 순위</summary>
        public TopMenuWindow() : this(DateTime.Today.Year, DateTime.Today.Month) { }

        /// <param name="mealType">null 이면 조식·중식·석식 통합 순위, "중식" 등을 주면 그 끼니만</param>
        public TopMenuWindow(int year, int month, string mealType = null)
        {
            InitializeComponent();

            var vm = new TopMenuViewModel(year, month, mealType);
            DataContext = vm;
            Loaded += async (s, e) => await vm.LoadAsync();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
