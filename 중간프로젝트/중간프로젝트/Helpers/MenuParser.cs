using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SchoolmealAPI.Helpers
{
    /// <summary>
    /// 한 끼 메뉴 문자열을 음식 이름 목록으로 나눠주는 도우미.
    ///
    /// 학교마다 알레르기 번호를 붙이는 방식이 달라서 아래 모양을 전부 처리합니다.
    ///   "김치찌개 (5.9.13)"      괄호 + 점
    ///   "된장국(5,6,9)"           괄호 + 쉼표
    ///   "소불고기（5.6.13）"      전각 괄호
    ///   "치즈닭갈비*1.2.5.6."    끝에 점 있음
    ///   "치즈닭갈비1.2.5.6.13"   끝에 점 없음
    ///   "깍두기9" / "우유 2"      숫자 하나
    ///   "요구르트 2 ."            숫자·점 사이 공백
    ///
    /// 알레르기 번호는 1~19 이므로, 그보다 큰 숫자는 이름의 일부로 보고 남깁니다.
    ///   "비타500", "에너지바20", "3분카레", "콜라 1.5L" → 그대로
    ///
    /// DB에 직접 넣은 "현미밥, 김치찌개" 같은 쉼표 구분 데이터도 처리합니다.
    /// </summary>
    public static class MenuParser
    {
        // 알레르기 번호 하나 : 1~19
        private const string Code = @"(?:1[0-9]|[1-9])";

        // 메뉴 사이 구분자 : <br>, <br/>, <br />, 줄바꿈, 쉼표 (단, 괄호 안 쉼표는 제외 → "(5,6,9)" 보호)
        private static readonly Regex SplitRegex =
            new Regex(@"<br\s*/?>|\r?\n|,(?![^()]*\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // 괄호 안 알레르기 번호 : "(5.9.13)", "( 5 , 9 )", "(1.2.5.)"
        private static readonly Regex ParenAllergyRegex =
            new Regex(@"\(\s*" + Code + @"(?:\s*[.,]\s*" + Code + @")*\s*[.,]?\s*\)", RegexOptions.Compiled);

        // 끝에 붙은 알레르기 번호 : "1.2.5.6.", "1.2.5.6", "9", "2 ."
        // (앞 글자가 숫자나 점이면 안 지움 → "비타500" 의 "00" 같은 걸 보호)
        private static readonly Regex TrailingAllergyRegex =
            new Regex(@"(?<![\d.])\s*" + Code + @"(?:\s*\.\s*" + Code + @")*\s*\.?\s*$", RegexOptions.Compiled);

        // NEIS가 붙이는 표시 기호 : * # @ + ★ ☆ ※ ⓢ ⓜ
        private static readonly Regex SymbolRegex =
            new Regex(@"[\*#@\+★☆※ⓢⓜ]", RegexOptions.Compiled);

        /// <summary>메뉴 문자열 → 음식 이름 리스트 (빈 값이면 빈 리스트)</summary>
        public static List<string> Parse(string? rawMenu)
        {
            if (string.IsNullOrWhiteSpace(rawMenu))
                return new List<string>();

            // "&amp;" 같은 HTML 문자 → "&"
            string text = WebUtility.HtmlDecode(rawMenu);

            return SplitRegex.Split(text)
                .Select(Clean)
                .Where(name => name.Length > 0)
                .ToList();
        }

        /// <summary>메뉴 문자열 → 한 줄에 하나씩 ("현미밥\n김치찌개\n...")</summary>
        public static string ToLines(string? rawMenu) => string.Join("\n", Parse(rawMenu));

        private static string Clean(string item)
        {
            string s = item.Replace('（', '(').Replace('）', ')');   // 전각 괄호 → 반각
            s = ParenAllergyRegex.Replace(s, "");
            s = SymbolRegex.Replace(s, "");          // 기호를 먼저 지워야 "닭갈비*1.2.5." 의 숫자가 끝으로 옴
            s = TrailingAllergyRegex.Replace(s, "");
            s = Regex.Replace(s, @"\s{2,}", " ");    // 공백 여러 개 → 하나
            return s.Trim();
        }
    }
}
