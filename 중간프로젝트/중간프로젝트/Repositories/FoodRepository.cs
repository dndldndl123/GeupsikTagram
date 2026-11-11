using MySql.Data.MySqlClient;
using SchoolmealAPI.Helpers;
using SchoolmealAPI.Models;

namespace SchoolmealAPI.Repositories
{
    public class FoodRepository
    {
        private readonly string _connectionString;

        public FoodRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 월별 식단 조회 (+ 메뉴를 음식 이름별로 나눠서 내려줌)
        //  mealType   : "조식" / "중식" / "석식", null = 전체
        //  schoolName : 학교명, null = 전체 학교
        public List<FoodResponse> GetFoods(int year, int month, string? mealType = null, string? schoolName = null)
        {
            List<FoodResponse> list = new List<FoodResponse>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                // 날짜 범위로 비교 → MealDate 인덱스를 탈 수 있음 (YEAR()/MONTH() 함수보다 빠름)
                string query = @"
                    SELECT f.FoodId, f.SchoolName, f.MealType, f.MealDate, f.FoodName, f.Calories,
                           (SELECT COUNT(*) FROM preference WHERE FoodId = f.FoodId) AS LikeCount
                    FROM Food f
                    WHERE f.MealDate >= @Start AND f.MealDate < @End
                      AND (@MealType IS NULL OR f.MealType = @MealType)
                      AND (@SchoolName IS NULL OR f.SchoolName = @SchoolName)
                    ORDER BY f.MealDate ASC, FIELD(f.MealType, '조식', '중식', '석식')";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    var start = new DateTime(year, month, 1);
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", start.AddMonths(1));
                    cmd.Parameters.AddWithValue("@MealType", (object?)mealType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SchoolName", (object?)schoolName ?? DBNull.Value);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? raw = reader["FoodName"] as string;
                            var items = MenuParser.Parse(raw);

                            list.Add(new FoodResponse
                            {
                                FoodId = Convert.ToInt32(reader["FoodId"]),
                                MealDate = Convert.ToDateTime(reader["MealDate"]),
                                MealType = reader["MealType"] as string ?? "",
                                SchoolName = reader["SchoolName"] as string ?? "",
                                Calories = reader["Calories"] as string ?? "",
                                FoodName = raw,
                                MenuItems = items,
                                MenuText = string.Join("\n", items),
                                LikeCount = Convert.ToInt32(reader["LikeCount"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // [NEW] 한 끼 상세 (칼로리 / 영양정보 / 원산지 / 급식인원 포함) — 없으면 null
        public FoodDetailResponse? GetFoodDetail(int foodId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT f.FoodId, f.SchoolName, f.MealType, f.MealDate, f.MealCount,
                           f.FoodName, f.Calories, f.NutritionInfo, f.OriginInfo,
                           (SELECT COUNT(*) FROM preference WHERE FoodId = f.FoodId) AS LikeCount
                    FROM Food f
                    WHERE f.FoodId = @Id";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", foodId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read()) return null;

                        string food = reader["FoodName"] as string ?? "";
                        string ntr  = reader["NutritionInfo"] as string ?? "";
                        string org  = reader["OriginInfo"] as string ?? "";

                        return new FoodDetailResponse
                        {
                            FoodId     = Convert.ToInt32(reader["FoodId"]),
                            SchoolName = reader["SchoolName"] as string ?? "",
                            MealType   = reader["MealType"] as string ?? "",
                            MealDate   = Convert.ToDateTime(reader["MealDate"]),
                            MealCount  = reader["MealCount"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["MealCount"]),
                            MenuItems  = MenuParser.Parse(food),
                            Calories   = reader["Calories"] as string ?? "",
                            Nutrition  = InfoParser.Parse(ntr, extractUnit: true),
                            Origins    = InfoParser.Parse(org, extractUnit: false),
                            LikeCount  = Convert.ToInt32(reader["LikeCount"]),
                            FoodName = food,
                            NutritionInfo = ntr,
                            OriginInfo = org
                        };
                    }
                }
            }
        }

        // [NEW] 이달의 급식 : 좋아요 많은 순 TOP N (기본 3개)
        //  - 좋아요가 0개인 식단은 순위에서 제외
        //  - 좋아요 수가 같으면 날짜가 빠른 식단이 앞, 순위 번호는 같게 (1, 1, 3)
        //  - mealType   : "조식" / "중식" / "석식", null = 끼니 구분 없이 통합 순위
        //  - schoolName : 학교명, null = 전체 학교
        public List<TopFoodResponse> GetTopFoods(int year, int month, int count, string? mealType = null, string? schoolName = null)
        {
            var list = new List<TopFoodResponse>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT f.FoodId, f.SchoolName, f.MealType, f.MealDate, f.FoodName, f.Calories,
                           COUNT(p.FoodId) AS LikeCount
                    FROM Food f
                    LEFT JOIN preference p ON p.FoodId = f.FoodId
                    WHERE f.MealDate >= @Start AND f.MealDate < @End
                      AND (@MealType IS NULL OR f.MealType = @MealType)
                      AND (@SchoolName IS NULL OR f.SchoolName = @SchoolName)
                    GROUP BY f.FoodId, f.SchoolName, f.MealType, f.MealDate, f.FoodName, f.Calories
                    HAVING COUNT(p.FoodId) > 0
                    ORDER BY LikeCount DESC, f.MealDate ASC
                    LIMIT @Count";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    var start = new DateTime(year, month, 1);
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", start.AddMonths(1));
                    cmd.Parameters.AddWithValue("@Count", count);
                    cmd.Parameters.AddWithValue("@MealType", (object?)mealType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SchoolName", (object?)schoolName ?? DBNull.Value);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        int index = 0, rank = 0, prevLikes = -1;
                        while (reader.Read())
                        {
                            index++;
                            int likes = Convert.ToInt32(reader["LikeCount"]);
                            if (likes != prevLikes) rank = index;   // 동점이면 이전 순위 유지
                            prevLikes = likes;

                            var items = MenuParser.Parse(reader["FoodName"] as string);
                            list.Add(new TopFoodResponse
                            {
                                Rank = rank,
                                FoodId = Convert.ToInt32(reader["FoodId"]),
                                MealDate = Convert.ToDateTime(reader["MealDate"]),
                                MealType = reader["MealType"] as string ?? "",
                                SchoolName = reader["SchoolName"] as string ?? "",
                                Calories = reader["Calories"] as string ?? "",
                                MenuItems = items,
                                MenuText = string.Join("\n", items),
                                LikeCount = likes
                            });
                        }
                    }
                }
            }
            return list;
        }

