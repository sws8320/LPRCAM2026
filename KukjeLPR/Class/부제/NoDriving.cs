using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KyungsinLPR
{
    public static class NoDriving
    {
        //사용
        public static bool Use;
        //부제 타입
        public static NoDrive Option;
        //LPR 기록
        public static bool WriteLpr;
        //전광판 출력
        public static bool DisPlay;
        //부제 예외 차량 (정기권 있으면 부제 무시)
        public static bool Exception;
        //전광판 문구1
        public static string Ment1;
        //전광판 문구1 색상
        public static string Color1;
        //전광판 문구2
        public static string Ment2;
        //전광판 문구2 색상
        public static string Color2;

        public static void Load()
        {
            Use = Util.Function.BoolTryParse(Util.Function.IniReadValue("NODRIVE", "Use"));
            Enum.TryParse(Util.Function.IniReadValue("NODRIVE", "Option"), out Option);
            WriteLpr = Util.Function.BoolTryParse(Util.Function.IniReadValue("NODRIVE", "WriteLpr"));
            DisPlay = Util.Function.BoolTryParse(Util.Function.IniReadValue("NODRIVE", "DisPlay"));
            Exception = Util.Function.BoolTryParse(Util.Function.IniReadValue("NODRIVE", "Exception"));
            Ment1 = Util.Function.IniReadValue("NODRIVE", "Ment1");
            Color1 = Util.Function.IniReadValue("NODRIVE", "Color1");
            Ment2 = Util.Function.IniReadValue("NODRIVE", "Ment2");
            Color2 = Util.Function.IniReadValue("NODRIVE", "Color2");
        }

        public static void Save()
        {
            Util.Function.IniWriteValue("NODRIVE", "Use", Use);
            Util.Function.IniWriteValue("NODRIVE", "Option", (int)Option);
            Util.Function.IniWriteValue("NODRIVE", "WriteLpr", WriteLpr);
            Util.Function.IniWriteValue("NODRIVE", "DisPlay", DisPlay);
            Util.Function.IniWriteValue("NODRIVE", "Exception", Exception);
            Util.Function.IniWriteValue("NODRIVE", "Ment1", Ment1);   // 수정: MENT1 → Ment1 통일
            Util.Function.IniWriteValue("NODRIVE", "Color1", Color1);
            Util.Function.IniWriteValue("NODRIVE", "Ment2", Ment2);
            Util.Function.IniWriteValue("NODRIVE", "Color2", Color2);
        }

        public static bool Check(string CarNo)
        {
            if (!Use) return false;

            if (CarNo.Length <= 5 || CarNo == "No_Detection") return false;

            // 요일제(토.일 제외) 처리
            if (Option == NoDrive.TypeDayOfWeek)
            {
                DayOfWeek today = DateTime.Now.DayOfWeek;

                // 토요일, 일요일은 부제 미적용
                if (today == DayOfWeek.Saturday || today == DayOfWeek.Sunday)
                    return false;

                // 차번 마지막 숫자 추출
                int lastNo = -1;
                if (!int.TryParse(CarNo.Substring(CarNo.Length - 1, 1), out lastNo))
                    return false;

                // 요일별 제한 끝자리:
                // 월(1): 1,6 / 화(2): 2,7 / 수(3): 3,8 / 목(4): 4,9 / 금(5): 0,5
                // 공식: lastNo % 5 == (int)today % 5
                return (lastNo % 5) == ((int)today % 5);
            }

            // 2부제 / 5부제 / 10부제 처리
            int lastDigit = -1;
            if (!int.TryParse(CarNo.Substring(CarNo.Length - 1, 1), out lastDigit))
                return false;

            int mod = DateTime.Now.Day % (int)Option;

            // 5부제, 10부제
            if ((int)Option != 2)
            {
                return mod == lastDigit % (int)Option;
            }
            // 2부제 (홀짝 반대로 적용)
            else
            {
                return mod != lastDigit % (int)Option;
            }
        }
    }

    public enum NoDrive
    {
        Type2 = 2,
        Type5 = 5,
        Type10 = 10,
        TypeDayOfWeek = 7   // 요일제(토.일 제외)
    }
}
