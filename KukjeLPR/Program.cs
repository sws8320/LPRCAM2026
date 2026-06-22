using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace KyungsinLPR
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // 전역 예외 핸들러 — 어떤 스레드에서든 예외 발생 시 사용자에게 메시지박스로 안내, 종료 방지
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => ShowFatalDialog("Application.ThreadException (UI 스레드)", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                ShowFatalDialog("AppDomain.UnhandledException (백그라운드 스레드)", ex);
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                ShowFatalDialog("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            bool flagMutex;

            Mutex m_hMutex;
            //LprRelay.Read_Ini();

            //if (LprRelay.USE && LprRelay.TYPE != string.Empty)
            //{
            //    m_hMutex = new Mutex(true, AppDomain.CurrentDomain.FriendlyName, out flagMutex);
                
            //}
            //else
            m_hMutex = new Mutex(true, "KyungsinLpr", out flagMutex);
            
            if (flagMutex)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Util.Function.IniFileName = string.Format("{0}\\CameraSetting.ini", Util.Global.ROOT);
                switch (Util.Function.IntTryParse(Util.Function.IniReadValue("COMMON", "starttype")))
                {
                    //case (int)ClsStructure.ProgramStartType.COM:
                    //    Application.Run(new frmLPRComm());
                    //    break;
                    default:
                        if (args.Length > 0 && args[0] == "ENV")
                        {
                            ClsStructure.EnvStruct ENV = new ClsStructure.EnvStruct();
                            clsFunction func = new clsFunction();
                            IPCamera m_camera1 = new IPCamera();
                            IPCamera m_camera2 = new IPCamera();
                            //leess iNova2추가
                            iNova2.IPCamera m_camera1_iNova2 = new iNova2.IPCamera();
                            iNova2.IPCamera m_camera2_iNova2 = new iNova2.IPCamera();
                            ENV = func.GetEnv(ENV);
                            BeforeCalOpt.Load();
                            clsOutService.Load();
                            clsBusinessCar.ReadIni();
                            NoDriving.Load();
                            Application.Run(new frmEnv(ENV, m_camera1, m_camera2, m_camera1_iNova2, m_camera2_iNova2));
                        }
                        else
                            Application.Run(new frmLprMain());
                        break;
                }
                m_hMutex.ReleaseMutex();
            }
            else
            {
                AutoClosingMessageBox msg = new AutoClosingMessageBox();
                msg.Show("프로그램이 이미 실행중입니다.", "중복실행방지", 3000);
            }
        }

        /// <summary>전역에서 잡힌 치명적 예외를 메시지박스로 표시 — 종료 막기 위한 안전망</summary>
        private static void ShowFatalDialog(string source, Exception ex)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("■ KyungsinLPR 예외 발생");
                sb.AppendLine();
                sb.AppendLine("발생 위치: " + source);
                if (ex != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("예외 형식: " + ex.GetType().Name);
                    sb.AppendLine("메시지: " + ex.Message);
                    if (ex.HResult != 0)
                        sb.AppendLine(string.Format("HRESULT: 0x{0:X8}", ex.HResult));
                    sb.AppendLine();
                    sb.AppendLine("스택 트레이스:");
                    sb.AppendLine(ex.StackTrace ?? "(없음)");
                    if (ex.InnerException != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine("내부 예외: " + ex.InnerException.GetType().Name);
                        sb.AppendLine("내부 메시지: " + ex.InnerException.Message);
                    }
                }
                sb.AppendLine();
                sb.AppendLine("로그 파일에서 시간대별 컨텍스트를 확인하세요.");
                try
                {
                    Util.Logger.Log(string.Format("[FATAL] {0}: {1}", source, ex != null ? ex.ToString() : "(null)"));
                }
                catch { }
                MessageBox.Show(sb.ToString(), "KyungsinLPR 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { /* 메시지박스 자체가 실패하면 무시 */ }
        }
    }
}
