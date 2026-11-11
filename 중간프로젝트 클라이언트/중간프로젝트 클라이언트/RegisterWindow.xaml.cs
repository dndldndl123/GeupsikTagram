using System;
using System.Windows;
using System.Windows.Controls;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 회원가입 창 (LoginWindow 에서 팝업으로 띄움)
    ///  - 가입 성공 시 RegisteredId 에 아이디를 담고 창을 닫음 → 로그인 창이 아이디를 채워줌
    ///
    ///  ※ 이 창의 XAML 에는 "이름" 입력칸이 없어요. 서버 회원가입에는 이름이 필요해서
    ///     XAML 에 <TextBox x:Name="TxtName"/> 를 추가하면 자동으로 그 값을 사용하고,
    ///     없으면 임시로 아이디를 이름으로 씁니다.
    /// </summary>
    public partial class RegisterWindow : Window
    {
        /// <summary>가입에 성공한 아이디 (실패/취소면 null)</summary>
        public string RegisteredId { get; private set; }

        public RegisterWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtUsername.Focus();
        }

        private async void BtnRegisterSubmit_Click(object sender, RoutedEventArgs e)
        {
            string id = TxtUsername.Text.Trim();

            // XAML 에 TxtName 이 있으면 그 값을, 없으면 아이디를 이름으로 사용
            string name = TxtName.Text.Trim();

            var data = new SignUpData
            {
                PersonId = id,
                Password = TxtPassword.Password,
                Name = name,
                SchoolName = TxtSchoolName.Text.Trim(),
                Type = CboRole.SelectedIndex + 1        // 0:학생 → 1, 1:학부모 → 2, 2:영양사 → 3
            };

            string error = SignUpValidator.Validate(data, TxtConfirmPassword.Password);
            if (error != null)
            {
                Warn(error);
                return;
            }

            BtnRegisterSubmit.IsEnabled = false;
            try
            {
                string result = await ApiService.SignUpAsync(data);
                if (result != null)
                {
                    Warn(result);
                    TxtUsername.Focus();
                    TxtUsername.SelectAll();
                    return;
                }

                AlertWindow.Show("회원가입이 완료되었어요! 로그인해 주세요.","알림",
                                AlertWindow.AlertType.Success);
                RegisteredId = data.PersonId;
                Close();
            }
            catch (Exception ex)
            {
                Warn("회원가입 중 문제가 생겼어요.\n" + ex.Message);
            }
            finally
            {
                BtnRegisterSubmit.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            RegisteredId = null;
            Close();
        }

        private void Warn(string message)
        {
            AlertWindow.Show(message, "다시 입력해 주세요", AlertWindow.AlertType.Warning);
        }
    }
}
