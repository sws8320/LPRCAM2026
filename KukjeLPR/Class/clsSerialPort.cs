using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using SerialDevice;

namespace KyungsinLPR
{
    public class clsSerialPort
    {
        public SerialDevice.ClsDisplay3Color FirstDisPlay3;
        public SerialDevice.ClsDisplay3Color SecondDisPlay3;
        public SerialDevice.ClsDisplay8Color FirstDisPlay8;
        public SerialDevice.ClsDisplay8Color SecondDisPlay8;
        public SerialDevice.Amano3ColorSmall FirstDisPlayAmano3;
        public SerialDevice.Amano3ColorSmall SecondDisPlayAmano3;

        public SerialDevice.ClsKJC_1000 KJC1000 = null;
        public SerialDevice.ClsRealSys RealSys = null;
        public ClsDingtian Dingtian = null;
        // 서버모드 다중 Dingtian 박스(카메라별 DioIp). 메인(.110) 외 추가 박스를 IP별로 캐시.
        private readonly System.Collections.Generic.Dictionary<string, ClsDingtian> _camDingtian = new System.Collections.Generic.Dictionary<string, ClsDingtian>();
        private ClsStructure.EnvStruct Env;
        private SerialPort FirstDisPlayPort = new SerialPort();
        private SerialPort SecondDisPlayPort = new SerialPort();
        private SerialPort DioPort = new SerialPort();

        public delegate void eventInput(int Port, bool Up);
        public event eventInput LoopOn;

        private bool disposed = false;

