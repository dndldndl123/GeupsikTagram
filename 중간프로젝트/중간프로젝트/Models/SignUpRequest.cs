namespace SchoolmealAPI.Models
{
    public class SignUpRequest
    {
        public string PersonId { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string SchoolName { get; set; }
        public int Type { get; set; } // 1:학생, 2:학부모, 3:영양사
    }
}