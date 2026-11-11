using System.Globalization;
using System.Text.Json;
using SchoolmealAPI.Models;

namespace SchoolmealAPI.Services
{
    /// <summary>
    /// NEIS 교육정보 개방 포털(open.neis.go.kr) 호출 담당.
    ///  - 학교기본정보 (schoolInfo)        : 학교 이름 → 교육청 코드 + 학교 코드
    ///  - 급식식단정보 (mealServiceDietInfo) : 코드 + 기간 → 끼니별 급식
    /// </summary>
    public class NeisService
    {
        private const string BaseUrl = "https://open.neis.go.kr/hub/";
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public NeisService(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            // appsettings.json 의 "Neis:ApiKey" (키가 없으면 NEIS가 5건만 돌려줌)
            _apiKey = configuration["Neis:ApiKey"] ?? "";
        }

        // ===== 학교 검색 =====
        public async Task<List<NeisSchool>> SearchSchoolsAsync(string schoolName)
        {
            var rows = await GetRowsAsync("schoolInfo", new Dictionary<string, string>
            {
                ["SCHUL_NM"] = schoolName
            });

            return rows.Select(r => new NeisSchool
            {
                OfficeCode = Text(r, "ATPT_OFCDC_SC_CODE"),
                OfficeName = Text(r, "ATPT_OFCDC_SC_NM"),
                SchoolCode = Text(r, "SD_SCHUL_CODE"),
                SchoolName = Text(r, "SCHUL_NM"),
                SchoolKind = Text(r, "SCHUL_KND_SC_NM"),
                Address    = Text(r, "ORG_RDNMA")
            }).ToList();
        }

        // ===== 급식 조회 (기간) =====
        public async Task<List<NeisMeal>> GetMealsAsync(string officeCode, string schoolCode, DateTime from, DateTime to)
        {
            var rows = await GetRowsAsync("mealServiceDietInfo", new Dictionary<string, string>
            {
                ["ATPT_OFCDC_SC_CODE"] = officeCode,
                ["SD_SCHUL_CODE"]      = schoolCode,
                ["MLSV_FROM_YMD"]      = from.ToString("yyyyMMdd"),
                ["MLSV_TO_YMD"]        = to.ToString("yyyyMMdd")
            });

            var meals = new List<NeisMeal>();
            foreach (var r in rows)
            {
                if (!DateTime.TryParseExact(Text(r, "MLSV_YMD"), "yyyyMMdd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    continue;

                int? count = null;
                if (double.TryParse(Text(r, "MLSV_FGR"), NumberStyles.Any, CultureInfo.InvariantCulture, out var fgr))
                    count = (int)fgr;

                meals.Add(new NeisMeal
                {
                    SchoolName    = Text(r, "SCHUL_NM"),
                    MealType      = Text(r, "MMEAL_SC_NM"),   // "조식" / "중식" / "석식"
                    MealDate      = date,
                    MealCount     = count,
                    FoodName      = Text(r, "DDISH_NM"),
                    Calories      = Text(r, "CAL_INFO"),
                    NutritionInfo = Text(r, "NTR_INFO"),
                    OriginInfo    = Text(r, "ORPLC_INFO")
                });
            }
            return meals;
        }

        // ===== 공통 : NEIS 호출 → row 목록 =====
        // 성공 : { "서비스명": [ { "head": [ {list_total_count}, {RESULT} ] }, { "row": [ ... ] } ] }
        // 실패 : { "RESULT": { "CODE": "INFO-200", "MESSAGE": "해당하는 데이터가 없습니다." } }
        private async Task<List<JsonElement>> GetRowsAsync(string service, Dictionary<string, string> query)
        {
            var all = new List<JsonElement>();
            const int pageSize = 1000;   // NEIS 최대값

            for (int page = 1; ; page++)
            {
                var qs = new Dictionary<string, string>(query)
                {
                    ["Type"]   = "json",
                    ["pIndex"] = page.ToString(),
                    ["pSize"]  = pageSize.ToString()
                };
                if (!string.IsNullOrEmpty(_apiKey)) qs["KEY"] = _apiKey;

                string url = BaseUrl + service + "?" +
                             string.Join("&", qs.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

                string json = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // 실패 응답 (데이터 없음 포함)
                if (root.TryGetProperty("RESULT", out var result))
                {
                    string code = Text(result, "CODE");
                    if (code == "INFO-200") break;   // 데이터 없음 → 빈 목록
                    throw new InvalidOperationException($"NEIS 오류 {code}: {Text(result, "MESSAGE")}");
                }

                if (!root.TryGetProperty(service, out var body) || body.GetArrayLength() < 2)
                    break;

                int total = 0;
                var head = body[0].GetProperty("head");
                foreach (var h in head.EnumerateArray())
                    if (h.TryGetProperty("list_total_count", out var t)) total = t.GetInt32();

                var rows = body[1].GetProperty("row");
                foreach (var row in rows.EnumerateArray())
                    all.Add(row.Clone());   // doc 이 using 으로 해제되므로 복사

                if (all.Count >= total || rows.GetArrayLength() < pageSize) break;
            }
            return all;
        }

        private static string Text(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString().Trim() : "";
    }
}
