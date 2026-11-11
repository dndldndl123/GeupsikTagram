using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace 중간프로젝트_클라이언트
{
    /// <summary>코멘트 한 개 (MenuDetailWindow 의 CommentTemplate)</summary>
    public class CommentItem
    {
        public int CommentId { get; set; }
        public bool IsMine { get; set; }                 // 내가 쓴 코멘트면 X 버튼 표시
        public string Writer { get; set; } = "";
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string CreatedAtText { get; set; } = "";
        public string Initial => string.IsNullOrEmpty(Writer) ? "?" : Writer.Substring(0, 1);

        public static CommentItem From(CommentInfo c, string myPersonId) => new CommentItem
        {
            CommentId = c.CommentId,
            IsMine = c.PersonId == myPersonId,
            Writer = c.Writer,
            Role = c.Role,
            Content = c.Content,
            CreatedAtText = FormatHelper.TimeAgo(c.CreatedAt)
        };
    }

    /// <summary>
    /// 메뉴 상세 창(MenuDetailWindow) 화면 데이터
    /// XAML 바인딩 : MealTypeText, DateText, MenuItems, Calories, LikeCount, IsLiked,
    ///               LikeCommand, Comments, NewCommentText, AddCommentCommand
    /// </summary>
    public class MenuDetailViewModel : ObservableObject
    {
        private readonly int _foodId;
        private readonly UserInfo _user;
        private int _likeCount;
        private bool _isLiked;
        private string _newCommentText = "";
        private bool _busy;

        public MenuDetailViewModel(DayItem day)
        {
            _user = AppSession.CurrentUser;
            _foodId = day.FoodId;
            _likeCount = day.Likes;

            MealTypeText = $"{MealTypeHelper.Emoji(day.MealType)} {day.MealType}";
            DateText = FormatHelper.LongDate(day.Date);
            MenuItems = day.MenuItems ?? new List<string>();
            Calories = day.Calories;

            LikeCommand = new RelayCommand(async _ => await ToggleLikeAsync(), _ => !_busy);
            AddCommentCommand = new RelayCommand(async _ => await AddCommentAsync(),
                                                 _ => !_busy && !string.IsNullOrWhiteSpace(NewCommentText));
            DeleteCommentCommand = new RelayCommand(async p => await DeleteCommentAsync(p as CommentItem),
                                                    p => !_busy && p is CommentItem && ((CommentItem)p).IsMine);
        }

        // ===== 바인딩 속성 =====
        public string MealTypeText { get; }
        public string DateText { get; }
        public List<string> MenuItems { get; }
        public string Calories { get; }

        public int LikeCount
        {
            get { return _likeCount; }
            private set { SetProperty(ref _likeCount, value); }
        }

        /// <summary>♥ 버튼 상태 (버튼을 누르면 먼저 이 값이 바뀌고, 그다음 LikeCommand 실행)</summary>
        public bool IsLiked
        {
            get { return _isLiked; }
            set { SetProperty(ref _isLiked, value); }
        }

        public ObservableCollection<CommentItem> Comments { get; } = new ObservableCollection<CommentItem>();

        public string NewCommentText
        {
            get { return _newCommentText; }
            set { SetProperty(ref _newCommentText, value); }
        }

        public ICommand LikeCommand { get; }
        public ICommand AddCommentCommand { get; }

        /// <summary>코멘트 옆 X 버튼 (파라미터 : 그 CommentItem)</summary>
        public ICommand DeleteCommentCommand { get; }

        /// <summary>상세 창 내용이 바뀌었는지 (닫은 뒤 달력을 새로고침할지 판단용)</summary>
        public bool Changed { get; private set; }

        // ===== 동작 =====

        public async Task LoadAsync()
        {
            try
            {
                IsLiked = await ApiService.IsLikedAsync(_user.PersonId, _foodId);
                await ReloadCommentsAsync();
            }
            catch (Exception ex)
            {
                ShowError("메뉴 정보를 불러오지 못했어요.", ex);
            }
        }

        private async Task ReloadCommentsAsync()
        {
            var list = await ApiService.GetCommentsAsync(_foodId);
            Comments.Clear();
            foreach (var c in list) Comments.Add(CommentItem.From(c, _user.PersonId));
        }

        private async Task ToggleLikeAsync()
        {
            bool want = IsLiked;     // 버튼이 이미 바꿔놓은 값
            SetBusy(true);
            try
            {
                bool ok = want
                    ? await ApiService.AddLikeAsync(_user.PersonId, _foodId)
                    : await ApiService.CancelLikeAsync(_user.PersonId, _foodId);

                if (ok)
                {
                    LikeCount += want ? 1 : -1;
                    Changed = true;
                }
                else
                {
                    IsLiked = !want;  // 실패하면 버튼 상태 되돌리기
                }
            }
            catch (Exception ex)
            {
                IsLiked = !want;
                ShowError("좋아요 처리 중 문제가 생겼어요.", ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task AddCommentAsync()
        {
            string text = (NewCommentText ?? "").Trim();
            if (text.Length == 0) return;

            SetBusy(true);
            try
            {
                if (await ApiService.AddCommentAsync(_foodId, _user.PersonId, text))
                {
                    NewCommentText = "";
                    await ReloadCommentsAsync();
                    Changed = true;
                }
                else
                {
                    AlertWindow.Show("코멘트를 등록하지 못했어요.", "급식타그램",
                                    AlertWindow.AlertType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowError("코멘트 등록 중 문제가 생겼어요.", ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task DeleteCommentAsync(CommentItem item)
        {
            if (item == null || !item.IsMine) return;

            if (!AlertWindow.Confirm("이 코멘트를 삭제할까요?", "코멘트 삭제", okText: "삭제")) return;

            SetBusy(true);
            try
            {
                if (await ApiService.DeleteCommentAsync(item.CommentId, _user.PersonId))
                {
                    Comments.Remove(item);   // 화면에서 바로 지우고
                    Changed = true;
                }
                else
                {
                    AlertWindow.Show("이미 삭제됐거나 지울 수 없는 코멘트예요.", "급식타그램",
                                    AlertWindow.AlertType.Error);
                    await ReloadCommentsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError("코멘트 삭제 중 문제가 생겼어요.", ex);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            RelayCommand.Refresh();
        }

        private static void ShowError(string message, Exception ex)
        {
            AlertWindow.Show(message + "\n" + ex.Message, "급식타그램",
                            AlertWindow.AlertType.Error);
        }
    }
}
