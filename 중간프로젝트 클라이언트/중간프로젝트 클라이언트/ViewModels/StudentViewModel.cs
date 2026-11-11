using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 메인 급식 달력(StudentWindow) 화면 데이터
    /// XAML 바인딩 : SchoolName, UserName, CurrentMonthText, Days,
    ///               PrevMonthCommand, NextMonthCommand, ChangeMealCommand
    /// </summary>
    public class StudentViewModel : ObservableObject
    {
        private readonly UserInfo _user;
        private int _year;
        private int _month;
        private string _mealType = MealTypeHelper.Lunch;   // XAML 기본 선택이 중식
        private List<DayItem> _days = new List<DayItem>();
        private bool _isLoading;

        public StudentViewModel()
        {
            _user = AppSession.CurrentUser;
            _year = DateTime.Today.Year;
            _month = DateTime.Today.Month;

            PrevMonthCommand = new RelayCommand(async _ => await MoveMonthAsync(-1), _ => !IsLoading);
            NextMonthCommand = new RelayCommand(async _ => await MoveMonthAsync(+1), _ => !IsLoading);
            ChangeMealCommand = new RelayCommand(async p => await ChangeMealAsync(MealTypeHelper.FromCommand(p)));
        }

        // ===== 바인딩 속성 =====
        public string SchoolName => _user.SchoolName;
        public string UserName => $"{_user.Name} · {_user.TypeName}";
        public string CurrentMonthText => $"{_year}년 {_month}월";

        public int Year => _year;
        public int Month => _month;
        public string MealType => _mealType;

        public List<DayItem> Days
        {
            get { return _days; }
            private set { SetProperty(ref _days, value); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); RelayCommand.Refresh(); }
        }

        public ICommand PrevMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand ChangeMealCommand { get; }

        // ===== 동작 =====

        private async Task MoveMonthAsync(int delta)
        {
            var d = new DateTime(_year, _month, 1).AddMonths(delta);
            _year = d.Year;
            _month = d.Month;
            OnPropertyChanged(nameof(CurrentMonthText));
            await LoadAsync();
        }

        private async Task ChangeMealAsync(string mealType)
        {
            if (_mealType == mealType) return;
            _mealType = mealType;
            await LoadAsync();
        }

        /// <summary>현재 연/월/끼니로 달력을 다시 채움 (평일 5열 × 5주 = 25칸)</summary>
        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var meals = await ApiService.GetFoodsAsync(_user.SchoolName, _year, _month, _mealType);
                var byDate = meals.GroupBy(m => m.MealDate.Date).ToDictionary(g => g.Key, g => g.First());

                Days = BuildCalendar(_year, _month, byDate);
            }
            catch (Exception ex)
            {
                AlertWindow.Show("급식 정보를 불러오지 못했어요.\n" + ex.Message, "급식타그램",
                                AlertWindow.AlertType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static List<DayItem> BuildCalendar(int year, int month, Dictionary<DateTime, MealInfo> byDate)
        {
            var list = new List<DayItem>();

            // 이번 달 첫 평일 → 그 주 월요일부터 시작
            var first = new DateTime(year, month, 1);
            while (first.DayOfWeek == DayOfWeek.Saturday || first.DayOfWeek == DayOfWeek.Sunday)
                first = first.AddDays(1);
            int diff = ((int)first.DayOfWeek + 6) % 7;      // 월=0, 화=1 ...
            var monday = first.AddDays(-diff);

            for (int week = 0; week < 5; week++)
            {
                for (int d = 0; d < 5; d++)                // 월~금
                {
                    var date = monday.AddDays(week * 7 + d);
                    bool isCurrent = date.Month == month;

                    MealInfo meal = null;
                    if (isCurrent) byDate.TryGetValue(date.Date, out meal);

                    list.Add(new DayItem
                    {
                        Date = date,
                        Day = date.Day,
                        DayOfWeek = date.DayOfWeek,
                        IsCurrentMonth = isCurrent,
                        IsToday = date.Date == DateTime.Today,
                        HasMeal = meal != null,
                        FoodId = meal != null ? meal.FoodId : 0,
                        MealType = meal != null ? meal.MealType : "",
                        MenuItems = meal != null ? meal.MenuItems : new List<string>(),
                        Menu = meal != null ? meal.MenuText : "",
                        Calories = meal != null ? FormatHelper.CaloriesNumber(meal.Calories) : "",
                        Likes = meal != null ? meal.LikeCount : 0
                    });
                }
            }
            return list;
        }
    }
}
