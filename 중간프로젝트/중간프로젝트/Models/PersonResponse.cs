namespace SchoolmealAPI.Models
{
    /// <summary>클라이언트에 보내는 사용자 정보 (비밀번호는 절대 포함하지 않음)</summary>
    public class PersonResponse
    {
        public string PersonId { get; set; } = "";
        public string Name { get; set; } = "";
        public string SchoolName { get; set; } = "";
        public int Type { get; set; }               // 1:학생, 2:학부모, 3:영양사
        public string TypeName => Type switch
        {
            1 => "학생",
            2 => "학부모",
            3 => "영양사",
            _ => "기타"
        };
    }
}
