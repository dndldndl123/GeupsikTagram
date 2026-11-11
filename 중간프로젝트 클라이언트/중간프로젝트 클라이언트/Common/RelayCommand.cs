using System;
using System.Windows.Input;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// XAML 의 Command="{Binding ...}" 에 연결할 수 있는 간단한 명령.
    ///   new RelayCommand(p => 할일(p), p => 실행가능한지(p))
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        // 입력이 바뀔 때마다 WPF 가 CanExecute 를 다시 확인 (버튼 활성/비활성 자동 갱신)
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public static void Refresh() => CommandManager.InvalidateRequerySuggested();
    }
}