        public clsSerialPort(ClsStructure.EnvStruct _Env)
        {
            Env = _Env;
            ///DisPlay Setting 
            try
            {
                if (Env.CommunicationEnv.DisPlay[0].Com.SerialPort != string.Empty)
                {
                    if (Env.CommunicationEnv.DisPlay[0].Use && !Env.CommunicationEnv.DisPlay[0].Net.Use)
                    {
                        Util.Logger.Log("1번 전광판 설정" + Env.CommunicationEnv.DisPlay[0].Com.SerialPort);
                        if (Env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                        {
                            FirstDisPlayPort = SetSerialPort(Env.CommunicationEnv.DisPlay[0].Com.SerialPort, Env.CommunicationEnv.DisPlay[0].Com.Setting);
                            FirstDisPlayPort.Open();
                            FirstDisPlay3 = new ClsDisplay3Color(FirstDisPlayPort);
                        }
                        else if (Env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                        {
                            FirstDisPlayPort = SetSerialPort(Env.CommunicationEnv.DisPlay[0].Com.SerialPort, Env.CommunicationEnv.DisPlay[0].Com.Setting);
                            FirstDisPlayPort.Open();
                            FirstDisPlay8 = new ClsDisplay8Color(FirstDisPlayPort);
                            FirstDisPlay8.TimerSync();
                        }
                        else if (Env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                        {
                            FirstDisPlayPort = SetSerialPort(Env.CommunicationEnv.DisPlay[0].Com.SerialPort, Env.CommunicationEnv.DisPlay[0].Com.Setting);
                            FirstDisPlayPort.Open();
                            FirstDisPlayAmano3 = new Amano3ColorSmall(FirstDisPlayPort);
                        }
                    }
                }
            }
            catch (Exception DisPlay1nd_Error)
            {
                Util.Logger.Log("DIO 1st DisPlay Open Fail " + DisPlay1nd_Error.Message);
            }
            try
            {
                if (Env.CommunicationEnv.DisPlay[1].Com.SerialPort != string.Empty)
                {
                    if (Env.CommunicationEnv.DisPlay[1].Use && !Env.CommunicationEnv.DisPlay[1].Net.Use)
                    {
                        Util.Logger.Log("2번 전광판 설정" + Env.CommunicationEnv.DisPlay[1].Com.SerialPort);
                        if (Env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                        {
                            SecondDisPlayPort = SetSerialPort(Env.CommunicationEnv.DisPlay[1].Com.SerialPort, Env.CommunicationEnv.DisPlay[1].Com.Setting);
                            SecondDisPlayPort.Open();
                            SecondDisPlay3 = new ClsDisplay3Color(SecondDisPlayPort);
                        }
                        else if (Env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                        {
                            SecondDisPlayPort = SetSerialPort(Env.CommunicationEnv.DisPlay[1].Com.SerialPort, Env.CommunicationEnv.DisPlay[1].Com.Setting);
                            //ExitDisPlayPort.DataReceived += new SerialDataReceivedEventHandler(ExitDisPlayPort_DataReceived);
                            SecondDisPlayPort.Open();
                            SecondDisPlay8 = new ClsDisplay8Color(SecondDisPlayPort);
                            SecondDisPlay8.TimerSync();
                        }
                        else if (Env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                        {
                            SecondDisPlayPort = SetSerialPort(Env.CommunicationEnv.DisPlay[1].Com.SerialPort, Env.CommunicationEnv.DisPlay[1].Com.Setting);
                            SecondDisPlayPort.Open();
                            SecondDisPlayAmano3 = new Amano3ColorSmall(SecondDisPlayPort);
                        }
                    }
                }
            }
            catch (Exception DisPlay2nd_Error)
            {
                Util.Logger.Log("DIO 2nd DisPlay Open Fail " + DisPlay2nd_Error.Message);
            }
            ///DIO Setting
            try
            {
                Util.Logger.Log("DIO 설정");
                if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.DINGTIAN.ToString()))
                {
                    // DINGTIAN 이더넷 릴레이 — 시리얼 포트 사용 안 함. HTTP /input.cgi 폴링으로 입력 수신
                    int netPort = Env.CommonEnv.Dio.DioSetting.NetPort > 0 ? Env.CommonEnv.Dio.DioSetting.NetPort : 60001;
                    Dingtian = new ClsDingtian(Env.CommonEnv.Dio.DioSetting.IpAddress, netPort);
                    Dingtian.InEvent += new ClsDingtian.eventInput(DIOINPUT);
                    // 입력 폴링 주기 — [COMMON] diopollms (없거나 범위밖이면 50ms). 낮을수록 검지 빠름(보드/네트워크 부하 증가)
                    int pollMs = Util.Function.IntTryParse(Util.Function.IniReadValue("COMMON", "diopollms"));
                    if (pollMs < 30 || pollMs > 5000) pollMs = 50;
                    Dingtian.StartInputPolling(pollMs);
                    Util.Logger.Log(string.Format("DINGTIAN 시작: {0}:{1} (입력폴링 {2}ms)", Env.CommonEnv.Dio.DioSetting.IpAddress, netPort, pollMs));
                }
                else
                {
                    DioPort = SetSerialPort(Env.CommonEnv.Dio.DioSetting.SerialPort, Env.CommonEnv.Dio.DioSetting.Setting);
                    DioPort.Open();
                    if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                    {
                        KJC1000 = new ClsKJC_1000(DioPort);
                        KJC1000.InEvent += new ClsKJC_1000.eventInput(DIOINPUT);
                    }
                    else if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
                    {
                        RealSys = new ClsRealSys(DioPort);
                        RealSys.InEvent += new ClsRealSys.eventInput(DIOINPUT);
                    }
                }
            }
            catch (Exception DioError)
            {
                Util.Logger.Log("DIO Port Open Fail " + DioError.Message);
            }
        }

        //public void DisPlayMent(int DevIdx, string Ment1, string Color1, string Ment2, string Color2)
        //{

        //}
        //string color convert to int color need
        public void DisPlayMent(int DevIdx, string Ment1, string Color1, string Ment2, string Color2)
        {
            if (Env.CommunicationEnv.DisPlay[DevIdx].Use)
                if (Env.CommunicationEnv.DisPlay[DevIdx].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                {
                    if (DevIdx.Equals(0))
                    {
                        if (FirstDisPlayPort.IsOpen)
                            FirstDisPlay3.WriteDisPlay(Ment1, Ment2, clsFunction.GetColor3Int(Color1), clsFunction.GetColor3Int(Color2));
                    }
                    else
                    {
                        if (SecondDisPlayPort.IsOpen)
                            SecondDisPlay3.WriteDisPlay(Ment1, Ment2, clsFunction.GetColor3Int(Color1), clsFunction.GetColor3Int(Color2));
                    }
                }
                else if (Env.CommunicationEnv.DisPlay[DevIdx].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                {
                    if (DevIdx.Equals(0))
                    {
                        if (FirstDisPlayPort.IsOpen)
                        {
                            //Util.Logger.Log(string.Format("DISPLAY {0} {1}", Ment1, Ment2));
                            FirstDisPlay8.SendDisplay(Ment1, Ment2, clsFunction.GetColor8Int(Color1), clsFunction.GetColor8Int(Color2));
                            //EntranceDisPlay8.SendDisplay(EntranceDisPlay8.GetOneLineMessageByte(Ment1, Ment2, (byte)clsFunction.GetColor8Int(Color1), (byte)clsFunction.GetColor8Int(Color2)));
                        }
                    }
                    else
                    {
                        if (SecondDisPlayPort.IsOpen)
                            SecondDisPlay8.SendDisplay(Ment1, Ment2, clsFunction.GetColor8Int(Color1), clsFunction.GetColor8Int(Color2));
                        //ExitDisPlay8.SendDisplay(ExitDisPlay8.GetOneLineMessageByte(Ment1, Ment2, (byte)clsFunction.GetColor8Int(Color1), (byte)clsFunction.GetColor8Int(Color2)));
                    }
                }
                else if (Env.CommunicationEnv.DisPlay[DevIdx].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                {
                    if (DevIdx.Equals(0))
                    {
                        if (FirstDisPlayPort.IsOpen)
                        {
                            //Util.Logger.Log(string.Format("DISPLAY {0} {1}", Ment1, Ment2));
                            FirstDisPlayAmano3.SendDisplay(Ment1, clsFunction.GetAmanoColor3uInt(Color1), Ment2, clsFunction.GetAmanoColor3uInt(Color2));
                            //EntranceDisPlay8.SendDisplay(EntranceDisPlay8.GetOneLineMessageByte(Ment1, Ment2, (byte)clsFunction.GetColor8Int(Color1), (byte)clsFunction.GetColor8Int(Color2)));
                        }
                    }
                    else
                    {
                        if (SecondDisPlayPort.IsOpen)
                            SecondDisPlayAmano3.SendDisplay(Ment1, clsFunction.GetAmanoColor3uInt(Color1), Ment2, clsFunction.GetAmanoColor3uInt(Color2));
                        //ExitDisPlay8.SendDisplay(ExitDisPlay8.GetOneLineMessageByte(Ment1, Ment2, (byte)clsFunction.GetColor8Int(Color1), (byte)clsFunction.GetColor8Int(Color2)));
                    }
                }
        }

        public bool IsDingtian()
        {
            return Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.DINGTIAN.ToString());
        }

        /// <summary>카메라(DevIdx)의 Dingtian 박스 — 서버캠(>=2)이 메인과 다른 DioIp면 그 박스(없으면 생성·캐시), 아니면 메인.</summary>
        private ClsDingtian DingtianFor(int DevIdx)
        {
            try
            {
                if (DevIdx >= 2)
                {
                    string ip = clsServerCamEnv.GetDioIp(DevIdx);
                    string mainIp = Env.CommonEnv.Dio.DioSetting.IpAddress ?? "";
                    if (!string.IsNullOrEmpty(ip) && ip != mainIp)
                    {
                        ClsDingtian d;
                        if (!_camDingtian.TryGetValue(ip, out d))
                        {
                            int port = Env.CommonEnv.Dio.DioSetting.NetPort > 0 ? Env.CommonEnv.Dio.DioSetting.NetPort : 60001;
                            d = new ClsDingtian(ip, port);
                            _camDingtian[ip] = d;
                            Util.Logger.Log(string.Format("DINGTIAN 추가 박스 연결 {0}:{1} (카메라{2})", ip, port, DevIdx + 1));
                        }
                        return d;
                    }
                }
            }
            catch (Exception ex) { Util.Logger.Log("DingtianFor 오류: " + ex.Message); }
            return Dingtian;   // 메인 공유박스
        }

        public void GateOpen(int DevIdx)
        {
            try
            {
                if (IsDingtian())
                {
                    // 서버모드 다중박스: 카메라별 Dingtian IP가 메인과 다르면 그 박스로 라우팅(8릴레이 초과/분산)
                    ClsDingtian dt = DingtianFor(DevIdx);
                    Util.Logger.Log("차단기 개방 DINGTIAN");
                    if (dt == null) return;
                    dt.RelayOn(Env.CommonEnv.Dio.DioOutPut[DevIdx].Port, Env.CommonEnv.Dio.DioOutPut[DevIdx].Delay, Env.CommonEnv.Dio.DioOutPut[DevIdx].Keep);
                    if (Env.CommonEnv.Dio.DioOutPut[DevIdx].AddPort > 0)
                        dt.RelayOn(Env.CommonEnv.Dio.DioOutPut[DevIdx].AddPort, Env.CommonEnv.Dio.DioOutPut[DevIdx].AddDelay, Env.CommonEnv.Dio.DioOutPut[DevIdx].AddKeep);
                    return;
                }
                Util.Logger.Log("차단기 개방 DioPort" + DioPort.IsOpen.ToString());
                if (!DioPort.IsOpen)
                    DioPort.Open();
                if (DioPort.IsOpen)
                {
                    Util.Logger.Log("GateOpen");
                    if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                    {
                        Util.Logger.Log("KJC1000");
                        KJC1000.RelayOn(Env.CommonEnv.Dio.DioOutPut[DevIdx].Port, Env.CommonEnv.Dio.DioOutPut[DevIdx].Delay, Env.CommonEnv.Dio.DioOutPut[DevIdx].Keep);
                        if (Env.CommonEnv.Dio.DioOutPut[DevIdx].AddPort > 0)
                            KJC1000.RelayOn(Env.CommonEnv.Dio.DioOutPut[DevIdx].AddPort, Env.CommonEnv.Dio.DioOutPut[DevIdx].AddDelay, Env.CommonEnv.Dio.DioOutPut[DevIdx].AddKeep);
                    }
                    else if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
                    {
                        RealSys.RelayOn(Env.CommonEnv.Dio.DioOutPut[DevIdx].Port, Env.CommonEnv.Dio.DioOutPut[DevIdx].Delay, Env.CommonEnv.Dio.DioOutPut[DevIdx].Keep);
                        if (Env.CommonEnv.Dio.DioOutPut[DevIdx].AddPort > 0)
                            RealSys.RelayOn(Env.CommonEnv.Dio.DioOutPut[DevIdx].AddPort, Env.CommonEnv.Dio.DioOutPut[DevIdx].AddDelay, Env.CommonEnv.Dio.DioOutPut[DevIdx].AddKeep);
                    }
                }
            }
            catch (Exception GateOpenError)
            {
                Util.Logger.Log(string.Format("GateOpen Error {0}", GateOpenError));
            }
        }

        public void TestGateOpen(int Port)
        {
            if (IsDingtian())
            {
                if (Dingtian != null) Dingtian.RelayOn(Port, 0, 800);
                return;
            }
            if (DioPort.IsOpen)
                if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                    KJC1000.RelayOn(Port, 0, 800);
                else if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
                    RealSys.RelayOn(Port, 0, 800);
        }

        public void IsolatedGateOpen()
        {
            if (IsDingtian())
            {
                if (Dingtian == null) return;
                Dingtian.RelayOn(Env.CommonEnv.Dio.IsolatePort.Out.Port, Env.CommonEnv.Dio.IsolatePort.Out.Delay, Env.CommonEnv.Dio.IsolatePort.Out.Keep);
                if (Env.CommonEnv.Dio.IsolatePort.Out.AddPort > 0)
                    Dingtian.RelayOn(Env.CommonEnv.Dio.IsolatePort.Out.AddPort, Env.CommonEnv.Dio.IsolatePort.Out.AddDelay, Env.CommonEnv.Dio.IsolatePort.Out.AddKeep);
                return;
            }
            if (DioPort.IsOpen)
            {
                if (Env.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                {
                    KJC1000.RelayOn(Env.CommonEnv.Dio.IsolatePort.Out.Port, Env.CommonEnv.Dio.IsolatePort.Out.Delay, Env.CommonEnv.Dio.IsolatePort.Out.Keep);
                    if (Env.CommonEnv.Dio.IsolatePort.Out.AddPort > 0)
                        KJC1000.RelayOn(Env.CommonEnv.Dio.IsolatePort.Out.AddPort, Env.CommonEnv.Dio.IsolatePort.Out.AddDelay, Env.CommonEnv.Dio.IsolatePort.Out.AddKeep);
                }
            }
        }
        
        private SerialPort Serial = new SerialPort();
        private Thread PollingThread;
        public delegate void eventRecv(string Recv);
        public event eventRecv SerialRecv;
        public int DataLength = 0;
        private String RecvString = String.Empty;
        public enum TransType
        {
            Event, Polling
        }
        public clsSerialPort() { }

        //public bool PortOpen(String Port, int BaudRate, Parity ParityBit, int DataBit, StopBits StopBit, TransType Ttype, String PollingString = "")
        public bool PortOpen(String Port, string Setting, TransType Ttype, String PollingString = "")
        {
            bool rtn = false;
            try
            {

                Serial.DataReceived += new SerialDataReceivedEventHandler(DataRecv);
                string[] sp = Setting.Split(',');
                Serial.BaudRate = Util.Function.IntTryParse(sp[0]);
                switch (sp[1].ToLower())
                {
                    case "e":
                        Serial.Parity = Parity.Even;
                        break;
                    case "m":
                        Serial.Parity = Parity.Mark;
                        break;
                    case "n":
                        Serial.Parity = Parity.None;
                        break;
                    case "o":
                        Serial.Parity = Parity.Odd;
                        break;
                    case "s":
                        Serial.Parity = Parity.Space;
                        break;
                }
                Serial.DataBits = Util.Function.IntTryParse(sp[2]);
                Serial.StopBits = (StopBits)Enum.Parse(typeof(StopBits), sp[3]);
                Serial.PortName = Port;
                if (Serial.IsOpen)
                    Serial.Close();
                Serial.Open();

                if (Ttype.Equals(TransType.Polling))
                {
                    PollingThread = new Thread(() => ThreadPolling(PollingString));
                    PollingThread.IsBackground = true;
                    PollingThread.Start();
                }
                rtn = true;
            }
            catch (Exception)
            {

            }
            return rtn;
        }

        public bool PortClose()
        {
            bool rtn = false;
            try
            {
                if (PollingThread != null && PollingThread.IsAlive)
                {
                    PollingThread.Join();
                    PollingThread = null;
                }
                Serial.Close();
                rtn = true;
            }
            catch (Exception)
            {

            }
            return rtn;
        }

        private void DataRecv(object sender, SerialDataReceivedEventArgs e)
        {
            RecvString += Serial.ReadExisting();
            if (DataLength.Equals(0) || RecvString.Length >= DataLength)
            {
                this.SerialRecv(RecvString);
                RecvString = string.Empty;
            }
        }

        private void ThreadPolling(String Poll)
        {
            string Send = string.Format("{0}{1}", Poll, (char)13);
            while (true)
            {
                try
                {
                    if (Serial.IsOpen)
                    {
                        Serial.WriteLine(Send);
                    }
                    Thread.Sleep(200);
                }
                catch (Exception)
                {

                }
            }
        }

        public SerialPort SetSerialPort(String Port, string Setting)
        {
            SerialPort ComPort = new SerialPort();
            try
            {
                string[] sp = Setting.Split(',');
                ComPort.BaudRate = Util.Function.IntTryParse(sp[0]);
                switch (sp[1])
                {
                    case "e":
                        ComPort.Parity = Parity.Even;
                        break;
                    case "m":
                        ComPort.Parity = Parity.Mark;
                        break;
                    case "n":
                        ComPort.Parity = Parity.None;
                        break;
                    case "o":
                        ComPort.Parity = Parity.Odd;
                        break;
                    case "s":
                        ComPort.Parity = Parity.Space;
                        break;
                }
                ComPort.DataBits = Util.Function.IntTryParse(sp[2]);
                ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), sp[3]);
                ComPort.PortName = Port;
            }
            catch (Exception)
            { }
            return ComPort;
        }

        private void DIOINPUT(int Port, bool On)
        {
            if (this.LoopOn != null)
            {
                if (On) Util.Logger.Log(string.Format("{0}번 포트 {1}", Port, On));
                this.LoopOn(Port, On);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (FirstDisPlayPort != null)
                    FirstDisPlayPort.Dispose();
                if (SecondDisPlayPort != null)
                    SecondDisPlayPort.Dispose();
                if (DioPort != null)
                    DioPort.Dispose();
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        ~clsSerialPort()
        {
            Dispose(false);
        }
    }
}
