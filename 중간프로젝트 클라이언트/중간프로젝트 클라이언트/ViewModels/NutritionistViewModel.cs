using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace 중간프로젝트_클라이언트
{
    /// <summary>순위 목록 한 줄</summary>
    public class RankingRow
    {
        public int Rank { get; set; }
        public int FoodId { get; set; }
        public string DateText { get; set; } = "";       // 11월 5일 (목)
        public string MealTag { get; set; } = "";        // ☀️ 중식
        public string MealKey { get; set; } = "";        // 11월 5일 (목) 중식  (코멘트 제목용)
        public string MenuSummary { get; set; } = "";    // 현미밥 · 김치찌개 · 치즈닭갈비
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
    }

    /// <summary>코멘트 목록 한 줄</summary>
    public class DashboardComment
    {
        public int CommentId { get; set; }
        public int FoodId { get; set; }
        public string Writer { get; set; } = "";
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string MealTag { get; set; } = "";        // 11/5 중식
        public int MealLikeCount { get; set; }           // 그 급식의 좋아요 수
        public string CreatedAtText { get; set; } = "";
        public string Initial => string.IsNullOrEmpty(Writer) ? "?" : Writer.Substring(0, 1);
    }

    /// <summary>
    /// 영양사 대시보드(NutritionistWindow) 화면 데이터
    ///  - 왼쪽 : 한 달 급식 좋아요 순위 (전체)
    ///  - 오른쪽 : 코멘트 (순위에서 하나를 고르면 그 급식 코멘트만)
    /// </summary>
    public class NutritionistViewModel : ObservableObject
    {
        private readonly UserInfo _user;
        private int _year;
        private int _month;
        private string _mealFilter;                       // null = 전체
        private bool _isLoading;

        private List<RankingRow> _rankings = new List<RankingRow>();
        private List<DashboardComment> _allComments = new List<DashboardComment>();
        private List<DashboardComment> _comments = new List<DashboardComment>();
        private RankingRow _selectedRanking;

        public NutritionistViewModel()
        {
            _user = AppSession.CurrentUser;
            _year = DateTime.Today.Year;
            _month = DateTime.Today.Month;

            PrevMonthCommand = new RelayCommand(async _ => await MoveMonthAsync(-1), _ => !IsLoading);
            NextMonthCommand = new RelayCommand(async _ => await MoveMonthAsync(+1), _ => !IsLoading);
            ChangeMealFilterCommand = new RelayCommand(async p => await ChangeMealFilterAsync(p as string));
            ShowAllCommentsCommand = new RelayCommand(_ => SelectedRanking = null, _ => SelectedRanking != null);
            RefreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);
            DeleteCommentCommand = new RelayCommand(async p => await DeleteCommentAsync(p as DashboardComment), _ => !IsLoading);
        }

        // ===== 상단 =====
        public string SchoolName => _user.SchoolName;
        public string UserName => $"{_user.Name} · {_user.TypeName}";
        public string CurrentMonthText => $"{_year}년 {_month}월";

        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); RelayCommand.Refresh(); }
        }

        // ===== 요약 숫자 =====
        public int TotalLikes => _rankings.Sum(r => r.LikeCount);
        public int TotalComments => _allComments.Count;
        public string TopMealText
        {
            get
            {
                var top = _rankings.FirstOrDefault(r => r.LikeCount > 0);
                return top != null ? top.MealKey : "-";
            }
        }

        // ===== 왼쪽 : 순위 =====
        public List<RankingRow> Rankings
        {
            get { return _rankings; }
            private set
            {
                SetProperty(ref _rankings, value);
                OnPropertyChanged(nameof(HasRankings));
                OnPropertyChanged(nameof(TotalLikes));
                OnPropertyChanged(nameof(TopMealText));
            }
        }
        public bool HasRankings => _rankings.Count > 0;

        /// <summary>순위에서 고른 급식 (null 이면 전체 코멘트)</summary>
        public RankingRow SelectedRanking
        {
            get { return _selectedRanking; }
            set
            {
                if (SetProperty(ref _selectedRanking, value))
                {
                    ApplyCommentFilter();
                    RelayCommand.Refresh();
                }
            }
        }

        // ===== 오른쪽 : 코멘트 =====
        public List<DashboardComment> Comments
        {
            get { return _comments; }
            private set { SetProperty(ref _comments, value); OnPropertyChanged(nameof(HasComments)); }
        }
        public bool HasComments => _comments.Count > 0;

        public string CommentsTitle => _selectedRanking == null
            ? $"💬 {_month}월 전체 코멘트"
            : $"💬 {_selectedRanking.MealKey} 코멘트";

        // ===== 명령 =====
        public ICommand PrevMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand ChangeMealFilterCommand { get; }   // 파라미터 : "All" / "조식" / "중식" / "석식"
        public ICommand ShowAllCommentsCommand { get; }
        public ICommand RefreshCommand { get; }
        public RelayCommand DeleteCommentCommand { get; }

        // ===== 동작 =====
        private async Task MoveMonthAsync(int delta)
        {
            var d = new DateTime(_year, _month, 1).AddMonths(delta);
            _year = d.Year;
            _month = d.Month;
            OnPropertyChanged(nameof(CurrentMonthText));
            await LoadAsync();
        }

        private async Task ChangeMealFilterAsync(string param)
        {
            string filter = (param == null || param == "All") ? null : param;
            if (filter == _mealFilter) return;
            _mealFilter = filter;
            await LoadAsync();
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                // 순위와 코멘트를 동시에 요청
                var rankingTask = ApiService.GetRankingAsync(_user.SchoolName, _year, _month, _mealFilter);
                var commentTask = ApiService.GetSchoolCommentsAsync(_user.SchoolName, _year, _month, _mealFilter);
                await Task.WhenAll(rankingTask, commentTask);

                var ranking = rankingTask.Result;
                var likesByFood = ranking.ToDictionary(r => r.FoodId, r => r.LikeCount);

                Rankings = ranking.Select(r => new RankingRow
                {
                    Rank = r.Rank,
                    FoodId = r.FoodId,
                    DateText = FormatHelper.ShortDate(r.MealDate),
                    MealTag = $"{MealTypeHelper.Emoji(r.MealType)} {r.MealType}",
                    MealKey = $"{FormatHelper.ShortDate(r.MealDate)} {r.MealType}",
                    MenuSummary = string.Join(" · ", (r.MenuItems ?? new List<string>()).Take(3)),
                    LikeCount = r.LikeCount,
                    CommentCount = r.CommentCount
                }).ToList();

                _allComments = commentTask.Result.Select(c =>
                {
                    int likes;
                    likesByFood.TryGetValue(c.FoodId, out likes);
                    return new DashboardComment
                    {
                        CommentId = c.CommentId,
                        FoodId = c.FoodId,
                        Writer = c.Writer,
                        Role = c.Role,
                        Content = c.Content,
                        MealTag = c.MealDate.HasValue ? $"{c.MealDate.Value.Month}/{c.MealDate.Value.Day} {c.MealType}" : c.MealType,
                        MealLikeCount = likes,
                        CreatedAtText = FormatHelper.TimeAgo(c.CreatedAt)
                    };
                }).ToList();
                OnPropertyChanged(nameof(TotalComments));

                _selectedRanking = null;
                OnPropertyChanged(nameof(SelectedRanking));
                ApplyCommentFilter();
            }
            catch (Exception ex)
            {
                AlertWindow.Show("데이터를 불러오지 못했어요.\n" + ex.Message, "급식타그램",
                                AlertWindow.AlertType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyCommentFilter()
        {
            Comments = _selectedRanking == null
                ? _allComments
                : _allComments.Where(c => c.FoodId == _selectedRanking.FoodId).ToList();
            OnPropertyChanged(nameof(CommentsTitle));
        }
        private async Task DeleteCommentAsync(DashboardComment item)
        {
            if (item == null) return;

            if (!AlertWindow.Confirm($"{item.Writer}님의 코멘트를 삭제할까요?\n삭제하면 되돌릴 수 없어요.",
                                     "코멘트 삭제", okText: "삭제"))
                return;

            try
            {
                await ApiService.DeleteCommentAsync(item.CommentId, _user.PersonId);
            }
            catch (Exception ex)
            {
                AlertWindow.Show("코멘트를 삭제하지 못했어요.\n" + ex.Message, "급식타그램",
                                 AlertWindow.AlertType.Error);
                return;
            }

            // 다시 불러오면 선택이 풀리니까, 보고 있던 급식을 기억했다가 다시 선택
            int? keepFoodId = _selectedRanking?.FoodId;

            await LoadAsync();

            if (keepFoodId.HasValue)
                SelectedRanking = _rankings.FirstOrDefault(r => r.FoodId == keepFoodId.Value);
        }
    }
}
