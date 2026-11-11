using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace 중간프로젝트_클라이언트
{
    public partial class AlertWindow : Window
    {
        public enum AlertType { Error, Warning, Info, Success }
        public bool Confirmed { get; private set; }
        public AlertWindow(string message, string title, AlertType type)
        {
            InitializeComponent();

            TxtTitle.Text = title;
            TxtMessage.Text = message;
            TxtIcon.Text = Icon(type);

            // 테두리 없는 창이라 직접 처리 : 끌어서 옮기기, Enter/Esc 로 닫기
            MouseLeftButtonDown += (s, e) => DragMove();
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter) { Confirmed = true; Close(); }
                else if (e.Key == Key.Escape) { Confirmed = false; Close(); }
            };
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            Close();
        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }

        private static new string Icon(AlertType type)
        {
            switch (type)
            {
                case AlertType.Error: return "❌";
                case AlertType.Info: return "ℹ️";
                case AlertType.Success: return "✅";
                default: return "⚠️";
            }
        }

        /// <summary>
        /// 어디서든 한 줄로 띄우기
        ///   AlertWindow.Show("아이디를 입력해 주세요.");
        ///   AlertWindow.Show("가입 완료!", "회원가입", AlertType.Success);
        /// </summary>
        public static void Show(string message, string title = "알림", AlertType type = AlertType.Warning)
        {
            var win = new AlertWindow(message, title, type);

            // 지금 사용 중인 창 위 가운데에 뜨도록
            var owner = Application.Current.Windows.OfType<Window>()
                                   .FirstOrDefault(w => w.IsActive && w != win);
            if (owner != null) win.Owner = owner;
            else win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            win.ShowDialog();
        }
        public static bool Confirm(string message, string title = "확인",
                           string okText = "확인", string cancelText = "취소")
        {
            var win = new AlertWindow(message, title, AlertType.Info);
            win.TxtIcon.Text = "❓";

            // 취소 버튼 보이기 : 두 버튼을 반반으로
            win.ColCancel.Width = new GridLength(1, GridUnitType.Star);
            win.ColGap.Width = new GridLength(10);
            win.BtnCancel.Visibility = Visibility.Visible;
            win.BtnConfirm.Content = okText;
            win.BtnCancel.Content = cancelText;

            var owner = Application.Current.Windows.OfType<Window>()
                                   .FirstOrDefault(w => w.IsActive && w != win);
            if (owner != null) win.Owner = owner;
            else win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            win.ShowDialog();
            return win.Confirmed;
        }
    }
}