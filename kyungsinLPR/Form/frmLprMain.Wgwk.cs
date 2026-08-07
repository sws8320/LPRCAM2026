using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using KyungsinLPR.WGWK;

namespace KyungsinLPR
{
    /// <summary>
    /// frmLprMain 확장 — WGWK-A05D(HTTP snapshot.cgi) 카메라 캡처/인식 흐름.
    /// iNova IPCamera 대신 WgwkCamera를 사용. RecogMode=0(스트로브) 전용. USB 흐름과 동일 구조.
    /// </summary>
    public partial class frmLprMain
    {
        private bool IsWgwkCam(int chIdx)
        {
            return GetCamSource(chIdx) == (int)ClsStructure.CameraSourceType.WGWK;
        }

        /// <summary>WGWK 카메라 초기화 + 연결 (StartCamera_USB 와 동등 역할).</summary>
        private bool StartCamera_Wgwk(int chIdx)
        {
            // 동영상(FAVEngine) 모드는 RTSP 기반 — WGWK 스냅샷 폴링과 맞지 않음
            if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1)
            {
                MessageBox.Show(
                    string.Format("카메라 {0}이(가) WGWK-A05D로 설정되어 있지만,\n인식 방식이 '동영상(FAVEngine)'입니다.\n\nWGWK-A05D는 '스트로브(SSEngine)' 방식만 지원합니다.\n환경설정에서 '인식 방식'을 '스트로브'로 변경하세요.", chIdx),
                    "WGWK 카메라 설정 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var info = (chIdx == 1) ? ENV.CameraEnv.IPCamera1Info : ENV.CameraEnv.IPCamera2Info;
            if (string.IsNullOrWhiteSpace(info.IP))
            {
                Util.Logger.Log(string.Format("[WGWK Cam{0}] IP 없음 — 환경설정에서 카메라 IP 입력 필요", chIdx));
                MessageBox.Show(
                    string.Format("카메라 {0}: WGWK-A05D IP가 비어있습니다.\n환경설정 → 카메라설정 에서 카메라 IP를 입력하세요.", chIdx),
                    "WGWK 카메라 설정 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // HTTP포트 80 / 메인스트림(1) 고정. 계정/비번은 설정값 사용(비면 admin/123456)
            WgwkCamera cam = (chIdx == 1) ? m_camera1_wgwk : m_camera2_wgwk;
            string user = string.IsNullOrWhiteSpace(info.WgwkUser) ? "admin" : info.WgwkUser;
            string pass = string.IsNullOrEmpty(info.WgwkPass) ? "123456" : info.WgwkPass;
            cam.Init(info.IP, 80, user, pass, 1);
            bool ok = cam.ConnectStreamPort();
            Util.Logger.Log(string.Format("[WGWK Cam{0}] 시작 결과={1} IP={2} port=80 stream=1(메인) user={3}",
                chIdx, ok, info.IP, user));
            return ok;
        }

        /// <summary>WGWK 캡처 루프 (채널1). GrabLoop1_USB 흐름과 동일.</summary>
        private void GrabLoop1_Wgwk(object threadParam)
        {
            int CapCnt = 0;
            int CurrentCnt = 0;
            int errCnt = 0;

            while (m_keepGrab1)
            {
                try
                {
                    Bitmap bitmap;
                    IPCamError err = m_camera1_wgwk.GetImage(1000, out bitmap);

                    if (err == IPCamError.OK && bitmap != null)
                    {
                        errCnt = 0;
                        if (label1.Visible) Util.Function.InvokeControlVisible(label1, false);

                        if (!(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1))
                            SetBitmap(PicLpr1Image, bitmap);

                        if (Capture1)
                        {
                            CurrentCnt = ENV.CameraEnv.IPCamera1Info.TriggerCnt;
                            if (CurrentCnt == 0) CurrentCnt = 1;
                            if (FirstDisPlayReturn != null) FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                            Cam1ID++;

                            if (CapCnt < CurrentCnt)
                            {
                                ImgCnt++;
                                string fname = ENV.CameraEnv.IPCamera1Info.ChName
                                               + DateTime.Now.ToString("yyyyMMddHHmmssffff")
                                               + ImgCnt.ToString() + ".jpg";
                                while (true)
                                {
                                    Util.Logger.Log(string.Format("WGWK CAM1 {0}", fname));
                                    if (m_camera1_wgwk.SaveLastImage(fname)) break;
                                    ImgCnt++;
                                    fname = ENV.CameraEnv.IPCamera1Info.ChName
                                            + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                                            + ImgCnt.ToString() + ".jpg";
                                }
                                Util.Logger.Log(string.Format("WGWK CAM1 {0} Saved", fname));

                                RECT roi = new RECT();
                                roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
                                roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
                                roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
                                roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;

                                clsThread.RegArray1[CapCnt].CapCnt = CapCnt;
                                clsThread.RegArray1[CapCnt].SourcePath = fname;
                                clsThread.RegArray1[CapCnt].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                                clsThread.RegArray1[CapCnt].PlateRoi = null;
                                clsThread.RegArray1[CapCnt].PlateNo = null;
                                clsThread.RegArray1[CapCnt].FirstCaptureTime = LastLoopTime1.ToString("yyyy-MM-dd HH:mm:ss");
                                clsThread.RegArray1[CapCnt].Send = false;
                                clsThread.RegArray1[CapCnt].Exposure = 0; // WGWK는 노출 정보 없음
                                if (File.Exists(fname))
                                    clsThread.RegArray1[CapCnt].Size = new FileInfo(fname).Length;

                                Util.Logger.Log(string.Format("****WGWK CAM1 {0} reg Start CapCnt {1} ROI {2}",
                                    fname, CapCnt, clsThread.RegArray1[CapCnt].Roi));

#if WIN64
                                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                    && ENV.CameraEnv.RecogMode == 0)
                                    CoreLogic.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
                                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                                    OptionK.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                                if (RegList1.Count.Equals(0)) Cam1ID = 0;
                                CapCnt++;
                            }
                        }
                        if (Capture1 && (CapCnt == CurrentCnt))
                        {
                            if (CapCnt == 0)
                            {
                                Util.Logger.Log("WGWK Cam1 영상 취득 실패");
                                Capture1 = false;
                                CapCnt = 0;
                            }
                            else
                            {
                                Capture1 = false;
                                CapCnt = 0;
                                Util.Logger.Log("AfterRegPlateCam WGWK Loop1");
                                ThreadPool.QueueUserWorkItem(_ =>
                                {
                                    try { clsThread.AfterRegPlateCam(0, ENV); }
                                    catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam(WGWK1) 예외: " + ex.Message); }
                                });
                            }
                        }
                    }
                    else
                    {
                        if (!label1.Visible) Util.Function.InvokeControlVisible(label1, true);
                        errCnt++;
                        if (errCnt > 50)
                        {
                            Util.Logger.Log("WGWK Cam1 재연결 시도");
                            m_camera1_wgwk.DisconnectStreamPort();
                            Thread.Sleep(200);
                            m_camera1_wgwk.ConnectStreamPort();
                            errCnt = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log("[GrabLoop1_Wgwk] " + ex.Message);
                    Thread.Sleep(100);
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>WGWK 캡처 루프 (채널2).</summary>
        private void GrabLoop2_Wgwk(object threadParam)
        {
            int CapCnt = 0;
            int CurrentCnt = 0;
            int errCnt = 0;

            while (m_keepGrab2)
            {
                try
                {
                    Bitmap bitmap;
                    IPCamError err = m_camera2_wgwk.GetImage(1000, out bitmap);

                    if (err == IPCamError.OK && bitmap != null)
                    {
                        errCnt = 0;
                        if (label2.Visible) Util.Function.InvokeControlVisible(label2, false);

                        if (!(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1))
                            SetBitmap(PicLpr2Image, bitmap);

                        if (Capture2)
                        {
                            CurrentCnt = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
                            if (CurrentCnt == 0) CurrentCnt = 1;
                            if (SecondDisPlayReturn != null) SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                            Cam2ID++;

                            if (CapCnt < CurrentCnt)
                            {
                                ImgCnt++;
                                string fname = ENV.CameraEnv.IPCamera2Info.ChName
                                               + DateTime.Now.ToString("yyyyMMddHHmmssffff")
                                               + ImgCnt.ToString() + ".jpg";
                                while (true)
                                {
                                    Util.Logger.Log(string.Format("WGWK CAM2 {0}", fname));
                                    if (m_camera2_wgwk.SaveLastImage(fname)) break;
                                    ImgCnt++;
                                    fname = ENV.CameraEnv.IPCamera2Info.ChName
                                            + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                                            + ImgCnt.ToString() + ".jpg";
                                }
                                Util.Logger.Log(string.Format("WGWK CAM2 {0} Saved", fname));

                                RECT roi = new RECT();
                                roi.x = ENV.CameraEnv.IPCamera2Info.Roi.Left;
                                roi.y = ENV.CameraEnv.IPCamera2Info.Roi.Top;
                                roi.w = ENV.CameraEnv.IPCamera2Info.Roi.Left + ENV.CameraEnv.IPCamera2Info.Roi.Width;
                                roi.h = ENV.CameraEnv.IPCamera2Info.Roi.Top + ENV.CameraEnv.IPCamera2Info.Roi.Height;

                                clsThread.RegArray2[CapCnt].CapCnt = CapCnt;
                                clsThread.RegArray2[CapCnt].SourcePath = fname;
                                clsThread.RegArray2[CapCnt].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                                clsThread.RegArray2[CapCnt].PlateRoi = null;
                                clsThread.RegArray2[CapCnt].PlateNo = null;
                                clsThread.RegArray2[CapCnt].FirstCaptureTime = LastLoopTime2.ToString("yyyy-MM-dd HH:mm:ss");
                                clsThread.RegArray2[CapCnt].Send = false;
                                clsThread.RegArray2[CapCnt].Exposure = 0;
                                if (File.Exists(fname))
                                    clsThread.RegArray2[CapCnt].Size = new FileInfo(fname).Length;

                                Util.Logger.Log(string.Format("****WGWK CAM2 {0} reg Start CapCnt {1} ROI {2}",
                                    fname, CapCnt, clsThread.RegArray2[CapCnt].Roi));

#if WIN64
                                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                    && ENV.CameraEnv.RecogMode == 0)
                                    CoreLogic.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
                                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                                    OptionK.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                                if (RegList2.Count.Equals(0)) Cam2ID = 0;
                                CapCnt++;
                            }
                        }
                        if (Capture2 && (CapCnt == CurrentCnt))
                        {
                            if (CapCnt == 0)
                            {
                                Util.Logger.Log("WGWK Cam2 영상 취득 실패");
                                Capture2 = false;
                                CapCnt = 0;
                            }
                            else
                            {
                                Capture2 = false;
                                CapCnt = 0;
                                Util.Logger.Log("AfterRegPlateCam WGWK Loop2");
                                ThreadPool.QueueUserWorkItem(_ =>
                                {
                                    try { clsThread.AfterRegPlateCam(1, ENV); }
                                    catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam(WGWK2) 예외: " + ex.Message); }
                                });
                            }
                        }
                    }
                    else
                    {
                        if (!label2.Visible) Util.Function.InvokeControlVisible(label2, true);
                        errCnt++;
                        if (errCnt > 50)
                        {
                            Util.Logger.Log("WGWK Cam2 재연결 시도");
                            m_camera2_wgwk.DisconnectStreamPort();
                            Thread.Sleep(200);
                            m_camera2_wgwk.ConnectStreamPort();
                            errCnt = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log("[GrabLoop2_Wgwk] " + ex.Message);
                    Thread.Sleep(100);
                }
                Thread.Sleep(10);
            }
        }
    }
}
