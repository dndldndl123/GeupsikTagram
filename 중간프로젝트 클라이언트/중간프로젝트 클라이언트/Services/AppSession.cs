using System.Linq;
using System.Windows;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 로그인한 사용자 정보를 프로그램 전체에서 같이 쓰기 위한 곳.
    /// 로그인 성공 시 AppSession.CurrentUser 에 저장 → 다른 창에서 꺼내 씀.
    /// </summary>
    public static class AppSession
    {
        private static UserInfo _currentUser;

        public static UserInfo CurrentUser
        {
            // 로그인 없이 창을 바로 띄워 테스트할 때를 위해 테스트 계정으로 대신함
            get { return _currentUser ?? ApiService.TestUser; }
            set { _currentUser = value; }
        }

        public static bool IsLoggedIn => _currentUser != null;

        /// <summary>학부모 계정인지 (Type 2)</summary>
        public static bool IsParent => CurrentUser.Type == 2;

        /// <summary>영양사 계정인지 (Type 3)</summary>
        public static bool IsNutritionist => CurrentUser.Type == 3;

        public static void Logout() => _currentUser = null;

        /// <summary>로그아웃 → 로그인 창만 남기고 나머지 창은 전부 닫기</summary>
        public static void LogoutToLogin()
        {
            _currentUser = null;

            var login = new LoginWindow();
            Application.Current.MainWindow = login;   // 프로그램의 기준 창을 로그인 창으로
            login.Show();

            // 로그인 창 말고 열려 있는 창 전부 닫기 (달력, 코멘트 창 등)
            foreach (var w in Application.Current.Windows.OfType<Window>().ToList())
                if (w != login) w.Close();
        }
    }

}
