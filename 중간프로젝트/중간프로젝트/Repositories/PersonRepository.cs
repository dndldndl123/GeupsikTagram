using MySql.Data.MySqlClient;
using SchoolmealAPI.Models;

namespace SchoolmealAPI.Repositories
{
    public class PersonRepository
    {
        private readonly string _connectionString;
        public PersonRepository(string connectionString) { _connectionString = connectionString; }

        // 회원가입
        public bool SignUp(SignUpRequest req)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Person (PersonId, Password, Name, SchoolName, Type) VALUES (@Id, @Pw, @Name, @School, @Type)";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", req.PersonId);
                    cmd.Parameters.AddWithValue("@Pw", req.Password);
                    cmd.Parameters.AddWithValue("@Name", req.Name);
                    cmd.Parameters.AddWithValue("@School", req.SchoolName);
                    cmd.Parameters.AddWithValue("@Type", req.Type);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // 로그인
        public bool Login(LoginRequest req)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Person WHERE PersonId = @Id AND Password = @Pw";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", req.PersonId);
                    cmd.Parameters.AddWithValue("@Pw", req.Password);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // 사용자 정보 조회 (학교 이름, 이름, 구분)
        public PersonResponse? GetPerson(string personId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT PersonId, Name, SchoolName, Type FROM Person WHERE PersonId = @Id";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", personId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        return new PersonResponse
                        {
                            PersonId   = reader["PersonId"].ToString() ?? "",
                            Name       = reader["Name"].ToString() ?? "",
                            SchoolName = reader["SchoolName"].ToString() ?? "",
                            Type       = Convert.ToInt32(reader["Type"])
                        };
                    }
                }
            }
        }
    }
}
