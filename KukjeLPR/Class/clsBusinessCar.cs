using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KyungsinLPR
{
    class clsBusinessCar
    {
        public static bool UseBusinessCar;
        public static bool UseEntranceGateOpen;
        public static bool UseEntranceSocketDataSend;
        public static bool UseExitGateOpen;
        public static bool UseExitSocketDataSend;
        public static string DisPlayLineMent;
        private static string[] BusinessCarNo = new string[] { "아", "바", "사", "자", "배" };

        public static void ReadIni()
        {
            UseBusinessCar = Util.Function.BoolTryParse(Util.Function.IniReadValue("BusinessCar", "USE"));
            UseEntranceGateOpen = Util.Function.BoolTryParse(Util.Function.IniReadValue("BusinessCar", "EntranceGateOpen"));
            UseEntranceSocketDataSend = Util.Function.BoolTryParse(Util.Function.IniReadValue("BusinessCar", "EntranceSocketData"));
            UseExitGateOpen = Util.Function.BoolTryParse(Util.Function.IniReadValue("BusinessCar", "ExitGateOpen"));
            UseExitSocketDataSend = Util.Function.BoolTryParse(Util.Function.IniReadValue("BusinessCar", "ExitSocketData"));
            DisPlayLineMent = Util.Function.IniReadValue("BusinessCar", "DisPlayLineMent");
        }

        public static void SetValue(bool use, bool entOpen, bool entSend, bool outOpen, bool outSend, string ment)
        {
            UseBusinessCar = use;
            UseEntranceGateOpen = entOpen;
            UseEntranceSocketDataSend = entSend;
            UseExitGateOpen = outOpen;
            UseExitSocketDataSend = outSend;
            DisPlayLineMent = ment;
        }

        public static void SaveIni()
        {
            Util.Function.IniWriteValue("BusinessCar", "USE", UseBusinessCar.ToString());
            Util.Function.IniWriteValue("BusinessCar", "EntranceGateOpen", UseEntranceGateOpen.ToString());
            Util.Function.IniWriteValue("BusinessCar", "EntranceSocketData", UseEntranceSocketDataSend.ToString());
            Util.Function.IniWriteValue("BusinessCar", "ExitGateOpen", UseExitGateOpen.ToString());
            Util.Function.IniWriteValue("BusinessCar", "ExitSocketData", UseExitSocketDataSend.ToString());
            Util.Function.IniWriteValue("BusinessCar", "DisPlayLineMent", DisPlayLineMent);
        }

        public static bool IsBusinessCar(string CarNo)
        {
            bool rtn = false;
            foreach (string item in BusinessCarNo)
            {
                if (CarNo.IndexOf(item) > -1)
                {
                    rtn = true;
                    break;
                }
            }
            return rtn;
        }

        public static string BusinessCarMent(string CarNo, string Ment)
        {
            // 영업용 번호판 확인 사용이 꺼져있으면 일반 멘트 그대로 반환
            // (기존 버그: UseBusinessCar 체크 없이 IsBusinessCar만 봐서, 자/아/바/사/배가 포함된
            //  일반 차량까지 영업용 멘트로 표시되던 문제 — 게이트/소켓 로직은 UseBusinessCar를 체크하지만 멘트만 누락)
            if (!UseBusinessCar || !IsBusinessCar(CarNo))
                return Ment;
            return string.IsNullOrEmpty(DisPlayLineMent) ? Ment : DisPlayLineMent;
        }
    }
}
