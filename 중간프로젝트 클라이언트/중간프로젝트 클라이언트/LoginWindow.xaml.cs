using System;
using System.Windows;
using System.Windows.Controls;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 로그인 창
    ///  - 로그인 성공 → 학생: HoomWindow(홈) / 학부모: StudentWindow(달력) / 영양사: NutritionistWindow(대시보드)
    ///  - "회원가입" → RegisterWindow 를 팝업으로 띄움
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtLoginId.Focus();
        }

        // ================= 로그인 =================
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string id = TxtLoginId.Text.Trim();
            string pw = TxtLoginPwd.Password;

            if (id.Length == 0 || pw.Length == 0)
            {
                Warn("아이디와 비밀번호를 입력해 주세요.");
                (id.Length == 0 ? (Control)TxtLoginId : TxtLoginPwd).Focus();
                return;
            }

            var button = sender as Button;
            if (button != null) button.IsEnabled = false;
            try
            {
                var user = await ApiService.LoginAsync(id, pw);
                if (user == null)
                {
                    Warn("아이디 또는 비밀번호가 틀렸습니다.");
                    TxtLoginPwd.Clear();
                    TxtLoginPwd.Focus();
                    return;
                }

                AppSession.CurrentUser = user;

                // 학부모(Type 2) → 바로 달력,  영양사(Type 3) → 대시보드,  학생 → 홈
                Window next;
                if (AppSession.IsParent) next = new StudentWindow();
                else if (AppSession.IsNutritionist) next = new NutritionistWindow();
                else next = new HoomWindow();
                Application.Current.MainWindow = next;   // 로그인 창을 닫아도 프로그램이 꺼지지 않게
                next.Show();
                Close();
            }
            catch (Exception ex)
            {
                Warn("로그인 중 문제가 생겼어요.\n" + ex.Message);
            }
            finally
            {
                if (button != null) button.IsEnabled = true;
            }
        }

        // ================= 회원가입 창 열기 =================
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow { Owner = this };
            registerWindow.ShowDialog();

            // 가입에 성공했으면 아이디를 미리 채워줌
            if (!string.IsNullOrEmpty(registerWindow.RegisteredId))
            {
                TxtLoginId.Text = registerWindow.RegisteredId;
                TxtLoginPwd.Clear();
                TxtLoginPwd.Focus();
            }
        }

        private void Warn(string message)
        {
            AlertWindow.Show(message, "다시 입력해 주세요", AlertWindow.AlertType.Warning);
        }
    }

    /// <summary>회원가입 입력값 검사 (LoginWindow 패널 / RegisterWindow 공통)</summary>
    public static class SignUpValidator
    {
        /// <summary>문제가 없으면 null, 있으면 안내 메시지</summary>
        public static string Validate(SignUpData d, string passwordConfirm)
        {
            if (string.IsNullOrWhiteSpace(d.SchoolName)) return "학교 이름을 입력해 주세요.";
            if (string.IsNullOrWhiteSpace(d.Name)) return "이름을 입력해 주세요.";
            if (string.IsNullOrWhiteSpace(d.PersonId)) return "아이디를 입력해 주세요.";
            if (d.PersonId.Length < 3 || d.PersonId.Length > 50) return "아이디는 3~50자로 입력해 주세요.";
            if (d.PersonId.Contains(" ")) return "아이디에는 공백을 넣을 수 없어요.";
            if (string.IsNullOrEmpty(d.Password)) return "비밀번호를 입력해 주세요.";
            if (d.Password.Length < 4) return "비밀번호는 4자 이상으로 입력해 주세요.";
            if (d.Password != passwordConfirm) return "비밀번호 확인이 일치하지 않아요.";
            return null;
        }
    }
}
