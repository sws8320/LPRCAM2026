using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KyungsinLPR
{
    public static class SpecialGroup
    {
        public static bool Use;
        public static int GroupIdx = 0;

        public static void LoadInfo()
        {
            GroupIdx = Util.Function.IntTryParse(Util.Function.IniReadValue("SpecialGroup", "Idx"));
            Use = GroupIdx > -1;
        }

        public static void SaveInfo()
        {
            Util.Function.IniWriteValue("SpecialGroup", "Idx", GroupIdx.ToString());
        }
    }
}
