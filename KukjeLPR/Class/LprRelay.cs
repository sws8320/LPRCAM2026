using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KyungsinLPR
{
    public static class LprRelay
    {
        private static bool Use = false;
        private static int Port = 0;
        private static string Ip = "127.0.0.1";
        private static string Type = "";
        public static bool USE
        {
            get { return Use; }
        }

        public static int PORT
        {
            get { return Port; }
        }

        public static string IP
        {
            get { return Ip; }
        }

        public static string TYPE
        {
            get { return Type; }
        }

        public static void Save_Ini(bool _Use , int _Port, string _Ip, string _Type)
        {
            Use = _Use;
            Port = _Port;
            Ip = _Ip;
            Type = _Type;

            Util.Function.IniWriteValue("LPR RELAY", "USE", Use);
            Util.Function.IniWriteValue("LPR RELAY", "PORT", Port);
            Util.Function.IniWriteValue("LPR RELAY", "IP", Ip);
            Util.Function.IniWriteValue("LPR RELAY", "TYPE", Type);
        }

        public static void Read_Ini()
        {
            Use = Util.Function.BoolTryParse(Util.Function.IniReadValue("LPR RELAY", "USE"));
            Port = Util.Function.IntTryParse(Util.Function.IniReadValue("LPR RELAY", "PORT"));
            Ip = Util.Function.IniReadValue("LPR RELAY", "IP");
            Type = Util.Function.IniReadValue("LPR RELAY", "TYPE");
        }
    }
}
