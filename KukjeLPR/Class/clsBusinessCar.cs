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
            string rtn = string.Empty;
            if (!IsBusinessCar(CarNo))
                rtn = Ment;
            else
            {
                if (DisPlayLineMent != string.Empty)
                    rtn = DisPlayLineMent;
                else
                    rtn = Ment;
            }
            return rtn;
        }
    }
}
