using System;
using System.Text;

namespace KyungsinLPR
{
    class CodeHexa
    {
        public const byte DLE = 0x10;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;
        public const byte POWER = 0x41;
        public const byte MEMORRY = 0x4B;
        public const byte TIME = 0x47;
        public const byte SIZE = 0x40;
        public const byte BACKNUMBER = 0x4F;
        public const byte SIGNAL = 0x4E;
        public const byte CONNECTION = 0x6A;
        public const byte NOMAL = 0x4C;
        public const byte GTIME = 0x66;
        public const byte RESET = 0x4A;
        public const byte MODULE = 0x49;
        public const byte BRIGHTNESS = 0x44;
        /// <summary>
        /// 시간읽기
        /// </summary>
        /// <param name="strdata"></param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] GetTime(string strdata, string mode)
        {
            byte[] SCMD = new byte[7];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 2;

            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = GTIME;// command

            SCMD[6] = ToHexString(strdata);

            byte[] ECMD = new byte[2];
            ECMD[0] = DLE;
            ECMD[1] = ETX;

            byte[] SEND = new byte[SCMD.Length + ECMD.Length];
            Buffer.BlockCopy(SCMD, 0, SEND, 0, SCMD.Length);

            Buffer.BlockCopy(ECMD, 0, SEND, SCMD.Length, ECMD.Length);
            // Serial.SendData(SCMD, data, ECMD, name);
            return SEND;
        }

