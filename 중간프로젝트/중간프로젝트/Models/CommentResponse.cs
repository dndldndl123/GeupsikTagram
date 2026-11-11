namespace SchoolmealAPI.Models
{
    /// <summary>메뉴 상세 창에 보여줄 코멘트 한 개</summary>
    public class CommentResponse
    {
        public int CommentId { get; set; }
        public int FoodId { get; set; }
        public string PersonId { get; set; } = "";
        public string Writer { get; set; } = "";     // 작성자 이름 (Person.Name)
        public int Type { get; set; }                // 1:학생, 2:학부모, 3:영양사
        public string Role => Type switch
        {
            1 => "학생",
            2 => "학부모",
            3 => "영양사",
            _ => "기타"
        };
        public string Content { get; set; } = "";
    }
}