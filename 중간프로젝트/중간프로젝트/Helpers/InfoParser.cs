using System.Collections.Generic;
using System.Text.RegularExpressions;
using SchoolmealAPI.Models;

namespace SchoolmealAPI.Helpers
{
    /// <summary>
    /// NEIS 영양정보(NTR_INFO) / 원산지정보(ORPLC_INFO) 를 "이름 : 값" 목록으로 나눔.
    ///
    ///  영양 : "탄수화물(g) : 105.6<br/>단백질(g) : 34.2<br/>칼슘(mg) : 245.1"
    ///       → [ {탄수화물, 105.6, g}, {단백질, 34.2, g}, {칼슘, 245.1, mg} ]
    ///  원산지 : "쌀 : 국내산<br/>쇠고기(종류) : 국내산(한우)"
    ///       → [ {쌀, 국내산}, {쇠고기(종류), 국내산(한우)} ]
    /// </summary>
    public static class InfoParser
    {
        private static readonly Regex SplitRegex =
            new Regex(@"<br\s*/?>|\r?\n", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // "탄수화물(g)" → 이름 "탄수화물", 단위 "g"
        private static readonly Regex UnitRegex =
            new Regex(@"^(?<name>.*?)\s*\((?<unit>[^()]*)\)\s*$", RegexOptions.Compiled);

        /// <param name="extractUnit">영양정보는 true (괄호 안을 단위로), 원산지는 false (괄호 그대로)</param>
        public static List<InfoItem> Parse(string? raw, bool extractUnit)
        {
            var list = new List<InfoItem>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            foreach (var part in SplitRegex.Split(raw))
            {
                string line = part.Trim();
                if (line.Length == 0) continue;

                int colon = line.IndexOf(':');
                string name = colon >= 0 ? line.Substring(0, colon).Trim() : line;
                string value = colon >= 0 ? line.Substring(colon + 1).Trim() : "";
                string unit = "";

                if (extractUnit)
                {
                    var m = UnitRegex.Match(name);
                    if (m.Success)
                    {
                        name = m.Groups["name"].Value.Trim();
                        unit = m.Groups["unit"].Value.Trim();
                    }
                }

                if (name.Length > 0)
                    list.Add(new InfoItem { Name = name, Value = value, Unit = unit });
            }
            return list;
        }
    }
}
