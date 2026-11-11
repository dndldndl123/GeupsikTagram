using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SchoolmealAPI.Models;
using SchoolmealAPI.Repositories;
using SchoolmealAPI.Services;
using System;
using System.Linq;

namespace SchoolmealAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        private readonly FoodRepository _repository;
        private readonly NeisImportService _import;

        public FoodController(IConfiguration configuration, NeisImportService import)
        {
            string connStr = configuration.GetConnectionString("DefaultConnection");
            _repository = new FoodRepository(connStr);
            _import = import;
        }

        private static readonly string[] MealTypes = { "조식", "중식", "석식" };

        // mealType 이 비어 있으면 null(전체), 아니면 조식/중식/석식 중 하나인지 확인
        private static bool TryNormalizeMealType(string? input, out string? mealType)
        {
            mealType = string.IsNullOrWhiteSpace(input) ? null : input.Trim();
            return mealType == null || MealTypes.Contains(mealType);
        }

        // NEIS 자동 가져오기 : 학교 이름이 NEIS 공식 이름과 정확히 같을 때만
        // (실패해도 달력은 빈 채로 보여주면 되므로 오류를 밖으로 던지지 않음)
        private async Task TryAutoImportAsync(string schoolName, int year, int month)
        {
            try
            {
                var (school, _) = await _import.ResolveSchoolAsync(schoolName, exactOnly: true);
                if (school != null)
                    await _import.ImportMonthAsync(school, year, month);
            }
            catch
            {
                // NEIS 접속 실패 등 → 무시
            }
        }

        // 1. [GET] /api/food?schoolName=가수원중학교&year=2026&month=11&mealType=중식
        //    → 월별 식단 (MenuItems / MenuText 포함)
        //    schoolName 필수, mealType 생략 시 조식·중식·석식 전부
        //    DB에 그 학교·그 달 급식이 하나도 없으면 NEIS에서 자동으로 받아와서 채움
        [HttpGet]
        public async Task<IActionResult> GetFoods([FromQuery, BindRequired] string schoolName,
                                                  [FromQuery] int year, [FromQuery] int month,
                                                  [FromQuery] string? mealType)
        {
            try
            {
                // 파라미터를 안 넣으면 이번 달
                if (year == 0) year = DateTime.Today.Year;
                if (month == 0) month = DateTime.Today.Month;
                if (month < 1 || month > 12) return BadRequest(new { message = "month는 1~12 사이여야 합니다." });

                if (!TryNormalizeMealType(mealType, out var meal))
                    return BadRequest(new { message = "mealType은 조식, 중식, 석식 중 하나여야 합니다." });

                string school = schoolName.Trim();

                // 그 달 급식이 DB에 하나도 없으면 NEIS에서 자동으로 가져오기
                if (_repository.GetFoods(year, month, null, school).Count == 0)
                    await TryAutoImportAsync(school, year, month);

                var data = _repository.GetFoods(year, month, meal, school);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "데이터를 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // 2. [GET] /api/food/top?year=2026&month=11&count=3&mealType=중식&schoolName=OO고등학교
        //    → 이달의 급식 TOP N (좋아요순)
        //    mealType 생략 시 끼니 구분 없이 통합 순위
        [HttpGet("top")]
        public IActionResult GetTopFoods([FromQuery] int year, [FromQuery] int month,
                                         [FromQuery, BindRequired] string schoolName,
                                         [FromQuery] int count = 3,
                                         [FromQuery] string? mealType = null)
        {
            try
            {
                if (year == 0) year = DateTime.Today.Year;
                if (month == 0) month = DateTime.Today.Month;
                if (month < 1 || month > 12) return BadRequest(new { message = "month는 1~12 사이여야 합니다." });
                if (count < 1 || count > 50) count = 3;

                if (!TryNormalizeMealType(mealType, out var meal))
                    return BadRequest(new { message = "mealType은 조식, 중식, 석식 중 하나여야 합니다." });

                var data = _repository.GetTopFoods(year, month, count, meal, schoolName.Trim());
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "순위를 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // [GET] /api/food/ranking?schoolName=가수원중학교&year=2026&month=11&mealType=중식
        //    → 한 달 전체 급식 좋아요 순위 (좋아요 0개 포함, 코멘트 수 포함) — 영양사 화면
        [HttpGet("ranking")]
        public IActionResult GetRanking([FromQuery, BindRequired] string schoolName,
                                        [FromQuery] int year, [FromQuery] int month,
                                        [FromQuery] string? mealType)
        {
            try
            {
                if (year == 0) year = DateTime.Today.Year;
                if (month == 0) month = DateTime.Today.Month;
                if (month < 1 || month > 12) return BadRequest(new { message = "month는 1~12 사이여야 합니다." });
                if (!TryNormalizeMealType(mealType, out var meal))
                    return BadRequest(new { message = "mealType은 조식, 중식, 석식 중 하나여야 합니다." });

                return Ok(_repository.GetRanking(year, month, schoolName.Trim(), meal));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "순위를 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // [GET] /api/food/{foodId} → 한 끼 상세 (메뉴 / 칼로리 / 영양정보 / 원산지 / 급식인원)
        [HttpGet("{foodId:int}")]
        public IActionResult GetFoodDetail(int foodId)
        {
            try
            {
                var detail = _repository.GetFoodDetail(foodId);
                if (detail == null) return NotFound(new { message = "해당 급식을 찾을 수 없습니다." });
                return Ok(detail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "급식 상세를 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // 3. [GET] /api/food/{foodId}/like?personId=abc → 내가 좋아요 눌렀는지
        [HttpGet("{foodId:int}/like")]
        public IActionResult IsLiked(int foodId, [FromQuery] string personId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personId))
                    return BadRequest(new { message = "personId가 필요합니다." });

                return Ok(new { foodId, personId, isLiked = _repository.IsLiked(personId, foodId) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "좋아요 상태를 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // 4. [POST] /api/food/like 좋아요 등록
        [HttpPost("like")]
        public IActionResult AddLike([FromBody] PreferenceRequest request)
        {
            try
            {
                bool isSuccess = _repository.AddLike(request);
                if (isSuccess)
                    return Ok(new { message = "좋아요가 등록되었습니다." });
                else
                    return BadRequest(new { message = "좋아요 등록에 실패했습니다." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "이미 좋아요를 눌렀거나 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // 5. [DELETE] /api/food/like 좋아요 취소
        [HttpDelete("like")]
        public IActionResult CancelLike([FromBody] PreferenceRequest request)
        {
            try
            {
                bool isSuccess = _repository.CancelLike(request);
                if (isSuccess)
                    return Ok(new { message = "좋아요가 취소되었습니다." });
                else
                    return BadRequest(new { message = "취소할 좋아요 기록이 없습니다." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "좋아요 취소 중 오류가 발생했습니다.", error = ex.Message });
            }
        }
    }
}
