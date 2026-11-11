using Microsoft.AspNetCore.Mvc;
using SchoolmealAPI.Models;
using SchoolmealAPI.Repositories;
using SchoolmealAPI.Services;

namespace SchoolmealAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private readonly PersonRepository _repository;
        private readonly NeisService _neis;   // ← 추가

        public PersonController(IConfiguration configuration, NeisService neis)   // ← 매개변수 추가
        {
            _repository = new PersonRepository(configuration.GetConnectionString("DefaultConnection"));
            _neis = neis;   // ← 추가
        }

        // [POST] /api/person/signup 회원가입
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            try
            {
                string school = (request.SchoolName ?? "").Trim();

                // NEIS 에 이름이 정확히 같은 학교가 있는지 확인
                var found = await _neis.SearchSchoolsAsync(school);
                if (school.Length == 0 || !found.Any(s => s.SchoolName == school))
                    return BadRequest(new { message = "학교명을 찾을 수 없습니다." });

                request.SchoolName = school;

                if (_repository.SignUp(request)) return Ok(new { message = "회원가입 성공" });
                return BadRequest(new { message = "회원가입 실패" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "회원가입 실패 (아이디 중복 등)", error = ex.Message });
            }
        }

        // [POST] /api/person/login 로그인
        // 로그인 성공 시 사용자 정보도 같이 내려줌 → 클라이언트가 바로 학교명/이름 표시 가능
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (!_repository.Login(request))
                return Unauthorized(new { message = "아이디 또는 비밀번호가 틀렸습니다" });

            var person = _repository.GetPerson(request.PersonId);
            return Ok(new { message = "로그인 성공", person });
        }

        // [GET] /api/person/{personId} → 학교 이름, 이름, 구분
        [HttpGet("{personId}")]
        public IActionResult GetPerson(string personId)
        {
            try
            {
                var person = _repository.GetPerson(personId);
                if (person == null) return NotFound(new { message = "사용자를 찾을 수 없습니다." });
                return Ok(person);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "사용자 정보를 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }
    }
}
