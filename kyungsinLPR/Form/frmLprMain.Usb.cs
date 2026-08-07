using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using KyungsinLPR.USB;

namespace KyungsinLPR
{
    /// <summary>
    /// frmLprMain 확장 — USB(DirectShow/UVC) 카메라 캡처/인식 흐름.
    /// iNova IPCamera 대신 USBCamera를 사용. RecogMode=0(스트로브) 전용.
    /// </summary>
    public partial class frmLprMain
    {
        /// <summary>채널의 카메라 소스 반환. CameraSource==Default(0)이면 전역 iNovaType 사용 (하위호환).</summary>
        private int GetCamSource(int chIdx)
        {
            int src = (chIdx == 1)
                ? ENV.CameraEnv.IPCamera1Info.CameraSource
                : ENV.CameraEnv.IPCamera2Info.CameraSource;
            if (src == (int)ClsStructure.CameraSourceType.Default)
                return ENV.CameraEnv.iNovaType; // 1 or 2
            return src;
        }

        private bool IsUsbCam(int chIdx)
        {
            return GetCamSource(chIdx) == (int)ClsStructure.CameraSourceType.USB;
        }

        /// <summary>USB 카메라 초기화 + 캡처 시작 (StartCamera_iNova1/iNova2 와 동등 역할).</summary>
        private bool StartCamera_USB(int chIdx)
        {
            // FAVEngine(동영상) 모드는 RTSP 기반이라 USB 불가 — 가드
            if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1)
            {
                MessageBox.Show(
                    string.Format("카메라 {0}이(가) USB로 설정되어 있지만,\n인식 방식이 '동영상(FAVEngine)'입니다.\n\nUSB 카메라는 '스트로브(SSEngine)' 방식만 지원합니다.\n환경설정에서 '인식 방식'을 '스트로브'로 변경하세요.", chIdx),
                    "USB 카메라 설정 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var info = (chIdx == 1) ? ENV.CameraEnv.IPCamera1Info : ENV.CameraEnv.IPCamera2Info;
            if (string.IsNullOrWhiteSpace(info.UsbMoniker))
            {
                Util.Logger.Log(string.Format("[USB Cam{0}] moniker 없음 — 환경설정에서 USB 장치 선택 필요", chIdx));
                MessageBox.Show(
                    string.Format("카메라 {0}: USB 장치가 선택되지 않았습니다.\n환경설정 → 카메라설정 → 'USB 설정...' 에서 장치를 선택하세요.", chIdx),
                    "USB 카메라 설정 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            USBCamera cam = (chIdx == 1) ? m_camera1_usb : m_camera2_usb;
            cam.Init(info.UsbMoniker, info.UsbDeviceName, info.UsbResolutionWidth, info.UsbResolutionHeight);
            bool ok = cam.ConnectStreamPort();
            Util.Logger.Log(string.Format("[USB Cam{0}] 시작 결과={1} 장치='{2}' {3}x{4}",
                chIdx, ok, info.UsbDeviceName, info.UsbResolutionWidth, info.UsbResolutionHeight));
            return ok;
        }

        /// <summary>USB 캡처 루프 (채널1). GrabLoop1_iNova2 흐름을 USB용으로 단순화.</summary>
        private void GrabLoop1_USB(object threadParam)
        {
            int CapCnt = 0;
            int CurrentCnt = 0;
            int errCnt = 0;

            while (m_keepGrab1)
            {
                try
                {
                    Bitmap bitmap;
                    IPCamError err = m_camera1_usb.GetImage(1000, out bitmap);

                    if (err == IPCamError.OK && bitmap != null)
                    {
                        errCnt = 0;
                        if (label1.Visible) Util.Function.InvokeControlVisible(label1, false);

                        // 프리뷰 갱신
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
                                    Util.Logger.Log(string.Format("USB CAM1 {0}", fname));
                                    if (m_camera1_usb.SaveLastImage(fname)) break;
                                    ImgCnt++;
                                    fname = ENV.CameraEnv.IPCamera1Info.ChName
                                            + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                                            + ImgCnt.ToString() + ".jpg";
                                }
                                Util.Logger.Log(string.Format("USB CAM1 {0} Saved", fname));

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
                                clsThread.RegArray1[CapCnt].Exposure = 0; // USB는 노출 정보 없음
                                if (File.Exists(fname))
                                    clsThread.RegArray1[CapCnt].Size = new FileInfo(fname).Length;

                                Util.Logger.Log(string.Format("****USB CAM1 {0} reg Start CapCnt {1} ROI {2}",
                                    fname, CapCnt, clsThread.RegArray1[CapCnt].Roi));

#if WIN64
                                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                    && ENV.CameraEnv.RecogMode == 0)
                                    CoreLogic.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
                                //Option(K) — 자체 CRNN (64bit 전용)
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
                                Util.Logger.Log("USB Cam1 영상 취득 실패");
                                Capture1 = false;
                                CapCnt = 0;
                            }
                            else
                            {
                                Capture1 = false;
                                CapCnt = 0;
                                Util.Logger.Log("AfterRegPlateCam USB Loop1");
                                ThreadPool.QueueUserWorkItem(_ =>
                                {
                                    try { clsThread.AfterRegPlateCam(0, ENV); }
                                    catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam(USB1) 예외: " + ex.Message); }
                                });
                            }
                        }
                        // bitmap.Dispose() 호출 금지 — SetBitmap이 BeginInvoke로 비동기 할당하므로
                        // PictureBox.Image가 소유권 가짐. SetBitmap 내부에서 이전 Image.Dispose() 처리됨.
                    }
                    else
                    {
                        if (!label1.Visible) Util.Function.InvokeControlVisible(label1, true);
                        errCnt++;
                        if (errCnt > 50)
                        {
                            Util.Logger.Log("USB Cam1 재연결 시도");
                            m_camera1_usb.DisconnectStreamPort();
                            Thread.Sleep(200);
                            m_camera1_usb.ConnectStreamPort();
                            errCnt = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log("[GrabLoop1_USB] " + ex.Message);
                    Thread.Sleep(100);
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>USB 캡처 루프 (채널2).</summary>
        private void GrabLoop2_USB(object threadParam)
        {
            int CapCnt = 0;
            int CurrentCnt = 0;
            int errCnt = 0;

            while (m_keepGrab2)
            {
                try
                {
                    Bitmap bitmap;
                    IPCamError err = m_camera2_usb.GetImage(1000, out bitmap);

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
                                    Util.Logger.Log(string.Format("USB CAM2 {0}", fname));
                                    if (m_camera2_usb.SaveLastImage(fname)) break;
                                    ImgCnt++;
                                    fname = ENV.CameraEnv.IPCamera2Info.ChName
                                            + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                                            + ImgCnt.ToString() + ".jpg";
                                }
                                Util.Logger.Log(string.Format("USB CAM2 {0} Saved", fname));

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

                                Util.Logger.Log(string.Format("****USB CAM2 {0} reg Start CapCnt {1} ROI {2}",
                                    fname, CapCnt, clsThread.RegArray2[CapCnt].Roi));

#if WIN64
                                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                    && ENV.CameraEnv.RecogMode == 0)
                                    CoreLogic.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
                                //Option(K) — 자체 CRNN (64bit 전용)
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
                                Util.Logger.Log("USB Cam2 영상 취득 실패");
                                Capture2 = false;
                                CapCnt = 0;
                            }
                            else
                            {
                                Capture2 = false;
                                CapCnt = 0;
                                Util.Logger.Log("AfterRegPlateCam USB Loop2");
                                ThreadPool.QueueUserWorkItem(_ =>
                                {
                                    try { clsThread.AfterRegPlateCam(1, ENV); }
                                    catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam(USB2) 예외: " + ex.Message); }
                                });
                            }
                        }
                        // bitmap.Dispose() 호출 금지 — PictureBox가 소유.
                    }
                    else
                    {
                        if (!label2.Visible) Util.Function.InvokeControlVisible(label2, true);
                        errCnt++;
                        if (errCnt > 50)
                        {
                            Util.Logger.Log("USB Cam2 재연결 시도");
                            m_camera2_usb.DisconnectStreamPort();
                            Thread.Sleep(200);
                            m_camera2_usb.ConnectStreamPort();
                            errCnt = 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log("[GrabLoop2_USB] " + ex.Message);
                    Thread.Sleep(100);
                }
                Thread.Sleep(10);
            }
        }
    }
}
