using SDKNetLib;
using SDKNetLib.Impl;
using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace KyungsinLPR
{
    public class NetworkDisplay
    {
        static CodeHexa _CodeHexa = new CodeHexa();
        private IAsyncSocketClient client;
        public string Ip;
        public int Port;
        private bool TCPOpen = false;
        //private DisplayMent dp = new DisplayMent();
        private byte[] _recieveBuffer = new byte[8142];
        public DateTime SendTime = new DateTime();
        public DateTime RecvTime = new DateTime();
        public bool Entrance_Type;
        public string Tag = "";              // 전광판 식별(카드1/카드2/카드3...) — 송신 로그용
        private string _lastLogSig = null;   // 직전 송신내용(중복 로그 억제: 매초 환영문구 재전송 스팸 방지)

        public delegate void SendDelegate(); //델리게이트 선언
        public event SendDelegate SendDel; //델리게이트 이벤트 선언
        public delegate void RecvDelegate(); //델리게이트 선언
        public event RecvDelegate RecvDel; //델리게이트 이벤트 선언
        public delegate void ErrorDelegate(); //델리게이트 선언
        public event ErrorDelegate ErrorDel; //델리게이트 이벤트 선언
        public delegate void ConnectDelegate(); //델리게이트 선언
        public event ConnectDelegate ConDel; //델리게이트 이벤트 선언
        public delegate void CloseDelegate(); //델리게이트 선언
        public event CloseDelegate CloseDel; //델리게이트 이벤트 선언

        private bool blRcv = false;
        private string currentMent1;
        private string currentMent2;
        private int currentColor1;
        private int currentColor2;

        private const byte DLE = 0x10;
        private const byte STX = 0x02;
        private const byte DST = 0x00;
        private const byte ETX = 0x03;
        private byte[] header = new byte[] { DLE, STX, DST };
        private byte[] tail = new byte[] { DLE, ETX };
        public byte CharCode = 0x00;
        
        //                                             긴급문구94   섹션번호      표시방법     폰트크기      퇴장효과     효과속도      X축시작점    X축종료점     배경이미지
        //                                             일반문구95         표시제어      문자코드     입장효과      보조효과     유지시간      Y축시작점     Y축종료점
        //                                                   페이지번호
        private byte[] Line1Stay12Byte  = new byte[] { 0x94, 0x00, 0x00, 0x63, 0x01, 0x00, 0x03, 0x01, 0x00, 0x00, 0x14, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 };
        private byte[] Line1Stay14Byte  = new byte[] { 0x94, 0x00, 0x00, 0x63, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x14, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 };
        private byte[] Line1Move        = new byte[] { 0x94, 0x00, 0x00, 0x63, 0x01, 0x00, 0x03, 0x06, 0x06, 0x00, 0x1E, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 };
        private byte[] Line2Stay12Byte  = new byte[] { 0x94, 0x00, 0x01, 0x63, 0x01, 0x00, 0x03, 0x01, 0x00, 0x00, 0x14, 0x04, 0x00, 0x04, 0x00, 0x00, 0x00 };
        private byte[] Line2Stay14Byte  = new byte[] { 0x94, 0x00, 0x01, 0x63, 0x01, 0x00, 0x02, 0x01, 0x00, 0x00, 0x14, 0x04, 0x00, 0x04, 0x00, 0x00, 0x00 };
        private byte[] Line2Move        = new byte[] { 0x94, 0x00, 0x01, 0x63, 0x01, 0x00, 0x03, 0x06, 0x06, 0x00, 0x1E, 0x04, 0x00, 0x04, 0x00, 0x00, 0x00 };
        private byte[] Timer = new byte[] { 0x00, 0x00, 0x02, 0x02, 0x00, 0x03, 0x03, 0x07, 0x03, 0x03, 0x20, 0x20, 0x12, 0x00, 0x20, 0x08, 0x00, 0x3A, 0x10, 0x00 };
        private byte[] DisplayStop = new byte[] { DLE, STX, DST, 0x00, 0x02, 0x45, 0x00, DLE, ETX };
        private byte[] DisplayStart = new byte[] { DLE, STX, DST, 0x00, 0x02, 0x45, 0x01, DLE, ETX };
        private byte[] RegMent = new byte[] { DLE, STX, DST, 0x00, 0x02, 0x4C, 0x01, DLE, ETX };
        private byte[] MentClear = new byte[] { DLE, STX, DST, 0x00, 0x02, 0x4B, 0x80, DLE, ETX };
        private byte[] bPowerOn = new byte[] { DLE, STX, DST, 0x00, 0x02, 0x41, 0x01, DLE, ETX };

        public void Init(string _ip, int _port, string Type)
        {
            Ip = _ip;
            Port = _port;
            client = new DefaultAsyncSocketClient();
            Line1Stay12Byte[5] = CharCode;
            Line1Stay14Byte[5] = CharCode;
            Line1Move[5] = CharCode;
            Line2Stay12Byte[5] = CharCode;
            Line2Stay14Byte[5] = CharCode;
            Line2Move[5] = CharCode;
            client.OnConnect += new SDKNetLib.Event.AsyncSocketConnectEventHandler(client_OnConnect);
            client.OnReceive += new SDKNetLib.Event.AsyncSocketReceiveEventHandler(client_OnReceive);
            client.OnSend += new SDKNetLib.Event.AsyncSocketSendEventHandler(client_OnSend);
            client.OnClose += new SDKNetLib.Event.AsyncSocketCloseEventHandler(client_OnClose);
            client.OnError += new SDKNetLib.Event.AsyncSocketErrorEventHandler(client_OnError);

            if (Type == "TCP")
                SocketOpen();
            else if (Type == "UDP")
                UdpOpen();
        }

        public void UdpClose()
        {
            client.Close();
        }

        public void UdpOpen()
        {
            client.Connect(Ip, Port, "udp");
        }

        void client_OnError(object sender, SDKNetLib.Event.AsyncSocketErrorEventArgs e)
        {
            try
            {
                ErrorDel?.Invoke();
                if (Ping())
                {
                    SocketClose();
                    SocketOpen();
                }
            }
            catch (Exception ex)
            {
                // 비동기 콜백에서 예외가 unhandled로 빠지면 프로세스 종료 — 반드시 삼킴
                Util.Logger.Log(string.Format("[NetworkDisplay.OnError] IP={0} Port={1} {2}", Ip, Port, ex.ToString()));
            }
        }

        void client_OnClose(object sender, SDKNetLib.Event.AsyncSocketConnectionEventArgs e)
        {
            CloseDel?.Invoke();
        }

        void client_OnSend(object sender, SDKNetLib.Event.AsyncSocketSendEventArgs e)
        {
            blRcv = false;
            SendTime = DateTime.Now;
            SendDel?.Invoke();
        }

        void client_OnReceive(object sender, SDKNetLib.Event.AsyncSocketReceiveEventArgs e)
        {
            blRcv = true;
            RecvTime = DateTime.Now;
            RecvDel?.Invoke();
        }

        void client_OnConnect(object sender, SDKNetLib.Event.AsyncSocketConnectionEventArgs e)
        {
            ConDel?.Invoke();
            PowerOn();
        }

        public void SocketClose()
        {
            client.Close();
        }

        public void SocketOpen()
        {
            if (string.IsNullOrWhiteSpace(Ip) || Port <= 0 || Port > 65535)
            {
                Util.Logger.Log(string.Format("[NetworkDisplay.SocketOpen] 잘못된 IP/Port 무시 — IP='{0}' Port={1}", Ip, Port));
                return;
            }
            try
            {
                client.Close();
                client.Connect(Ip, (short)Port);
            }
            catch (Exception ex)
            {
                Util.Logger.Log(string.Format("[NetworkDisplay.SocketOpen] IP={0} Port={1} 연결 실패: {2}", Ip, Port, ex.ToString()));
            }
        }

        private string ToHexString(byte[] nor, int Size)
        {
            string hexString = "";
            for (int i = 0; i < Size; i++)
            {
                hexString += nor[i].ToString("X2") + " ";
            }
            return hexString;
        }

        public void LogMessage(string message)
        {
            //if (this.InvokeRequired)
            //{
            //    this.Invoke(new Action<string>(LogMessage), message);
            //}
            //else
            //{
            //    text_Log.AppendText("[" + DateTime.Now.ToLocalTime().ToString() + "]     " + message + "\n");
            //    text_Log.ScrollToCaret();
            //}
            Console.WriteLine(message);
        }

        public void SendMsg(string Ment1, int Color1, string Ment2, int Color2)
        {
            // 어느 카드가 어느 보드에 무엇을 보내는지 기록. 내용 동일하면 생략(매초 환영문구 재전송 스팸 방지).
            try
            {
                string sig = Ment1 + "|" + Ment2 + "|" + Color1 + "|" + Color2;
                if (sig != _lastLogSig)
                {
                    _lastLogSig = sig;
                    Util.Logger.Log(string.Format("[전광판송신] {0} {1}:{2}  1열='{3}'(색{4}) 2열='{5}'(색{6})",
                        string.IsNullOrEmpty(Tag) ? "?" : Tag, Ip, Port, Ment1, Color1, Ment2, Color2));
                }
            }
            catch { }
            SendDisplay(Ment1, Ment2, Color1, Color2);
            currentMent1 = Ment1;
            currentMent2 = Ment2;
            currentColor1 = Color1;
            currentColor2 = Color2;
            return;
            //Realtime Message/0/0/ON/Clear/KSC-5601/16/Shift/Left/Stop/NoDir/NotUse/20/10/0/0/0/0/NotUse/112233/0/Text Message
            //Realtime Message/0/0/ON/Clear/KSC-5601/16/Stop/NoDir/Stop/NoDir/NotUse/20/10/0/0/0/0/NotUse/112233/0/Text Message

            //Realtime Message/0/1/ON/Clear/KSC-5601/16/Shift/Left/Stop/NoDir/NotUse/20/10/0/16/0/0/NotUse/112233/0/Urgent Message
            //Realtime Message/0/1/ON/Clear/KSC-5601/16/Stop/NoDir/Stop/NoDir/NotUse/20/10/0/16/0/0/NotUse/112233/0/Urgent Message
            currentMent1 = Ment1;
            currentMent2 = Ment2;
            currentColor1 = Color1;
            currentColor2 = Color2;
            Util.Logger.Log(string.Format("전광판 출력 {0} {1} {2} {3}", Ment1, Ment2, Color1, Color2));
            byte[][] msgs = new byte[2][];
            string data = "";
            if (Encoding.Default.GetByteCount(Ment1) < 13)
                data = string.Format("Realtime Message/0/0/ON/Clear/KSC-5601/16/Stop/NoDir/Stop/NoDir/NotUse/20/10/0/0/0/0/NotUse/{0}/0/{1}", Color1, Ment1);
            else
                data = string.Format("Realtime Message/0/0/ON/Clear/KSC-5601/16/Shift/Left/Stop/NoDir/NotUse/20/10/0/0/0/0/NotUse/{0}/0/{1}", Color1, Ment1);
            byte[] msg = _CodeHexa.Data(data, "DABIT 00");
            msgs[0] = msg;

            if (Encoding.Default.GetByteCount(Ment2) < 13)
                data = string.Format("Realtime Message/0/1/ON/Clear/KSC-5601/16/Stop/NoDir/Stop/NoDir/NotUse/20/10/0/16/0/0/NotUse/{0}/0/{1}", Color2, Ment2);
            else
                data = string.Format("Realtime Message/0/1/ON/Clear/KSC-5601/16/Shift/Left/Stop/NoDir/NotUse/20/10/0/16/0/0/NotUse/{0}/0/{1}", Color2, Ment2);
            msg = _CodeHexa.Data(data, "DABIT 00");
            msgs[1] = msg;

            int len = Encoding.Default.GetByteCount(Ment1);
            for (int i = len; i < 12; i++)
            {
                Ment1 += " ";
            }

            data = string.Format("![000/C{0}{1}/C{2}{3}!]", Color1, Ment1, Color2, Ment2);
            Console.WriteLine(BitConverter.ToString(Encoding.Default.GetBytes(data)));
            Console.WriteLine(BitConverter.ToString(Encoding.Unicode.GetBytes(data)));
            Console.WriteLine(BitConverter.ToString(Encoding.Unicode.GetBytes("주차장")));
            Console.WriteLine(BitConverter.ToString(Encoding.Unicode.GetBytes("2 พน5678")));
            Send(Encoding.Default.GetBytes(data));
            //int cnt = 0;
            //foreach (byte[] item in msgs)
            //{
            //    Send(item);
            //    if (cnt == 0)
            //        Console.WriteLine(Ment1);
            //    else
            //        Console.WriteLine(Ment2);
            //    cnt++;
            //}
            if (Ment1.Trim() != "주차요금")
                DisPlayTime = DateTime.Now;
            else
                DisPlayTime = default(DateTime);
        }

        public void Send(byte[] msg)
        {
            if (client != null)
            {
                client.Send(msg);
                Wait();
            }
        }

        public void Send(byte[][] msg)
        {
            if (client != null)
            {
                foreach (byte[] item in msg)
                {
                    client.Send(item);
                    Wait();
                }
            }
        }

        public void DisplayDateTime()
        {
            byte[] msg;
            //Display Stop
            msg = new byte[9];
            msg[0] = 0x10;
            msg[1] = 0x2;
            msg[2] = 0x0;
            msg[3] = 0x0;
            msg[4] = 0x2;
            msg[5] = 0x45;
            msg[6] = 0x0;
            msg[7] = 0x10;
            msg[8] = 0x3;

            Send(msg);
            Thread.Sleep(100);
            //Information Message Format
            msg = new byte[261];
            msg[0] = 0x10; msg[1] = 0x2; msg[2] = 0x0; msg[3] = 0x0; msg[4] = 0xFE; msg[5] = 0x96; msg[6] = 0x3; msg[7] = 0x0; msg[8] = 0x0; msg[9] = 0x1; msg[10] = 0x10;
            msg[11] = 0x3; msg[12] = 0x0A; msg[13] = 0x1; msg[14] = 0x12; msg[15] = 0x3; msg[16] = 0x0B; msg[17] = 0x1; msg[18] = 0x13; msg[19] = 0x3; msg[20] = 0x0C;
            msg[21] = 0x3; msg[22] = 0x0; msg[23] = 0x4; msg[24] = 0x2; msg[25] = 0x18; msg[26] = 0x7; msg[27] = 0x15; msg[28] = 0x2; msg[29] = 0x3A; msg[30] = 0x7;
            msg[31] = 0x16; msg[32] = 0x2; msg[33] = 0x3A; msg[34] = 0x7; msg[35] = 0x17; msg[36] = 0x0; msg[37] = 0x0; msg[38] = 0x0; msg[39] = 0x0; msg[40] = 0x0;
            msg[41] = 0x0; msg[42] = 0x0; msg[43] = 0x0; msg[44] = 0x0; msg[45] = 0x0; msg[46] = 0x0; msg[47] = 0x0; msg[48] = 0x0; msg[49] = 0x0; msg[50] = 0x0;
            msg[51] = 0x0; msg[52] = 0x0; msg[53] = 0x0; msg[54] = 0x0; msg[55] = 0x0; msg[56] = 0x0; msg[57] = 0x0; msg[58] = 0x0; msg[59] = 0x0; msg[60] = 0x0;
            msg[61] = 0x0; msg[62] = 0x0; msg[63] = 0x0; msg[64] = 0x0; msg[65] = 0x0; msg[66] = 0x0; msg[67] = 0x0; msg[68] = 0x0; msg[69] = 0x0; msg[70] = 0x0;
            msg[71] = 0x0; msg[72] = 0x0; msg[73] = 0x0; msg[74] = 0x0; msg[75] = 0x0; msg[76] = 0x0; msg[77] = 0x0; msg[78] = 0x0; msg[79] = 0x0; msg[80] = 0x0;
            msg[81] = 0x0; msg[82] = 0x0; msg[83] = 0x0; msg[84] = 0x0; msg[85] = 0x0; msg[86] = 0x0; msg[87] = 0x0; msg[88] = 0x0; msg[89] = 0x0; msg[90] = 0x0;
            msg[91] = 0x0; msg[92] = 0x0; msg[93] = 0x0; msg[94] = 0x0; msg[95] = 0x0; msg[96] = 0x0; msg[97] = 0x0; msg[98] = 0x0; msg[99] = 0x0; msg[100] = 0x0;
            msg[101] = 0x0; msg[102] = 0x0; msg[103] = 0x0; msg[104] = 0x0; msg[105] = 0x0; msg[106] = 0x0; msg[107] = 0x0; msg[108] = 0x0; msg[109] = 0x0; msg[110] = 0x0;
            msg[111] = 0x0; msg[112] = 0x0; msg[113] = 0x0; msg[114] = 0x0; msg[115] = 0x0; msg[116] = 0x0; msg[117] = 0x0; msg[118] = 0x0; msg[119] = 0x0; msg[120] = 0x0;
            msg[121] = 0x0; msg[122] = 0x0; msg[123] = 0x0; msg[124] = 0x0; msg[125] = 0x0; msg[126] = 0x3; msg[127] = 0x0A; msg[128] = 0x0; msg[129] = 0x1; msg[130] = 0x23;
            msg[131] = 0x2; msg[132] = 0x26; msg[133] = 0x0; msg[134] = 0x0; msg[135] = 0x3; msg[136] = 0x2D; msg[137] = 0x1; msg[138] = 0x13; msg[139] = 0x0; msg[140] = 0x0;
            msg[141] = 0x3; msg[142] = 0x0C; msg[143] = 0x4; msg[144] = 0x1; msg[145] = 0x25; msg[146] = 0x2; msg[147] = 0x27; msg[148] = 0x0; msg[149] = 0x16; msg[150] = 0x3;
            msg[151] = 0x1F; msg[152] = 0x2; msg[153] = 0x17; msg[154] = 0x0; msg[155] = 0x0; msg[156] = 0x0; msg[157] = 0x0; msg[158] = 0x0; msg[159] = 0x0; msg[160] = 0x0;
            msg[161] = 0x0; msg[162] = 0x0; msg[163] = 0x0; msg[164] = 0x0; msg[165] = 0x0; msg[166] = 0x0; msg[167] = 0x0; msg[168] = 0x0; msg[169] = 0x0; msg[170] = 0x0;
            msg[171] = 0x0; msg[172] = 0x0; msg[173] = 0x0; msg[174] = 0x0; msg[175] = 0x0; msg[176] = 0x0; msg[177] = 0x0; msg[178] = 0x0; msg[179] = 0x0; msg[180] = 0x0;
            msg[181] = 0x0; msg[182] = 0x0; msg[183] = 0x0; msg[184] = 0x0; msg[185] = 0x0; msg[186] = 0x0; msg[187] = 0x20; msg[188] = 0x0; msg[189] = 0x36; msg[190] = 0x0;
            msg[191] = 0x78; msg[192] = 0x0; msg[193] = 0x36; msg[194] = 0x0; msg[195] = 0x2D; msg[196] = 0x0; msg[197] = 0x30; msg[198] = 0x0; msg[199] = 0x38; msg[200] = 0x0;
            msg[201] = 0x42; msg[202] = 0x0; msg[203] = 0x2D; msg[204] = 0x0; msg[205] = 0x4E; msg[206] = 0x0; msg[207] = 0x31; msg[208] = 0x0; msg[209] = 0x46; msg[210] = 0x0;
            msg[211] = 0x2D; msg[212] = 0x0; msg[213] = 0x43; msg[214] = 0x0; msg[215] = 0x6C; msg[216] = 0x0; msg[217] = 0x6F; msg[218] = 0x0; msg[219] = 0x63; msg[220] = 0x0;
            msg[221] = 0x6B; msg[222] = 0x0; msg[223] = 0x2D; msg[224] = 0x0; msg[225] = 0x33; msg[226] = 0x0; msg[227] = 0x30; msg[228] = 0x0; msg[229] = 0x2E; msg[230] = 0x0;
            msg[231] = 0x61; msg[232] = 0x0; msg[233] = 0x6E; msg[234] = 0x0; msg[235] = 0x69; msg[236] = 0x0; msg[237] = 0x0; msg[238] = 0x0; msg[239] = 0x0; msg[240] = 0x0;
            msg[241] = 0x0; msg[242] = 0x0; msg[243] = 0x0; msg[244] = 0x0; msg[245] = 0x0; msg[246] = 0x0; msg[247] = 0x0; msg[248] = 0x0; msg[249] = 0x0; msg[250] = 0x0;
            msg[251] = 0x0; msg[252] = 0x0; msg[253] = 0x0; msg[254] = 0x0; msg[255] = 0x0; msg[256] = 0x0; msg[257] = 0x0; msg[258] = 0x0; msg[259] = 0x10; msg[260] = 0x3;

            Thread.Sleep(100);
            Send(msg);
            //D-Day Setting
            msg = new byte[11];
            msg[0] = 0x10;
            msg[1] = 0x2;
            msg[2] = 0x0;
            msg[3] = 0x0;
            msg[4] = 0x4;
            msg[5] = 0x43;
            msg[6] = 0x10;
            msg[7] = 0x0B;
            msg[8] = 0x11;
            msg[9] = 0x10;
            msg[10] = 0x3;

            Thread.Sleep(100);
            Send(msg);
            //Block Playlist(정보문구1)
            msg = new byte[58];
            msg[0] = 0x10; msg[1] = 0x2; msg[2] = 0x0; msg[3] = 0x0; msg[4] = 0x33; msg[5] = 0x91; msg[6] = 0x1; msg[7] = 0x0; msg[8] = 0x0; msg[9] = 0x0; msg[10] = 0x0;
            msg[11] = 0x0; msg[12] = 0x0; msg[13] = 0x0; msg[14] = 0x0; msg[15] = 0x0; msg[16] = 0x0; msg[17] = 0x1; msg[18] = 0x1; msg[19] = 0x0; msg[20] = 0x0;
            msg[21] = 0x0; msg[22] = 0x0; msg[23] = 0x0; msg[24] = 0x0; msg[25] = 0x0; msg[26] = 0x0; msg[27] = 0x10; msg[28] = 0x0; msg[29] = 0x0; msg[30] = 0x1;
            msg[31] = 0x0; msg[32] = 0x1; msg[33] = 0x32; msg[34] = 0x32; msg[35] = 0x32; msg[36] = 0x0A; msg[37] = 0x0; msg[38] = 0x0; msg[39] = 0x0; msg[40] = 0x0;
            msg[41] = 0x0; msg[42] = 0x0; msg[43] = 0x0; msg[44] = 0x0; msg[45] = 0x0; msg[46] = 0x0; msg[47] = 0x0; msg[48] = 0x0; msg[49] = 0x0; msg[50] = 0x0;
            msg[51] = 0x0; msg[52] = 0x0; msg[53] = 0x0; msg[54] = 0x0; msg[55] = 0x0; msg[56] = 0x10; msg[57] = 0x3;

            Thread.Sleep(100);
            Send(msg);

            //DisPlay Start
            msg = new byte[9];
            msg[0] = 0x10;
            msg[1] = 0x2;
            msg[2] = 0x0;
            msg[3] = 0x0;
            msg[4] = 0x2;
            msg[5] = 0x45;
            msg[6] = 0x1;
            msg[7] = 0x10;
            msg[8] = 0x3;

            Thread.Sleep(100);
            Send(msg);
        }

        public void SyncTime()
        {
            byte[] msg = new byte[15];
            msg[0] = 0x10;
            msg[1] = 0x2;
            msg[2] = 0x0;
            msg[3] = 0x0;
            msg[4] = 0x8;
            msg[5] = 0x47;
            msg[6] = (byte)(DateTime.Now.Year % 100);
            msg[7] = (byte)DateTime.Now.Month;
            msg[8] = (byte)DateTime.Now.Day;
            msg[9] = (byte)(int)DateTime.Now.DayOfWeek;
            msg[10] = (byte)DateTime.Now.Hour;
            msg[11] = (byte)DateTime.Now.Minute;
            msg[12] = (byte)DateTime.Now.Second;
            msg[13] = 0x10;
            msg[14] = 0x3;

            Send(msg);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Br">% string</param>
        public void Brightness(string Br)
        {
            Send(_CodeHexa.Brightness(Br, "3"));
            Wait();
        }

        public void PowerOn()
        {
            Send(_CodeHexa.Power(0x01, "DABIT 00"));
        }

        public void PowerOff()
        {
            Send(_CodeHexa.Power(0x00, "DABIT 00"));

        }

        private bool Ping()
        {
            if (string.IsNullOrWhiteSpace(Ip)) return false;
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(Ip, 100);
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                Util.Logger.Log(string.Format("[NetworkDisplay.Ping] IP={0} 실패: {1}", Ip, ex.Message));
                return false;
            }
        }

        private void Wait()
        {
            DateTime dateTime = DateTime.Now;
            while (!blRcv)
            {
                Thread.Sleep(10);
                if ((DateTime.Now - dateTime).TotalMilliseconds > 200)
                    break;
            }
        }

        public string Ment1 = string.Empty;
        public string Ment2 = string.Empty;
        public int Color1 = 0;
        public int Color2 = 0;
        public int Term = 5;
        public DateTime DisPlayTime = DateTime.Now.AddSeconds(-60);
        public bool isFull = false;
        public byte[][] FullMent;
        public Thread t;
        public bool full;
        public void ReturnStart()
        {
            t = new Thread(new ThreadStart(NormalDisPlay));
            t.IsBackground = true;
            t.Start();
        }

        private void NormalDisPlay()
        {
            DateTime interval = DateTime.Now;

            while (true)
            {
                try
                {
                    if (Entrance_Type && isFull != full || FullSpaceControl.ForceFull)
                        DisPlayTime = DateTime.Now.AddSeconds(-60);
                    if (DisPlayTime > default(DateTime))
                    {
                        TimeSpan diff = DateTime.Now - DisPlayTime;
                        Console.WriteLine(string.Format("DisPlayTime : {0} Term {1}", DisPlayTime, diff.TotalSeconds));
                        if (diff.TotalSeconds > Term)
                        {
                            //Util.Logger.Log("일반문구 출력");
                            DisPlayTime = default(DateTime);

                            if ((isFull || FullSpaceControl.ForceFull) && Entrance_Type)
                            {
                                Send(FullMent);
                                //Util.Logger.Log("Send : " + FullMent);
                                full = true;
                            }
                            else
                            {
                                SendMsg(Ment1, (byte)Color1, Ment2, (byte)Color2);
                                full = false;
                            }
                        }
                    }
                    else if (!full)
                        SendMsg(currentMent1, (byte)currentColor1, currentMent2, (byte)currentColor2);
                    else if (full)
                        Send(FullMent);
                }
                catch (Exception) { }
                Thread.Sleep(1000);
            }
            Console.WriteLine("NormalDisPlay abrot");
        }

        public void SendDisplay(string Ment1, string Ment2, int Color1, int Color2)
        {
            //byte[][] arr = GetMessageByte(Ment1, Ment2, (byte)Color1, (byte)Color2);
            byte[][] arr = new byte[2][];
            if (Ment1 == "Visitor")
            { }
            arr[0] = MkMsg(Ment1, Color1);
            arr[1] = MkMsg(Ment2, Color2, false);
            //Util.Logger.Log(string.Format("8색 전광판 출력 1열 '{0}' {1} 2열 '{2}' {3}", Ment1, Color1, Ment2, Color2));
            try
            {
                // (전광판 전송 바이트 덤프 로그 제외 — 노이즈 제거)
                for (int i = 0; i < arr.Length; i++)
                {
                    Send(arr[i]);
                    string indata = string.Empty;
                }
            }
            catch (Exception e)
            {
                Util.Logger.Log(string.Format("SendDisplay Error {0}", e.Message));
            }
        }

        public byte[][] GetMessageByte(string Ment1, string Ment2, byte color1, byte color2)
        {
            byte[] data1 = null;
            byte[] len = null;
            byte[] byteMent1 = null;
            byte[] Color1 = null;
            byte[] Line1 = null;

            byte[] data2 = null;
            byte[] byteMent2 = null;
            byte[] Color2 = null;
            byte[] Line2 = null;

            byteMent1 = byteencoding(Ment1);
            Color1 = GetColor(Ment1, color1);

            if (Encoding.Default.GetBytes(Ment1).Length <= 12)
                data1 = Line1Stay12Byte;
            else
                data1 = Line1Move;

            if (!Ment1.Equals("현재시각"))
            {
                len = ArrayLength(data1.Length + Color1.Length + byteMent1.Length);
                Line1 = CombineTwo(header, len);
                Line1 = CombineTwo(Line1, data1);
                Line1 = CombineTwo(Line1, Color1);
                Line1 = CombineTwo(Line1, byteMent1);
            }
            else
            {
                len = ArrayLength(data1.Length + Timer.Length);
                Line1 = CombineTwo(header, len);
                Line1 = CombineTwo(Line1, data1);
                Line1 = CombineTwo(header, Timer);
            }
            Line1 = CombineTwo(Line1, tail);

            byteMent2 = byteencoding(Ment2);
            if (Ment1 == "Visitor")
            {
                Console.WriteLine(BitConverter.ToString(byteMent2));
            }
            Color2 = GetColor(Ment2, color2);

            if (Encoding.Default.GetBytes(Ment2).Length <= 12)
                data2 = Line2Stay12Byte;
            else
                data2 = Line2Move;
            if (!Ment2.Equals("현재시각"))
            {
                len = ArrayLength(data2.Length + Color2.Length + byteMent2.Length);
                Line2 = CombineTwo(header, len);
                Line2 = CombineTwo(Line2, data2);
                Line2 = CombineTwo(Line2, Color2);
                Line2 = CombineTwo(Line2, byteMent2);
            }
            else
            {
                len = ArrayLength(data2.Length + Timer.Length);
                Line2 = CombineTwo(header, len);
                Line2 = CombineTwo(Line2, data2);
                Line2 = CombineTwo(Line2, Timer);
            }
            Line2 = CombineTwo(Line2, tail);
            return new byte[][] { Line1, Line2 };
        }

        private byte[] byteencoding(String s, String s1)
        {
            System.Text.Encoding euckr = System.Text.Encoding.GetEncoding(51949);
            byte[] euckrBytes = euckr.GetBytes(s + "".PadRight(12 - Encoding.Default.GetByteCount(s)) + s1 + "".PadRight(12 - Encoding.Default.GetByteCount(s1)));
            return euckrBytes;
        }

        private byte[] CombineTwo(byte[] a1, byte[] a2)
        {
            byte[] ret = new byte[a1.Length + a2.Length];
            Array.Copy(a1, 0, ret, 0, a1.Length);
            Array.Copy(a2, 0, ret, a1.Length, a2.Length);
            return ret;
        }

        private byte[] ArrayLength(int length)
        {
            byte[] tmplen = BitConverter.GetBytes(length);
            Array.Reverse(tmplen);

            byte[] len = new byte[2] { 0, 0 };
            Array.Copy(tmplen, 2, len, 0, 2);
            return len;
        }

        private byte[] byteencoding(String s)
        {
            //System.Text.Encoding euckr = System.Text.Encoding.GetEncoding(51949);
            //System.Text.Encoding euckr = System.Text.Encoding.Unicode;
            //byte[] euckrBytes = euckr.GetBytes(s);
            //return euckrBytes;
            byte[] rtnBytes = null;
            char[] ch = s.ToCharArray();
            Encoding encoding = Encoding.Unicode;
            if (CharCode == 0x00)
                encoding = Encoding.Default;
            foreach (char item in ch)
            {
                //태국어 인코딩 코드
                if (0x0E01 <= item && item <= 0x0E5B)
                {
                    encoding = Encoding.Unicode;
                }
                else
                {
                    encoding = Encoding.GetEncoding(51949);
                }
                byte[] en = encoding.GetBytes(item.ToString());
                if (rtnBytes == null)
                    rtnBytes = en;
                else
                {
                    if (encoding == Encoding.Unicode)
                        Array.Reverse(en);
                    rtnBytes = CombineTwo(rtnBytes, en);
                }
            }
            return rtnBytes;
        }

        private byte[] GetColor(string ment, byte color)
        {
            int mentidx = 0;
            byte[] arr = Encoding.Default.GetBytes(ment);
            for (int i = 0; i < arr.Length; i++)
            {
                char[] ch = ment.Substring(mentidx, 1).ToCharArray();
                arr[i] = color;
                if (IsKorean(ch[0]))
                {
                    i++;
                    arr[i] = 0;
                }
                mentidx++;
            }
            return arr;
        }

        private bool IsKorean(char ch)
        {
            //( 한글자 || 자음 , 모음 )
            if ((0xAC00 <= ch && ch <= 0xD7A3) || (0x3131 <= ch && ch <= 0x318E))
                return true;
            else
                return false;
        }

        private byte[] MkMsg(string msg, int icolor, bool isFirst = true)
        {
            byte[] bmsg;
            byte[] ment;
            if (frmLprMain.ENV.CameraEnv.CoreCountry == CoreLogic.THA)
                CharCode = 0x01;
            Line1Stay12Byte[5] = CharCode;
            Line2Stay12Byte[5] = CharCode;
            byte[] data;
            if (isFirst)
                data = Line1Stay12Byte;
            else
                data = Line2Stay12Byte;
            if (CharCode == 0x00)
                ment = Encoding.Default.GetBytes(msg);
            else
            {
                char[] ctmp = msg.ToCharArray();
                Array.Reverse(ctmp);
                msg = new string(ctmp);
                byte[] btmp = Encoding.Unicode.GetBytes(msg);
                Array.Reverse(btmp);
                ment = btmp;
            }
            bmsg = new byte[24 + ment.Length * 2];
            int idx = 0;
            Array.Copy(header, 0, bmsg, 0, header.Length);
            idx += header.Length;
            bmsg[4] = (byte)(bmsg.Length - 7);
            idx += 2;
            Array.Copy(data, 0, bmsg, idx, data.Length);
            idx += data.Length;
            byte[] color = new byte[ment.Length];
            for (int i = 0; i < ment.Length; i++)
            {
                color[i] = (byte)icolor;
                if (ment[i] > 125 || CharCode == 0x01)
                {
                    i++;
                    if (i < ment.Length)
                        color[i] = 0;
                }
            }
            Array.Copy(color, 0, bmsg, idx, color.Length);
            idx += color.Length;
            Array.Copy(ment, 0, bmsg, idx, ment.Length);
            idx += ment.Length;
            Array.Copy(tail, 0, bmsg, idx, tail.Length);
            idx += tail.Length;
            if (msg == "2พน5678")
                Console.WriteLine(BitConverter.ToString(bmsg));
            return bmsg;
        }
    }
}
