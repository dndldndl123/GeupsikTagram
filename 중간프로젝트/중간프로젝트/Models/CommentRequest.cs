namespace SchoolmealAPI.Models
{
    public class CommentRequest
    {
        public int FoodId { get; set; }
        public string PersonId { get; set; }
        public string Content { get; set; }
    }
}