        // [NEW] NEIS에서 받은 급식 저장
        //  - (학교명, 날짜, 끼니)가 같은 행이 있으면 UPDATE → FoodId 가 그대로라 좋아요·코멘트 유지
        //  - 없으면 INSERT
        //  - 한 번에 트랜잭션으로 처리 (중간에 실패하면 전부 취소)
        public (int inserted, int updated) UpsertMeals(IEnumerable<NeisMeal> meals)
        {
            int inserted = 0, updated = 0;
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (MySqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var m in meals)
                        {
                            object? existingId;
                            using (var find = new MySqlCommand(
                                "SELECT FoodId FROM Food WHERE SchoolName = @School AND MealDate = @Date AND MealType = @Type LIMIT 1",
                                conn, tx))
                            {
                                find.Parameters.AddWithValue("@School", Cut(m.SchoolName, 100));
                                find.Parameters.AddWithValue("@Date", m.MealDate.Date);
                                find.Parameters.AddWithValue("@Type", Cut(m.MealType, 20));
                                existingId = find.ExecuteScalar();
                            }

                            string sql = existingId == null
                                ? @"INSERT INTO Food (SchoolName, MealType, MealDate, MealCount, FoodName, Calories, NutritionInfo, OriginInfo)
                                    VALUES (@School, @Type, @Date, @Count, @Food, @Cal, @Ntr, @Org)"
                                : @"UPDATE Food SET MealCount = @Count, FoodName = @Food, Calories = @Cal,
                                           NutritionInfo = @Ntr, OriginInfo = @Org
                                    WHERE FoodId = @Id";

                            using (var cmd = new MySqlCommand(sql, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@School", Cut(m.SchoolName, 100));
                                cmd.Parameters.AddWithValue("@Type", Cut(m.MealType, 20));
                                cmd.Parameters.AddWithValue("@Date", m.MealDate.Date);
                                cmd.Parameters.AddWithValue("@Count", (object?)m.MealCount ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Food", Cut(m.FoodName, 500));
                                cmd.Parameters.AddWithValue("@Cal", Cut(m.Calories, 50));
                                cmd.Parameters.AddWithValue("@Ntr", Cut(m.NutritionInfo, 1000));
                                cmd.Parameters.AddWithValue("@Org", Cut(m.OriginInfo, 1000));
                                if (existingId != null) cmd.Parameters.AddWithValue("@Id", existingId);
                                cmd.ExecuteNonQuery();
                            }

                            if (existingId == null) inserted++; else updated++;
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
            return (inserted, updated);
        }

        // 컬럼 길이(VARCHAR)보다 길면 잘라서 저장 오류 방지
        private static string Cut(string? s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

        // [NEW] 한 달 전체 급식 좋아요 순위 (좋아요 0개도 포함, 영양사 화면용)
        //  - 좋아요 많은 순 → 코멘트 많은 순 → 날짜 빠른 순
        public List<RankingResponse> GetRanking(int year, int month, string schoolName, string? mealType = null)
        {
            var list = new List<RankingResponse>();
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT f.FoodId, f.MealDate, f.MealType, f.FoodName,
                           (SELECT COUNT(*) FROM preference p WHERE p.FoodId = f.FoodId) AS LikeCount,
                           (SELECT COUNT(*) FROM Comment c WHERE c.FoodId = f.FoodId)    AS CommentCount
                    FROM Food f
                    WHERE f.MealDate >= @Start AND f.MealDate < @End
                      AND f.SchoolName = @SchoolName
                      AND (@MealType IS NULL OR f.MealType = @MealType)
                    ORDER BY LikeCount DESC, CommentCount DESC, f.MealDate ASC";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    var start = new DateTime(year, month, 1);
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", start.AddMonths(1));
                    cmd.Parameters.AddWithValue("@SchoolName", schoolName);
                    cmd.Parameters.AddWithValue("@MealType", (object?)mealType ?? DBNull.Value);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        int index = 0, rank = 0, prevLikes = -1;
                        while (reader.Read())
                        {
                            index++;
                            int likes = Convert.ToInt32(reader["LikeCount"]);
                            if (likes != prevLikes) rank = index;
                            prevLikes = likes;

                            list.Add(new RankingResponse
                            {
                                Rank = rank,
                                FoodId = Convert.ToInt32(reader["FoodId"]),
                                MealDate = Convert.ToDateTime(reader["MealDate"]),
                                MealType = reader["MealType"] as string ?? "",
                                MenuItems = MenuParser.Parse(reader["FoodName"] as string),
                                LikeCount = likes,
                                CommentCount = Convert.ToInt32(reader["CommentCount"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // [NEW] 이 사용자가 이 식단에 좋아요를 눌렀는지 (상세 창의 ♥ 버튼 상태용)
        public bool IsLiked(string personId, int foodId)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM preference WHERE PersonId = @PersonId AND FoodId = @FoodId";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonId", personId);
                    cmd.Parameters.AddWithValue("@FoodId", foodId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public bool AddLike(PreferenceRequest request)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "INSERT INTO preference (PersonId, FoodId) VALUES (@PersonId, @FoodId)";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonId", request.PersonId);
                    cmd.Parameters.AddWithValue("@FoodId", request.FoodId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        public bool CancelLike(PreferenceRequest request)
        {
            using (MySqlConnection conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                string query = "DELETE FROM preference WHERE PersonId = @PersonId AND FoodId = @FoodId";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonId", request.PersonId);
                    cmd.Parameters.AddWithValue("@FoodId", request.FoodId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
