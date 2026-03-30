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
    }
}
