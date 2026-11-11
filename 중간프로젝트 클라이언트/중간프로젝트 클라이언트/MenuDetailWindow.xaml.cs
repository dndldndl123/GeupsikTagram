using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 메뉴 상세 창 (달력 칸을 누르면 뜸)
    ///  - 메뉴 / 칼로리 / 좋아요 / 코멘트 목록 / 코멘트 쓰기
    ///  - 화면 데이터와 버튼 동작은 MenuDetailViewModel 이 담당
    /// </summary>
    public partial class MenuDetailWindow : Window
    {
        private readonly MenuDetailViewModel _vm;

        /// <summary>디자이너 미리보기용 (데이터 없음)</summary>
        public MenuDetailWindow()
        {
            InitializeComponent();
        }

        public MenuDetailWindow(DayItem day) : this()
        {
            _vm = new MenuDetailViewModel(day);
            DataContext = _vm;
            Title = $"메뉴 상세 - {_vm.DateText}";

            Loaded += async (s, e) =>
            {
                await _vm.LoadAsync();
                TxtComment.Focus();
            };
        }

        /// <summary>좋아요나 코멘트가 바뀌었는지</summary>
        public bool Changed => _vm != null && _vm.Changed;
    }
}
