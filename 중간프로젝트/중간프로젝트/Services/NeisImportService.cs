using SchoolmealAPI.Models;
using SchoolmealAPI.Repositories;

namespace SchoolmealAPI.Services
{
    /// <summary>
    /// 학교 이름 + 연/월 → NEIS에서 한 달치 급식을 받아 Food 테이블에 저장
    /// </summary>
    public class NeisImportService
    {
        private readonly NeisService _neis;
        private readonly FoodRepository _foods;

        public NeisImportService(NeisService neis, IConfiguration configuration)
        {
            _neis = neis;
            _foods = new FoodRepository(configuration.GetConnectionString("DefaultConnection"));
        }

        /// <summary>
        /// 학교 이름으로 NEIS 학교를 하나로 정한다.
        ///  - 이름이 정확히 같은 학교가 하나면 그 학교
        ///  - exactOnly=false 이고 검색 결과가 하나뿐이면 그 학교 (예: "가수원중" → "가수원중학교")
        ///  - officeCode 를 주면 같은 이름 학교가 여러 지역에 있을 때 구분
        /// 정하지 못하면 null + 후보 목록
        /// </summary>
        public async Task<(NeisSchool? school, List<NeisSchool> candidates)> ResolveSchoolAsync(
            string schoolName, string? officeCode = null, bool exactOnly = false)
        {
            var found = await _neis.SearchSchoolsAsync(schoolName.Trim());
            if (!string.IsNullOrWhiteSpace(officeCode))
                found = found.Where(s => s.OfficeCode == officeCode).ToList();

            var exact = found.Where(s => s.SchoolName == schoolName.Trim()).ToList();
            if (exact.Count == 1) return (exact[0], found);
            if (!exactOnly && exact.Count == 0 && found.Count == 1) return (found[0], found);
            return (null, found);
        }

        /// <summary>한 달치 급식 가져와서 저장</summary>
        public async Task<NeisImportResult> ImportMonthAsync(NeisSchool school, int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var to = from.AddMonths(1).AddDays(-1);

            var meals = await _neis.GetMealsAsync(school.OfficeCode, school.SchoolCode, from, to);

            // DB에는 NEIS 공식 학교명으로 저장 (SCHUL_NM 이 비어 있으면 검색 결과 이름 사용)
            foreach (var m in meals)
                if (string.IsNullOrEmpty(m.SchoolName)) m.SchoolName = school.SchoolName;

            meals = meals
                .GroupBy(m => new { m.MealDate, m.MealType })
                .Select(g => g.First())
                .ToList();

            var (inserted, updated) = _foods.UpsertMeals(meals);

            return new NeisImportResult
            {
                SchoolName = school.SchoolName,
                Year = year,
                Month = month,
                Total = meals.Count,
                Inserted = inserted,
                Updated = updated
            };
        }
    }
}
