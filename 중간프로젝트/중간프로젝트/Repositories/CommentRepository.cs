using MySql.Data.MySqlClient;
using SchoolmealAPI.Models;

namespace SchoolmealAPI.Repositories
{
    public class CommentRepository
    {
        private readonly string _connectionString;

        public CommentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool AddComment(CommentRequest request)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Comment (FoodId, PersonId, comment) VALUES (@FoodId, @PersonId, @Content)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FoodId", request.FoodId);
                    cmd.Parameters.AddWithValue("@PersonId", request.PersonId);
                    cmd.Parameters.AddWithValue("@Content", request.Content);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        // [NEW] 학교의 한 달 코멘트 전체 (어느 날 어느 끼니에 달린 건지 포함, 최신순) — 영양사 화면
        public List<SchoolCommentResponse> GetSchoolComments(string schoolName, int year, int month, string? mealType = null)
        {
            var list = new List<SchoolCommentResponse>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT c.CommentId, c.FoodId, c.PersonId, c.comment, p.Name, p.Type,
                           f.MealDate, f.MealType
                    FROM Comment c
                    JOIN Food f ON f.FoodId = c.FoodId
                    LEFT JOIN Person p ON p.PersonId = c.PersonId
                    WHERE f.SchoolName = @SchoolName
                      AND f.MealDate >= @Start AND f.MealDate < @End
                      AND (@MealType IS NULL OR f.MealType = @MealType)
                    ORDER BY c.CommentId DESC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    var start = new DateTime(year, month, 1);
                    cmd.Parameters.AddWithValue("@SchoolName", schoolName);
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", start.AddMonths(1));
                    cmd.Parameters.AddWithValue("@MealType", (object?)mealType ?? DBNull.Value);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SchoolCommentResponse
                            {
                                CommentId = Convert.ToInt32(reader["CommentId"]),
                                FoodId    = Convert.ToInt32(reader["FoodId"]),
                                PersonId  = reader["PersonId"].ToString() ?? "",
                                Writer    = reader["Name"] == DBNull.Value ? "알 수 없음" : reader["Name"].ToString()!,
                                Type      = reader["Type"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Type"]),
                                Content   = reader["comment"].ToString() ?? "",
                                MealDate  = Convert.ToDateTime(reader["MealDate"]),
                                MealType  = reader["MealType"] as string ?? ""
                            });
                        }
                    }
                }
            }
            return list;
        }

        // [NEW] 한 식단의 코멘트 목록 (작성자 이름 / 구분 포함)
        public List<CommentResponse> GetComments(int foodId)
        {
            var list = new List<CommentResponse>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // 탈퇴 등으로 Person 이 없어도 코멘트는 보이도록 LEFT JOIN
                string query = @"
                    SELECT c.CommentId, c.FoodId, c.PersonId, c.comment, p.Name, p.Type
                    FROM Comment c
                    LEFT JOIN Person p ON p.PersonId = c.PersonId
                    WHERE c.FoodId = @FoodId
                    ORDER BY c.CommentId DESC";   // 최신 코멘트가 위로

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FoodId", foodId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CommentResponse
                            {
                                CommentId = Convert.ToInt32(reader["CommentId"]),
                                FoodId   = Convert.ToInt32(reader["FoodId"]),
                                PersonId = reader["PersonId"].ToString() ?? "",
                                Writer   = reader["Name"] == DBNull.Value ? "알 수 없음" : reader["Name"].ToString()!,
                                Type     = reader["Type"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Type"]),
                                Content  = reader["comment"].ToString() ?? ""
                            });
                        }
                    }
                }
            }
            return list;
        }

        // [DELETE] 코멘트 하나 삭제 (본인이 쓴 것만)
        public bool DeleteComment(int commentId, string personId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"DELETE c FROM Comment c
               JOIN Food f   ON c.FoodId = f.FoodId
               JOIN Person p ON p.PersonId = @PersonId
               WHERE c.CommentId = @CommentId
                 AND ( c.PersonId = @PersonId                          -- 본인 댓글
                       OR (p.Type = 3 AND p.SchoolName = f.SchoolName) -- 같은 학교 영양사
                     )";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CommentId", commentId);
                    cmd.Parameters.AddWithValue("@PersonId", personId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
