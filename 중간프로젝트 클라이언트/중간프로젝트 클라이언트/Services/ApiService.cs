using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace 중간프로젝트_클라이언트
{
    /// <summary>
    /// 서버(SchoolmealAPI) 호출 담당.
    ///
    ///      LoginAsync        →  POST   /api/person/login
    ///      SignUpAsync       →  POST   /api/person/signup
    ///      GetFoodsAsync     →  GET    /api/food?schoolName=&year=&month=&mealType=
    ///      GetTopFoodsAsync  →  GET    /api/food/top?schoolName=&year=&month=&count=&mealType=
    ///      GetFoodDetailAsync→  GET    /api/food/{foodId}
    ///      GetFoodImageAsync →  GET    /api/food/{foodId}/image
    ///      GetRankingAsync   →  GET    /api/food/ranking?schoolName=&year=&month=&mealType=
    ///      GetSchoolCommentsAsync → GET /api/comment?schoolName=&year=&month=&mealType=
    ///      IsLikedAsync      →  GET    /api/food/{foodId}/like?personId=
    ///      AddLikeAsync      →  POST   /api/food/like
    ///      CancelLikeAsync   →  DELETE /api/food/like
    ///      GetCommentsAsync  →  GET    /api/comment/{foodId}
    ///      AddCommentAsync   →  POST   /api/comment
    ///      DeleteCommentAsync→  DELETE /api/comment/{commentId}?personId=
    ///
    /// 예상 가능한 실패(비밀번호 틀림, 아이디 중복 등)는 null / false / 메시지로 돌려주고,
    /// 서버 연결 실패·서버 오류(500 등)는 ApiException 을 던집니다.
    /// → 창/ViewModel 의 try-catch 가 받아서 안내 메시지를 띄움
    /// </summary>
    public static class ApiService
    {
        /// <summary>서버 주소 (Swagger 주소에서 /swagger 앞부분)</summary>
        public const string BaseUrl = "https://localhost:7192/";

        private static readonly HttpClient Http = CreateClient();

        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,                 // 서버 JSON 은 camelCase (foodId)
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase   // 보낼 때도 camelCase
        };

        /// <summary>
        /// 로그인 없이 창을 바로 띄워 테스트할 때 쓰는 계정 (AppSession 참고).
        /// 서버 DB 에 이 아이디가 있어야 좋아요/코멘트가 저장돼요.
        /// </summary>
        public static readonly UserInfo TestUser = new UserInfo
        {
            PersonId = "test",
            Name = "홍길동",
            SchoolName = "가수원중학교",
            Type = 1
        };

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler();
#if DEBUG
            // 개발 중 localhost 개발용 인증서를 믿지 못해 HTTPS 가 실패하는 경우 대비 (localhost 에만 적용)
            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                errors == System.Net.Security.SslPolicyErrors.None
                || (request.RequestUri != null && request.RequestUri.IsLoopback);
#endif
            return new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(20)   // NEIS 자동 가져오기가 있어서 넉넉하게
            };
        }

        // ================================================================
        //  사용자
        // ================================================================

        private class LoginResponse
        {
            public string Message { get; set; }
            public UserInfo Person { get; set; }
        }

        /// <summary>로그인 성공 시 사용자 정보, 아이디/비밀번호가 틀리면 null</summary>
        public static async Task<UserInfo> LoginAsync(string personId, string password)
        {
            var res = await SendAsync(HttpMethod.Post, "api/person/login", new { personId, password });
            if (res.StatusCode == HttpStatusCode.Unauthorized) return null;

            var body = await ReadAsync<LoginResponse>(res);
            return body?.Person;
        }

        /// <summary>회원가입. 성공하면 null, 실패하면 오류 메시지</summary>
        public static async Task<string> SignUpAsync(SignUpData data)
        {
            var res = await SendAsync(HttpMethod.Post, "api/person/signup", data, throwOnError: false);
            if (res.IsSuccessStatusCode) return null;
            if (res.StatusCode == HttpStatusCode.BadRequest)
                return await ReadMessageAsync(res) ?? "회원가입에 실패했어요. (아이디 중복 등)";

            throw await ToExceptionAsync(res);
        }

        // ================================================================
        //  급식
        // ================================================================

        /// <summary>월별 식단 (mealType 이 null 이면 세 끼니 전부)</summary>
        public static async Task<List<MealInfo>> GetFoodsAsync(string schoolName, int year, int month, string mealType)
        {
            string url = "api/food" + Query(
                "schoolName", schoolName,
                "year", year.ToString(),
                "month", month.ToString(),
                "mealType", mealType);

            var res = await SendAsync(HttpMethod.Get, url);
            return await ReadAsync<List<MealInfo>>(res) ?? new List<MealInfo>();
        }

        /// <summary>좋아요 많은 순 TOP N</summary>
        public static async Task<List<MealInfo>> GetTopFoodsAsync(string schoolName, int year, int month, int count, string mealType)
        {
            string url = "api/food/top" + Query(
                "schoolName", schoolName,
                "year", year.ToString(),
                "month", month.ToString(),
                "count", count.ToString(),
                "mealType", mealType);

            var res = await SendAsync(HttpMethod.Get, url);
            return await ReadAsync<List<MealInfo>>(res) ?? new List<MealInfo>();
        }

        /// <summary>한 끼 상세 (칼로리 / 영양정보 / 원산지 / 급식인원). 없으면 null</summary>
        public static async Task<FoodDetailInfo> GetFoodDetailAsync(int foodId)
        {
            var res = await SendAsync(HttpMethod.Get, $"api/food/{foodId}", throwOnError: false);
            if (res.StatusCode == HttpStatusCode.NotFound) return null;
            if (!res.IsSuccessStatusCode) throw await ToExceptionAsync(res);
            return await ReadAsync<FoodDetailInfo>(res);
        }

        /// <summary>한 달 전체 급식 좋아요 순위 (영양사 화면)</summary>
        public static async Task<List<RankingInfo>> GetRankingAsync(string schoolName, int year, int month, string mealType)
        {
            string url = "api/food/ranking" + Query(
                "schoolName", schoolName,
                "year", year.ToString(),
                "month", month.ToString(),
                "mealType", mealType);

            var res = await SendAsync(HttpMethod.Get, url);
            return await ReadAsync<List<RankingInfo>>(res) ?? new List<RankingInfo>();
        }

        // ================================================================
        //  좋아요
        // ================================================================

        private class LikeStatus
        {
            public bool IsLiked { get; set; }
        }

        public static async Task<bool> IsLikedAsync(string personId, int foodId)
        {
            var res = await SendAsync(HttpMethod.Get, $"api/food/{foodId}/like" + Query("personId", personId));
            var body = await ReadAsync<LikeStatus>(res);
            return body != null && body.IsLiked;
        }

        /// <summary>이미 눌렀으면 false (서버가 400 을 돌려줌)</summary>
        public static async Task<bool> AddLikeAsync(string personId, int foodId)
        {
            var res = await SendAsync(HttpMethod.Post, "api/food/like", new { personId, foodId }, throwOnError: false);
            if (res.IsSuccessStatusCode) return true;
            if (res.StatusCode == HttpStatusCode.BadRequest) return false;
            throw await ToExceptionAsync(res);
        }

        /// <summary>취소할 좋아요가 없으면 false</summary>
        public static async Task<bool> CancelLikeAsync(string personId, int foodId)
        {
            var res = await SendAsync(HttpMethod.Delete, "api/food/like", new { personId, foodId }, throwOnError: false);
            if (res.IsSuccessStatusCode) return true;
            if (res.StatusCode == HttpStatusCode.BadRequest) return false;
            throw await ToExceptionAsync(res);
        }

        // ================================================================
        //  코멘트
        // ================================================================

        /// <summary>최신 코멘트가 위로 (서버에서 정렬해서 줌)</summary>
        public static async Task<List<CommentInfo>> GetCommentsAsync(int foodId)
        {
            var res = await SendAsync(HttpMethod.Get, $"api/comment/{foodId}");
            return await ReadAsync<List<CommentInfo>>(res) ?? new List<CommentInfo>();
        }

        /// <summary>학교의 한 달 코멘트 전체 (최신순, 영양사 화면)</summary>
        public static async Task<List<CommentInfo>> GetSchoolCommentsAsync(string schoolName, int year, int month, string mealType)
        {
            string url = "api/comment" + Query(
                "schoolName", schoolName,
                "year", year.ToString(),
                "month", month.ToString(),
                "mealType", mealType);

            var res = await SendAsync(HttpMethod.Get, url);
            return await ReadAsync<List<CommentInfo>>(res) ?? new List<CommentInfo>();
        }

        /// <summary>내 코멘트 삭제. 없거나 남의 코멘트면 false (서버 404)</summary>
        public static async Task<bool> DeleteCommentAsync(int commentId, string personId)
        {
            var res = await SendAsync(HttpMethod.Delete, $"api/comment/{commentId}" + Query("personId", personId),
                                      throwOnError: false);
            if (res.IsSuccessStatusCode) 
                return true;
            if (res.StatusCode == HttpStatusCode.NotFound || res.StatusCode == HttpStatusCode.BadRequest) 
                return false;
            throw await ToExceptionAsync(res);
        }

        public static async Task<bool> AddCommentAsync(int foodId, string personId, string content)
        {
            var res = await SendAsync(HttpMethod.Post, "api/comment", new { foodId, personId, content }, throwOnError: false);
            if (res.IsSuccessStatusCode) return true;
            if (res.StatusCode == HttpStatusCode.BadRequest) return false;
            throw await ToExceptionAsync(res);
        }

        // ================================================================
        //  공통
        // ================================================================

        /// <summary>
        /// 요청 보내기. 서버에 연결이 안 되면 ApiException.
        /// throwOnError=true 이면 2xx 가 아닐 때도 ApiException (단, 401 은 호출한 쪽에서 판단하도록 그대로 반환)
        /// </summary>
        private static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url,
                                                                 object body = null, bool throwOnError = true)
        {
            var request = new HttpRequestMessage(method, url);
            if (body != null)
            {
                string json = JsonSerializer.Serialize(body, Json);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage res;
            try
            {
                res = await Http.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                throw new ApiException("서버 응답이 너무 늦어요. 잠시 후 다시 시도해 주세요.");
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("서버에 연결할 수 없어요. 서버가 켜져 있는지 확인해 주세요.\n(" + ex.Message + ")");
            }

            if (throwOnError && !res.IsSuccessStatusCode && res.StatusCode != HttpStatusCode.Unauthorized)
                throw await ToExceptionAsync(res);

            return res;
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage res) where T : class
        {
            string text = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(text, Json);
            }
            catch (JsonException)
            {
                throw new ApiException("서버 응답을 읽지 못했어요.");
            }
        }

        private class MessageBody
        {
            public string Message { get; set; }
            public string Error { get; set; }
        }

        /// <summary>서버가 보낸 { message: "..." } 꺼내기</summary>
        private static async Task<string> ReadMessageAsync(HttpResponseMessage res)
        {
            try
            {
                var body = await ReadAsync<MessageBody>(res);
                return body?.Message;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<ApiException> ToExceptionAsync(HttpResponseMessage res)
        {
            string msg = await ReadMessageAsync(res);
            return new ApiException(msg ?? $"서버 오류가 발생했어요. ({(int)res.StatusCode})", res.StatusCode);
        }

        /// <summary>"?키=값&키=값" 만들기 (값이 비어 있으면 그 항목은 빼고, 한글은 인코딩)</summary>
        private static string Query(params string[] keyValues)
        {
            var parts = new List<string>();
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
            {
                if (string.IsNullOrEmpty(keyValues[i + 1])) continue;
                parts.Add(keyValues[i] + "=" + Uri.EscapeDataString(keyValues[i + 1]));
            }
            return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
        }
    }

    /// <summary>서버 호출 실패 (메시지는 그대로 사용자에게 보여줘도 되는 문장)</summary>
    public class ApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public ApiException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
