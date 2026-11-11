using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SchoolmealAPI.Services;

namespace SchoolmealAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NeisController : ControllerBase
    {
        private readonly NeisService _neis;
        private readonly NeisImportService _import;

        public NeisController(NeisService neis, NeisImportService import)
        {
            _neis = neis;
            _import = import;
        }

        // [GET] /api/neis/schools?name=가수원중
        // → 학교 검색 (회원가입 때 학교 이름 확인용으로도 사용 가능)
        [HttpGet("schools")]
        public async Task<IActionResult> SearchSchools([FromQuery, BindRequired] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { message = "학교 이름을 입력해 주세요." });

                return Ok(await _neis.SearchSchoolsAsync(name.Trim()));
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "NEIS 학교 검색 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // [POST] /api/neis/import?schoolName=가수원중학교&year=2026&month=11
        // → 그 달 급식을 NEIS에서 받아 DB에 저장 (이미 있으면 갱신)
        //   같은 이름의 학교가 여러 지역에 있으면 officeCode(예: G10) 로 구분
        [HttpPost("import")]
        public async Task<IActionResult> Import([FromQuery, BindRequired] string schoolName,
                                                [FromQuery] int year, [FromQuery] int month,
                                                [FromQuery] string? officeCode = null)
        {
            try
            {
                if (year == 0) year = DateTime.Today.Year;
                if (month == 0) month = DateTime.Today.Month;
                if (month < 1 || month > 12) return BadRequest(new { message = "month는 1~12 사이여야 합니다." });

                var (school, candidates) = await _import.ResolveSchoolAsync(schoolName, officeCode);
                if (school == null)
                {
                    if (candidates.Count == 0)
                        return NotFound(new { message = $"'{schoolName}' 학교를 NEIS에서 찾을 수 없습니다." });

                    return Conflict(new
                    {
                        message = "같은 이름의 학교가 여러 개입니다. officeCode 를 함께 보내 주세요.",
                        candidates
                    });
                }

                var result = await _import.ImportMonthAsync(school, year, month);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { message = "NEIS 급식을 가져오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }
    }
}
