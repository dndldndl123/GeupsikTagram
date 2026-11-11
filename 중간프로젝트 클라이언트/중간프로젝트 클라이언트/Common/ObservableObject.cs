using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 값이 바뀌면 화면(바인딩)에 알려주는 기본 클래스.
    /// ViewModel 들이 이걸 상속받아서 SetProperty(ref 필드, 값) 으로 값을 바꿉니다.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
