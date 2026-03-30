using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace KyungsinLPR
{
    public static class clsLPR
    {
        public static List<LprInfo> LPR = new List<LprInfo>();

        public static void Read()
        {
            if (File.Exists("data.txt"))
            {
                LprInfo item = new LprInfo();
                BinaryFormatter serializer = new BinaryFormatter();
                Stream stream = File.Open("data.txt", FileMode.Create);
                try
                {
                    while (true)
                    {
                        serializer.Serialize(stream, item);
                        LPR.Add(item);
                    }
                }
                catch { }
                stream.Close();
            }
        }

        public static void Write()
        {
            BinaryFormatter serializer = new BinaryFormatter();
            Stream stream = File.Open("data.txt", FileMode.Create);
            foreach (LprInfo item in LPR)
            {
                serializer.Serialize(stream, item);
            }
            stream.Close();
        }
    }

    [Serializable]
    public class LprInfo
    {
        public int No;
        public string ChNo;
        public string IP;
        public int Port;
        public string strType;
    }
}