        byte Get_Brightness(string data)
        {
            string[] strdata = data.Split('%');
            byte retbyte = Convert.ToByte(strdata[0]);
            return retbyte;
        }
        /// <summary>
        /// 일반문구등록
        /// </summary>
        /// <param name="data"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public byte[] Brightness(string data, string mode)
        {
            byte[] SCMD = new byte[9];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 2;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = BRIGHTNESS;// command

            SCMD[6] = Get_Brightness(data);

            SCMD[7] = DLE;
            SCMD[8] = ETX;
            //   Serial.SendData(SCMD, name);

            return SCMD;
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        public byte[] Modul_Set(string data, string strcolor, string mode)
        {

            byte[] SCMD = new byte[13];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 7;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = MODULE;// command
            SCMD[6] = 0x00;//사용안함
            string[] temp = data.Split('-');
            byte value = Convert.ToByte(temp[0].Replace("D", ""));
            SCMD[6] = value;

            string[] temp1 = temp[1].Split('D');
            string[] temp2 = temp1[1].Split('S');
            string scanorder = temp2[1].Substring(1);
            SCMD[7] = Convert.ToByte(temp2[0]);
            SCMD[8] = Convert.ToByte(temp2[1].Substring(0, 2), 16);

            if (strcolor == "RGB") SCMD[9] = 0x01;
            if (strcolor == "RBG") SCMD[9] = 0x02;
            if (strcolor == "GRB") SCMD[9] = 0x03;
            if (strcolor == "GBR") SCMD[9] = 0x04;
            if (strcolor == "BRG") SCMD[9] = 0x05;
            if (strcolor == "BGR") SCMD[9] = 0x06;

            if (scanorder == "0") SCMD[10] = 0x02;
            else SCMD[10] = 0x01;
            SCMD[11] = DLE;
            SCMD[12] = ETX;
            //   Serial.SendData(SCMD, name);

            return SCMD;
        }
        /// <summary>
        /// 일반문구등록
        /// </summary>
        /// <param name="data"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public byte[] Nomal_Txt(string data, string mode)
        {

            byte[] SCMD = new byte[9];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 2;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = NOMAL;// command

            SCMD[6] = Convert.ToByte(data);



            SCMD[7] = DLE;
            SCMD[8] = ETX;
            //   Serial.SendData(SCMD, name);

            return SCMD;
        }
        /// <summary>
        /// 통신상태점검
        /// </summary>
        /// <param name="strdata">0123456789</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] ConnectionStatus(string strdata, string mode)
        {

            byte[] SCMD = new byte[6];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = TxtLenght(strdata) + 1;

            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = CONNECTION;// command

            byte[] data = TxtHex(strdata);

            byte[] ECMD = new byte[2];
            ECMD[0] = DLE;
            ECMD[1] = ETX;

            byte[] SEND = new byte[SCMD.Length + data.Length + ECMD.Length];
            Buffer.BlockCopy(SCMD, 0, SEND, 0, SCMD.Length);
            Buffer.BlockCopy(data, 0, SEND, SCMD.Length, data.Length);
            Buffer.BlockCopy(ECMD, 0, SEND, SCMD.Length + data.Length, ECMD.Length);
            // Serial.SendData(SCMD, data, ECMD, name);
            return SEND;
        }
        /// <summary>
        /// 외부신호
        /// </summary>
        /// <param name="signaldata1">J7 신호 값</param>
        /// <param name="signaldata2">J8 신호 값</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] SignalSet(string signaldata1, string signaldata2, string mode)
        {

            byte[] SCMD = new byte[12];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 5;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = SIGNAL;// command

            SCMD[6] = (byte)(Signal(signaldata1) & 0X00FF);
            SCMD[7] = (byte)(Signal(signaldata1) >> 8 & 0X00FF);
            SCMD[8] = (byte)(Signal(signaldata2) & 0X00FF);
            SCMD[9] = (byte)(Signal(signaldata2) >> 8 & 0X00FF);
            //  SCMD[7] = Signal(signaldata2);
            SCMD[10] = DLE;
            SCMD[11] = ETX;
            //  Serial.SendData(SCMD,name);
            return SCMD;
        }
        /// <summary>
        /// 전광파on =0x01,off =0x00  mode=0x00(싱글모드),0x01--0x1f(멀티모드)
        /// </summary>
        /// <param name="mode"></param>
        public byte[] Reset(string H, string W, string Pixel, string mode)
        {
            byte[] SCMD = new byte[11];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 4;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = RESET;//리셋 command
            SCMD[6] = BPP(Pixel);
            SCMD[7] = Convert.ToByte(H);
            SCMD[8] = Convert.ToByte(W);
            SCMD[9] = DLE;
            SCMD[10] = ETX;
            // Serial.SendData(SCMD,name);
            return SCMD;
        }
        /// <summary>
        /// 배경이미지 선택
        /// </summary>
        /// <param name="data">선택문구</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] Back_NumberSet(string data, string mode)
        {
            byte[] SCMD = new byte[9];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 2;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = BACKNUMBER;// command

            SCMD[6] = BackNumber(data);

            SCMD[7] = DLE;
            SCMD[8] = ETX;
            //Serial.SendData(SCMD, name);
            return SCMD;
        }
        public byte[] SizeSet(string W, string H, string Color, string mode)
        {
            byte[] SCMD = new byte[11];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 4;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = SIZE;// command
            SCMD[6] = ColorData(Color);
            SCMD[7] = Convert.ToByte(H);
            SCMD[8] = Convert.ToByte(W);

            SCMD[9] = DLE;
            SCMD[10] = ETX;
            // Serial.SendData(SCMD,name);
            return SCMD;
        }
        /// <summary>
        /// 사이즈설정
        /// </summary>
        /// <param name="W">width</param>
        /// <param name="H">hight</param>
        /// <param name="Color">color</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] SizeSet(string W, string H, string mode)
        {
            byte[] SCMD = new byte[10];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 3;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = SIZE;// command
            //  SCMD[6] = 0x02;// ColorData(Color);
            SCMD[6] = Convert.ToByte(H);
            SCMD[7] = Convert.ToByte(W);


            SCMD[8] = DLE;
            SCMD[9] = ETX;
            // Serial.SendData(SCMD,name);
            return SCMD;
        }
        /// <summary>
        /// 시간설정
        /// </summary>
        /// <param name="strdata">시간데이타</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] TimeSet(string strdata, string mode)
        {
            byte[] SCMD = new byte[6];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 8;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = TIME;//시간 command
            byte[] data = Time(strdata);
            byte[] ECMD = new byte[2];
            ECMD[0] = DLE;
            ECMD[1] = ETX;
            // Serial.SendData(SCMD, data, ECMD,name);
            byte[] SEND = new byte[SCMD.Length + data.Length + ECMD.Length];
            Buffer.BlockCopy(SCMD, 0, SEND, 0, SCMD.Length);
            Buffer.BlockCopy(data, 0, SEND, SCMD.Length, data.Length);
            Buffer.BlockCopy(ECMD, 0, SEND, SCMD.Length + data.Length, ECMD.Length);
            // Serial.SendData(SCMD, data, ECMD, name);
            return SEND;
        }
        /// <summary>
        /// 메모리지우기,mode=0x00(싱글모드),0x01--0x1f(멀티모드)
        /// </summary>
        /// <param name="data">메모리데이타</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        public byte[] Memorry(string data, string mode)
        {
            byte[] SCMD = new byte[9];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 2;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = MEMORRY;//메모리 command

            SCMD[6] = Memorry_Number(data);
            SCMD[7] = DLE;
            SCMD[8] = ETX;
            //   Serial.SendData(SCMD,name);
            return SCMD;
        }
        /// <summary>
        /// 전광파on =0x01,off =0x00  mode=0x00(싱글모드),0x01--0x1f(멀티모드)
        /// </summary>
        /// <param name="mode"></param>
        public byte[] Power(byte data, string mode)
        {
            byte[] SCMD = new byte[9];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/
            int Len = 2;
            SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
            SCMD[4] = Convert.ToByte(Len & 0x00ff);//

            SCMD[5] = POWER;//전원 command
            SCMD[6] = data;
            SCMD[7] = DLE;
            SCMD[8] = ETX;
            // Serial.SendData(SCMD,name);
            return SCMD;
        }

        /// <summary>
        ///  데이타 전송mode=0x00(싱글모드),0x01--0x1f(멀티모드)
        /// </summary>
        /// <param name="strdata">문구데이타</param>
        /// <param name="mode">mode=0x00(싱글모드),0x01--0x1f(멀티모드)</param>
        /// <returns></returns>
        public byte[] Data(string strdata, string mode)
        {
            string[] StrData = strdata.Split('/');
            byte[] SCMD = new byte[22];
            SCMD[0] = DLE;//프로토콜시작
            SCMD[1] = STX;//프로토콜시작
            SCMD[2] = Adress(mode);//전광판주소
            /*********************길이구하기***************************************/

            SCMD[5] = Name(StrData[0]);//긴급,일반

            SCMD[6] = Txt_Number(StrData[1]);//문구번호
            SCMD[7] = Section_Number(StrData[2]);//섹션번호
            SCMD[8] = Display_Repeat(StrData[3]);//표시제어
            SCMD[9] = Display_Method(StrData[4]);//표시방법
            SCMD[10] = Txt_Code(StrData[5]);//문자코드
            SCMD[11] = Font_Size(StrData[6]);//폰트크기
            SCMD[12] = Effect(StrData[7], StrData[8]);//입장효과
            SCMD[13] = Effect(StrData[9], StrData[10]);//퇴장효과
            SCMD[14] = SubEffect(StrData[11]);//보조효과
            SCMD[15] = Effect_Speed(StrData[12]);//효과속도
            SCMD[16] = Keep_Time(StrData[13]);//유지시간
            SCMD[17] = XYPoint(StrData[14]);//x축시작
            SCMD[18] = XYPoint(StrData[15]);//y축시작
            SCMD[19] = XYPoint(StrData[16]);//x축종료
            SCMD[20] = XYPoint(StrData[17]);//y축종료
            SCMD[21] = BackImage_Number(StrData[18]);//배경이미지
            byte[] Colordata;
            byte[] data;
            if (SCMD[10] == 0x01)//유니코드
            {
                Colordata = Color_BackTxt_Unicode(StrData[19], StrData[20], StrData[21]);
                data = TxtHex_Unicode(StrData[21]);

                int Len = (data.Length * 2) + 17;
                SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
                SCMD[4] = Convert.ToByte(Len & 0x00ff);//
            }
            else//완성형코드
            {
                Colordata = Color_BackTxt(StrData[19], StrData[20], StrData[21]);
                data = TxtHex(StrData[21]);

                int Len = (data.Length * 2) + 17;
                SCMD[3] = Convert.ToByte((Len >> 8) & 0x00ff);//command+Data 크기 상위
                SCMD[4] = Convert.ToByte(Len & 0x00ff);//
            }

            byte[] ECMD = new byte[2];
            ECMD[0] = DLE;//프로토콜끝
            ECMD[1] = ETX;//프로토콜끝

            byte[] SEND = new byte[SCMD.Length + Colordata.Length + data.Length + ECMD.Length];
            Buffer.BlockCopy(SCMD, 0, SEND, 0, SCMD.Length);
            Buffer.BlockCopy(Colordata, 0, SEND, SCMD.Length, Colordata.Length);
            Buffer.BlockCopy(data, 0, SEND, SCMD.Length + Colordata.Length, data.Length);
            Buffer.BlockCopy(ECMD, 0, SEND, SCMD.Length + Colordata.Length + data.Length, ECMD.Length);

            return SEND;
        }

        private byte BPP(string str)
        {
            byte value = 0;

            switch (str)
            {
                case "2BPP(3Color)":
                    value = 2;
                    break;
                case "3BPP(8Color)":
                    value = 3;
                    break;
                case "8BPP((256Color)":
                    value = 8;
                    break;
                case "24BPP(FullColor)":
                    value = 24;
                    break;
                default:
                    value = 3;
                    break;
            }
            return value;
        }
        /// <summary>
        /// 일반문구,긴급문구 구분
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Adress(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "DABIT 00":
                    value = 0;
                    break;
                case "DABIT 01":
                    value = 1;
                    break;
                case "DABIT 02":
                    value = 2;
                    break;
                case "DABIT 03":
                    value = 3;
                    break;
                case "DABIT 04":
                    value = 4;
                    break;
                case "DABIT 05":
                    value = 5;
                    break;
                case "DABIT 06":
                    value = 6;
                    break;
                case "DABIT 07":
                    value = 7;
                    break;
                case "DABIT 08":
                    value = 8;
                    break;
                case "DABIT 09":
                    value = 9;
                    break;
                case "DABIT 10":
                    value = 10;
                    break;
                case "DABIT 11":
                    value = 11;
                    break;
                case "DABIT 12":
                    value = 12;
                    break;
                case "DABIT 13":
                    value = 13;
                    break;
                case "DABIT 14":
                    value = 14;
                    break;
                case "DABIT 15":
                    value = 15;
                    break;
                default:
                    value = 0;
                    break;
            }
            return value;
        }
        /// <summary>
        /// 일반문구,긴급문구 구분
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Name(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "Realtime Message":
                    value = 0x94;
                    break;
                case "Page Message":
                    value = 0x95;
                    break;
                default:
                    break;
            }
            return value;
        }
        /// <summary>
        /// 문구번호
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Txt_Number(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "0":
                    value = 0x00;
                    break;
                case "1":
                    value = 0x01;
                    break;
                case "2":
                    value = 0x02;
                    break;
                case "3":
                    value = 0x03;
                    break;
                case "4":
                    value = 0x04;
                    break;

                default:
                    break;
            }
            return value;
        }
        /// <summary>
        /// 섹션번호
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Section_Number(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "0":
                    value = 0x00;
                    break;
                case "1":
                    value = 0x01;
                    break;
                case "2":
                    value = 0x02;
                    break;

                default:
                    break;
            }
            return value;
        }
        /// <summary>
        /// 표시제어
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Display_Repeat(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "OFF":
                    value = 0x00;
                    break;
                case "ON":
                    value = 0x63;
                    break;
                default:
                    value = ToHexString(str);
                    break;
            }

            return value;
        }
        /// <summary>
        /// 표시방법
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Display_Method(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "Normal":
                    value = 0x00;
                    break;
                case "Clear":
                    value = 0x01;
                    break;
                default:

                    break;
            }

            return value;
        }
        /// <summary>
        /// 문자코드
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Txt_Code(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "KSC-5601":
                    value = 0x00;
                    break;
                case "UniCode":
                    value = 0x01;
                    break;
                default:

                    break;
            }

            return value;
        }
        /// <summary>
        /// 폰트사이즈
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Font_Size(string str)
        {
            int temp = (Convert.ToInt16(str) / 4) - 1;
            return (byte)temp;
        }
        /// <summary>
        /// 입장 효과,퇴장 효과
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Effect(string str, string direction)
        {
            byte value = 0;
            switch (str)
            {
                case "Stop":
                    value = Stop_Effect(direction);
                    break;

                case "Shift":
                    value = Move_Effect(direction);

                    break;
                case "Wipe":
                    value = Wipe_Effect(direction);

                    break;
                case "Blind":
                    value = Blind_Effect(direction);

                    break;
                case "Curtain":
                    value = Curtain_Effect(direction);

                    break;
                case "ZoomOut":
                    value = Scale_In_Effect(direction);

                    break;
                case "ZoomIn":
                    value = Scale_Out_Effect(direction);

                    break;
                case "Rotate":
                    value = Rotate_Effect(direction);
                    break;
                case "Blink B.G.":
                    value = Blink_Effect(direction);
                    break;

                case "invertEffect":
                    value = Blink_Reverse_Effect(direction);

                    break;
                case "BarMove":
                    value = Module_Test_Reverse_Effect(direction);

                    break;
                case "Random":
                    value = Whole_Effect(direction);

                    break;
                default:

                    break;
            }
            return value;
        }
        /// <summary>
        /// 보조 효과
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte SubEffect(string str)//사용안함
        {
            byte value = 0;
            value = 0x00;
            return value;
        }
        /// <summary>
        /// 효과 속도
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Effect_Speed(string str)
        {
            byte value = Convert.ToByte(str);
            return value;
        }
        /// <summary>
        /// 유지시간
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Keep_Time(string str)
        {
            byte value = 0;

            switch (str)
            {
                case "2Min":
                    value = 0xF0;
                    break;
                case "3Min":
                    value = 0xF1;
                    break;
                case "5Min":
                    value = 0xF2;
                    break;
                case "10Min":
                    value = 0xF3;
                    break;
                case "30Min":
                    value = 0xF4;
                    break;
                case "1Hour":
                    value = 0xF5;
                    break;
                case "3Hour":
                    value = 0xF6;
                    break;
                case "5Hour":
                    value = 0xF7;
                    break;
                case "9Hour":
                    value = 0xF8;
                    break;
                default:
                    value = Convert.ToByte(str);
                    break;
            }
            return value;
        }
        /// <summary>
        /// X,Y축 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte XYPoint(string str)
        {
            int temp = (Convert.ToInt16(str) / 4);
            // ToHexString(temp.ToString();
            return (byte)temp;
        }
        /// <summary>
        /// 배경이미지
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte BackImage_Number(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "NotUse":
                    value = 0x00;
                    break;
                default:
                    value = ToHexString(str);
                    break;
            }
            return value;
        }
        /// <summary>
        /// 메모리
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte Memorry_Number(string str)
        {
            byte value = 0;
            switch (str)
            {
                case "All":
                    value = 0x80;
                    break;
                default:
                    value = ToHexString(str);
                    break;
            }
            return value;
        }
        /// <summary>
        /// 요일 변환
        /// </summary>
        /// <param name="strdata"></param>
        /// <returns></returns>
        private byte Day(string strdata)
        {
            byte temp = 0;
            switch (strdata)
            {
                case "일":
                    temp = 0x00;
                    break;
                case "월":
                    temp = 0x01;
                    break;
                case "화":
                    temp = 0x02;
                    break;
                case "수":
                    temp = 0x03;
                    break;
                case "목":
                    temp = 0x04;
                    break;
                case "금":
                    temp = 0x05;
                    break;
                case "토":
                    temp = 0x06;
                    break;
                default:
                    break;
            }
            return temp;
        }
        /// <summary>
        /// 시간변환
        /// </summary>
        /// <param name="strdata"></param>
        /// <returns></returns>
        private byte[] Time(string strdata)
        {
            string[] StrData = strdata.Split('-');
            byte[] temp = new byte[7];
            temp[0] = ToHexString(StrData[0]);
            temp[1] = ToHexString(StrData[1]);
            temp[2] = ToHexString(StrData[2]);
            temp[3] = Day(StrData[3]);
            temp[4] = ToHexString(StrData[4]);
            temp[5] = ToHexString(StrData[5]);
            temp[6] = ToHexString(StrData[6]);
            return temp;
        }
        /// <summary>
        /// 색상변환
        /// </summary>
        /// <returns></returns>
        private byte ColorData(string strdata)
        {
            byte temp = 0;
            switch (strdata)
            {
                case "2Bit(3Color)":
                    temp = 0x02;
                    break;
                case "24Bit(FullColor)":
                    temp = 0x18;
                    break;
                default:
                    break;
            }
            return temp;
        }
        /// <summary>
        /// 배경이미지 선택
        /// </summary>
        /// <param name="strdata"></param>
        /// <returns></returns>
        private byte BackNumber(string strdata)
        {
            byte temp = 0;
            switch (strdata)
            {
                case "NotUse":
                    temp = 0x00;
                    break;
                case "Disabled":
                    temp = 0x00;
                    break;
                default:
                    temp = Convert.ToByte(strdata);
                    break;
            }
            return temp;
        }
        private int Signal(string strdata)
        {
            int temp = 0;
            switch (strdata)
            {
                case "None":
                    temp = 0xF100;
                    break;
                case "On":
                    temp = 0xF000;
                    break;
                case "Off":
                    temp = 0x0000;
                    break;
                default:
                    temp = Convert.ToInt16(strdata);
                    break;
            }
            return temp;
        }
        #region 서브효과
        private byte Stop_Effect(string str)//정지-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "NoDir":
                    value = 0x01;
                    break;
                case "BrightON":
                    value = 0x02;
                    break;
                case "BrightOff":
                    value = 0x03;
                    break;
                case "HoriMirror":
                    value = 0x04;
                    break;
                case "VertMirror":
                    value = 0x05;
                    break;
                default:
                    break;

            }
            return value;
        }
        private byte Move_Effect(string str)//이동하기-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Left":
                    value = 0x06;
                    break;
                case "Right":
                    value = 0x07;
                    break;
                case "Up":
                    value = 0x08;
                    break;
                case "Down":
                    value = 0x09;
                    break;
                case "LeftUp":
                    value = 0x0A;
                    break;
                case "UpDown":
                    value = 0x0B;
                    break;
                default:
                    break;
            }
            return value;
        }
        private byte Wipe_Effect(string str)//닦아내기-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Left":
                    value = 0x0C;
                    break;
                case "Right":
                    value = 0x0D;
                    break;
                case "Up":
                    value = 0x0E;
                    break;
                case "Down":
                    value = 0x0F;
                    break;
                case "LeftUp":
                    value = 0x10;
                    break;
                case "RightDown":
                    value = 0x11;
                    break;
                default:
                    break;
            }
            return value;
        }
        private byte Blind_Effect(string str)//블라인드-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Left":
                    value = 0x12;
                    break;
                case "Right":
                    value = 0x13;
                    break;
                case "Up":
                    value = 0x14;
                    break;
                case "Down":
                    value = 0x15;
                    break;
                case "LeftUp":
                    value = 0x16;
                    break;
                case "RightDown":
                    value = 0x17;
                    break;
                default:
                    break;
            }
            return value;
        }
        private byte Curtain_Effect(string str)//커튼효과-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Hori.Side":
                    value = 0x18;
                    break;
                case "Hori.Center":
                    value = 0x19;
                    break;
                case "Vert.Side":
                    value = 0x1A;
                    break;
                case "Vert.Center":
                    value = 0x1B;
                    break;
                case "HoriCenter2":
                    value = 0x1C;
                    break;
                case "VertCenter2":
                    value = 0x1D;
                    break;
                default:
                    break;
            }
            return value;
        }
        private byte Scale_In_Effect(string str)//축소효과-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "LeftUp":
                    value = 0x1E;
                    break;
                case "LeftDown":
                    value = 0x1F;
                    break;
                case "RightUp":
                    value = 0x20;
                    break;
                case "RightDown":
                    value = 0x21;
                    break;
                case "Center":
                    value = 0x22;
                    break;
                default:
                    break;

            }
            return value;
        }

        private byte Scale_Out_Effect(string str)//확대효과-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "LeftUp":
                    value = 0x23;
                    break;
                case "LeftDown":
                    value = 0x24;
                    break;
                case "RightUp":
                    value = 0x25;
                    break;
                case "RightDown":
                    value = 0x26;
                    break;
                case "Center":
                    value = 0x27;
                    break;
                default:
                    break;

            }
            return value;
        }
        private byte Rotate_Effect(string str)//회전효과-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Counter":
                    value = 0x28;
                    break;
                case "Clockwise":
                    value = 0x29;
                    break;
                default:
                    break;

            }
            return value;
        }
        private byte Blink_Effect(string str)//깜빡이기-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Red":
                    value = 0x2C;
                    break;
                case "Green":
                    value = 0x2D;
                    break;
                case "Blue":
                    value = 0x2E;
                    break;
                case "White":
                    value = 0x2F;
                    break;
                case "All":
                    value = 0x30;
                    break;
                default:
                    break;

            }
            return value;
        }
        private byte Blink_Reverse_Effect(string str)//반전효과-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Red":
                    value = 0x31;
                    break;
                case "Green":
                    value = 0x32;
                    break;
                case "Blue":
                    value = 0x33;
                    break;
                case "White":
                    value = 0x34;
                    break;
                case "All":
                    value = 0x35;
                    break;
                case "All(at once)"://추가부분
                    value = 0x37;
                    break;
                default:
                    break;

            }
            return value;
        }

        private byte Module_Test_Reverse_Effect(string str)//막대바이동-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Left":
                    value = 0x6F;
                    break;
                case "Down":
                    value = 0x72;
                    break;
                case "LeftDown":
                    value = 0x76;
                    break;
                default:
                    break;

            }
            return value;
        }
        private byte Whole_Effect(string str)//전체-서브효과
        {
            byte value = 0;
            switch (str)
            {
                case "Sequential":
                    value = 0x79;
                    break;
                case "Random":
                    value = 0x7A;
                    break;
                default:
                    break;

            }
            return value;
        }
        #endregion
        public bool fnQAscii(int strS)
        {
            if (32 <= strS && strS <= 126)//32스페이스부터~까지
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool fnQAscii(string strS)
        {
            bool value = false;
            if (Encoding.Default.GetByteCount(strS) == 1)
            {
                byte[] temp = Encoding.Default.GetBytes(strS);

                if (32 <= temp[0] && temp[0] <= 126)
                {
                    value = true;
                }
                else
                {
                    value = false;
                }
            }
            else
            {
                value = false;
            }

            return value;
        }
        /// <summary>
        /// 색상설정
        /// </summary>
        /// <param name="TxtColor">문구색상</param>
        /// <param name="BackColor">배경색상</param>
        /// <param name="Txt">텍스트</param>
        /// <returns></returns>
        private byte[] Color_BackTxt(string TxtColor, string BackColor, string Txt)
        {
            byte[] tempbyte_Txt = Encoding.Default.GetBytes(Txt);

            byte[] data = new byte[tempbyte_Txt.Length];
            int TxtTemp = 0;
            int BackTemp = 0;
            int Count = -1;

            for (int i = 0; i < Txt.Length; i++)
            {
                Count++;
                if (TxtColor.Length <= i) TxtTemp = TxtColor.Length - 1;
                else TxtTemp = i;
                if (BackColor.Length <= i) BackTemp = BackColor.Length - 1;
                else BackTemp = i;
                string temp = Txt.Substring(i, 1);
                if (fnQAscii(Txt.Substring(i, 1)))
                {
                    if (TxtColor.Substring(TxtTemp, 1) == "1")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x01 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x01 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x01 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x01 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x01 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x01 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x01 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x01 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x01 | 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "2")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x02 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x02 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x02 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x02 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x02 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x02 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x02 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x02 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x02 | 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "3")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x03 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x03 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x03 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x03 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x03 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x03 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x03 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x03 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x03 | 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "4")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x04 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x04 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x04 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x04 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x04 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x04 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x04 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x04 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x04 | 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "5")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x05 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x05 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x05 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x05 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x05 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x05 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x05 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x05 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x05 | 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "6")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x06 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x06 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x06 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x06 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x06 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x06 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x06 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x06 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x06 | 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "7")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x07 | 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x07 | 0x10;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x07 | 0x20;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x07 | 0x30;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x07 | 0x40;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x07 | 0x50;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x07 | 0x60;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x07 | 0x70;
                        }
                        else
                        {
                            data[Count] = 0x07 | 0x00;
                        }
                    }
                    else
                    {

                    }
                }
                else
                {
                    if (TxtColor.Substring(TxtTemp, 1) == "1")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x01 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x01 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x01 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x01 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x01 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x01 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x01 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x01 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x01 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "2")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x02 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x02 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x02 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x02 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x02 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x02 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x02 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x02 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x02 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "3")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x03 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x03 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x03 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x03 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x03 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x03 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x03 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x03 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x03 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "4")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x04 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x04 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x04 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x04 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x04 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x04 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x04 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x04 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x04 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "5")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x05 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x05 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x05 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x05 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x05 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x05 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x05 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x05 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x05 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "6")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x06 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x06 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x06 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x06 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x06 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x06 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x06 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x06 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x06 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else if (TxtColor.Substring(TxtTemp, 1) == "7")
                    {
                        if (BackColor.Substring(BackTemp, 1) == "0")
                        {
                            data[Count] = 0x07 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "1")
                        {
                            data[Count] = 0x07 | 0x10;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "2")
                        {
                            data[Count] = 0x07 | 0x20;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "3")
                        {
                            data[Count] = 0x07 | 0x30;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "4")
                        {
                            data[Count] = 0x07 | 0x40;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "5")
                        {
                            data[Count] = 0x07 | 0x50;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "6")
                        {
                            data[Count] = 0x07 | 0x60;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else if (BackColor.Substring(BackTemp, 1) == "7")
                        {
                            data[Count] = 0x07 | 0x70;
                            Count++;
                            data[Count] = 0x00;
                        }
                        else
                        {
                            data[Count] = 0x07 | 0x00;
                            Count++;
                            data[Count] = 0x00;
                        }
                    }
                    else
                    {

                    }
                }
            }
            return data;
        }

        private byte[] Color_BackTxt_Unicode(string TxtColor, string BackColor, string Txt)
        {
            byte[] tempbyte_Txt = Encoding.Unicode.GetBytes(Txt);

            byte[] data = new byte[tempbyte_Txt.Length];
            int TxtTemp = 0;
            int BackTemp = 0;
            int Count = -1;

            for (int i = 0; i < Txt.Length; i++)
            {
                Count++;
                if (TxtColor.Length <= i) TxtTemp = TxtColor.Length - 1;
                else TxtTemp = i;
                if (BackColor.Length <= i) BackTemp = BackColor.Length - 1;
                else BackTemp = i;
                string temp = Txt.Substring(i, 1);

                if (TxtColor.Substring(TxtTemp, 1) == "1")
                {
                    if (BackColor.Substring(BackTemp, 1) == "0")
                    {
                        data[Count] = 0x01 | 0x00;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "1")
                    {
                        data[Count] = 0x01 | 0x10;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "2")
                    {
                        data[Count] = 0x01 | 0x20;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "3")
                    {
                        data[Count] = 0x01 | 0x30;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else
                    {
                        data[Count] = 0x01 | 0x00;
                        Count++;
                        data[Count] = 0x00;
                    }
                }
                else if (TxtColor.Substring(TxtTemp, 1) == "2")
                {
                    if (BackColor.Substring(BackTemp, 1) == "0")
                    {
                        data[Count] = 0x02 | 0x00;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "1")
                    {
                        data[Count] = 0x02 | 0x10;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "2")
                    {
                        data[Count] = 0x02 | 0x20;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "3")
                    {
                        data[Count] = 0x02 | 0x30;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else
                    {
                        data[Count] = 0x02 | 0x00;
                        Count++;
                        data[Count] = 0x00;
                    }
                }
                else if (TxtColor.Substring(TxtTemp, 1) == "3")
                {
                    if (BackColor.Substring(BackTemp, 1) == "0")
                    {
                        data[Count] = 0x03 | 0x00;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "1")
                    {
                        data[Count] = 0x03 | 0x10;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "2")
                    {
                        data[Count] = 0x03 | 0x20;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else if (BackColor.Substring(BackTemp, 1) == "3")
                    {
                        data[Count] = 0x03 | 0x30;
                        Count++;
                        data[Count] = 0x00;
                    }
                    else
                    {
                        data[Count] = 0x03 | 0x00;
                        Count++;
                        data[Count] = 0x00;
                    }
                }
                else
                {

                }
            }
            return data;
        }

        private byte ToHexString(string nor)//숫자를 16진수 string 변환
        {
            byte tempbyte = Convert.ToByte(nor, 16);

            return tempbyte;
        }

        public byte[] TxtHex_Unicode(string str)//유니코드
        {
            byte[] Temp = Encoding.Unicode.GetBytes(str);
            byte[] value = new byte[Temp.Length];
            for (int i = 0; i < Temp.Length; i++)
            {
                if (i % 2 == 0)//
                {
                    value[i] = Temp[i + 1];
                }
                else
                {
                    value[i] = Temp[i - 1];
                }
            }
            return value;
        }

        public byte[] TxtHex(string str)
        {
            return Encoding.Default.GetBytes(str);
        }

        public int TxtLenght(string str)
        {
            byte[] tempbyte = Encoding.Default.GetBytes(str);
            return tempbyte.Length;
        }
    }
}
