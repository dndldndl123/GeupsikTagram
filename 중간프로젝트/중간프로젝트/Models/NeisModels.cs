using System;

namespace SchoolmealAPI.Models
{
    /// <summary>NEIS 학교 검색 결과 한 개</summary>
    public class NeisSchool
    {
        public string OfficeCode { get; set; } = "";   // ATPT_OFCDC_SC_CODE (시도교육청 코드, 예: G10 = 대전)
        public string OfficeName { get; set; } = "";   // ATPT_OFCDC_SC_NM
        public string SchoolCode { get; set; } = "";   // SD_SCHUL_CODE (학교 코드)
        public string SchoolName { get; set; } = "";   // SCHUL_NM
        public string SchoolKind { get; set; } = "";   // SCHUL_KND_SC_NM (초/중/고)
        public string Address { get; set; } = "";      // ORG_RDNMA (도로명 주소)
    }

    /// <summary>NEIS 급식 한 끼</summary>
    public class NeisMeal
    {
        public string SchoolName { get; set; } = "";   // SCHUL_NM
        public string MealType { get; set; } = "";     // MMEAL_SC_NM (조식/중식/석식)
        public DateTime MealDate { get; set; }         // MLSV_YMD
        public int? MealCount { get; set; }            // MLSV_FGR (급식 인원수)
        public string FoodName { get; set; } = "";     // DDISH_NM (요리명, <br/> 로 구분)
        public string Calories { get; set; } = "";     // CAL_INFO (예: "812.3 Kcal")
        public string NutritionInfo { get; set; } = "";// NTR_INFO
        public string OriginInfo { get; set; } = "";   // ORPLC_INFO
    }

    /// <summary>가져오기 결과</summary>
    public class NeisImportResult
    {
        public string SchoolName { get; set; } = "";
        public int Year { get; set; }
        public int Month { get; set; }
        public int Total { get; set; }      // NEIS에서 받은 끼니 수
        public int Inserted { get; set; }   // 새로 추가
        public int Updated { get; set; }    // 이미 있어서 내용만 갱신
    }
}
