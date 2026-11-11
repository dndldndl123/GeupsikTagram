using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SchoolmealAPI.Models;
using SchoolmealAPI.Repositories;

namespace SchoolmealAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        // 컨트롤러 안에 있던 SQL을 이미 만들어 둔 CommentRepository 로 옮김
        private readonly CommentRepository _repository;

        public CommentController(IConfiguration configuration)
        {
            _repository = new CommentRepository(configuration.GetConnectionString("DefaultConnection"));
        }

        // [POST] /api/comment → 코멘트 작성
        [HttpPost]
        public IActionResult AddComment([FromBody] CommentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { message = "내용을 입력해 주세요." });
                if (request.Content.Length > 1000)   // Comment.comment 가 VARCHAR(1000)
                    return BadRequest(new { message = "코멘트는 1000자까지 쓸 수 있어요." });

                if (_repository.AddComment(request)) 
                    return Ok(new { message = "댓글이 작성되었습니다." });
                return BadRequest(new { message = "댓글 작성에 실패했습니다." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "댓글 작성 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // [GET] /api/comment?schoolName=가수원중학교&year=2026&month=11&mealType=중식
        [HttpGet]
        public IActionResult GetSchoolComments([FromQuery, BindRequired] string schoolName,
                                               [FromQuery] int year, [FromQuery] int month,
                                               [FromQuery] string? mealType)
        {
            try
            {
                if (year == 0) year = DateTime.Today.Year;
                if (month == 0) month = DateTime.Today.Month;
                if (month < 1 || month > 12) return BadRequest(new { message = "month는 1~12 사이여야 합니다." });

                string? meal = string.IsNullOrWhiteSpace(mealType) ? null : mealType.Trim();
                if (meal != null && meal != "조식" && meal != "중식" && meal != "석식")
                    return BadRequest(new { message = "mealType은 조식, 중식, 석식 중 하나여야 합니다." });

                return Ok(_repository.GetSchoolComments(schoolName.Trim(), year, month, meal));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "댓글을 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // [GET] /api/comment/{foodId} → 그 식단의 코멘트 목록
        [HttpGet("{foodId:int}")]
        public IActionResult GetComments(int foodId)
        {
            try
            {
                return Ok(_repository.GetComments(foodId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "댓글을 불러오는 중 오류가 발생했습니다.", error = ex.Message });
            }
        }

        // [DELETE] /api/comment/{commentId}?personId=abc → 내 코멘트 하나 삭제
        [HttpDelete("{commentId:int}")]
        public IActionResult DeleteComment(int commentId, [FromQuery] string personId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personId))
                    return BadRequest(new { message = "personId가 필요합니다." });

                if (_repository.DeleteComment(commentId, personId))
                    return Ok(new { message = "댓글이 삭제되었습니다." });

                // 번호가 없거나, 다른 사람이 쓴 코멘트
                return NotFound(new { message = "삭제할 댓글이 없거나 삭제 권한이 없습니다." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "댓글 삭제 중 오류가 발생했습니다.", error = ex.Message });
            }
        }
    }
}
