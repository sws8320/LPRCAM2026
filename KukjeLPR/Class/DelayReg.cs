//20161124 이미지 저장 시 누락 되는 경우 발생 인식 이미지는 현재 경로 저장 이미지는 Back 이미지 사용
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KyungsinLPR
{
    public static class DelayReg
    {
        /// <summary>
        /// AS-IS     : 2번 LPR 1초 지연 처리
        /// TO-BE     : 2번 카메라 지연(ms) 설정 
        ///           : 동일 장소 촬영 옵션 중복 처리 여부 
        ///           : 중복 처리 간격
        ///           : 마지막 처리 차량 번호
        ///           : 마지막 처리 시각
        ///           : APB (Anti Pass Back) 기능 추가
        /// 기능 추가 : 20170522
        /// 작성      : 한상훈
        /// TO-BE     : 1대만 중복 체크하니 후면 촬영시 중복 체크 되지 않아 2개로 apb 제한 차량 댓수 늘림
        /// 기능 추가 : 2017-10-17
        /// </summary>
        private static bool _Delay = false;
        private static int _Delay_Term = 0;

        private static bool _Duplicate = false;
        private static int _Duplicate_Term = 0;

        private static string _Apb_CarNo = "";
        private static DateTime _Apb_Time = new DateTime();
        // 기능 추가 : 20170522
        private static List<check> checkList = new List<check>();
        private static int count = 2;

        public static bool Delay
        {
            get { return _Delay; }
            set { _Delay = value; }
        }

        public static int DelayTerm
        {
            get
            {
                if (_Delay && _Delay_Term == 0)
                    _Delay_Term = 1000;
                return _Delay_Term;
            }
            set { _Delay_Term = value; }
        }

        public static bool Duplicate
        {
            get { return _Duplicate; }
            set { _Duplicate = value; }
        }

        public static int Duplicate_Term
        {
            get
            {
                if (_Duplicate && _Duplicate_Term == 0)
                    _Duplicate_Term = 1;
                return _Duplicate_Term;
            }
            set { _Duplicate_Term = value; }
        }

        /// <summary>
        /// APB 옵션이 True 일때 동작
        /// 마지막 입력 차량 번호, 시각과 현재 차량 번호를 비교 하여 결과 리턴
        /// </summary>
        /// <param name="CarNo">차량 번호</param>
        /// <returns>True  : Not APB </returns>
        /// <returns>False : APB     </returns>
        public static bool CheckAPB(string CarNo)
        {
            //bool rtn = true;
            //if (_Duplicate)
            //{
            //    if (CarNo != _Apb_CarNo)
            //        rtn = true;
            //    else
            //    {
            //        if ((DateTime.Now - _Apb_Time).TotalSeconds > _Duplicate_Term)
            //            rtn = true;
            //        else
            //            rtn = false;
            //    }
            //    _Apb_CarNo = CarNo;
            //    _Apb_Time = DateTime.Now;
            //}
            return CheckCarno(CarNo);
        }

        public static void SaveDelay()
        {
            Util.Function.IniWriteValue("REGDELAYCH2", "USE", _Delay);
            Util.Function.IniWriteValue("REGDELAYCH2", "TERM", _Delay_Term);
            Util.Function.IniWriteValue("REGDELAYCH2", "USEAPB", _Duplicate);
            Util.Function.IniWriteValue("REGDELAYCH2", "APBTERM", _Duplicate_Term);
        }

        public static void ReadDelay()
        {
            _Delay = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGDELAYCH2", "USE"));
            _Delay_Term = Util.Function.IntTryParse(Util.Function.IniReadValue("REGDELAYCH2", "Term"));
            _Duplicate = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGDELAYCH2", "USEAPB"));
            _Duplicate_Term = Util.Function.IntTryParse(Util.Function.IniReadValue("REGDELAYCH2", "APBTERM"));
        }

        /// <returns>True  : Not APB </returns>
        /// <returns>False : APB     </returns>
        private static bool CheckCarno(string CarNo)
        {
            check item = new check();
            bool rtn = false;
            int idx = checkList.FindIndex(x => x.checkCarno == CarNo);

            if (idx >= 0)
            {
                if ((DateTime.Now - checkList[idx].checkTime).TotalSeconds > _Duplicate_Term)
                    rtn = true;
            }
            else if (idx == -1)
                rtn = true;
            else if (checkList.Count == 0)
            {
                rtn = true;
            }
            item.checkCarno = CarNo;
            item.checkTime = DateTime.Now;
            for (int i = checkList.Count - 1; i >= 0 ; i--)
            {
                if (checkList[i].checkCarno == CarNo)
                    checkList.RemoveAt(i);
            }
            checkList.Add(item);
            if (checkList.Count > count)
                checkList.RemoveAt(0);
            return rtn;
        }
    }

    internal class check
    {
        public string checkCarno;
        public DateTime checkTime;
    }
}
