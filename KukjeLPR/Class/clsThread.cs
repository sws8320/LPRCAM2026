#define SaveBigSize

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace KyungsinLPR
{
    public static class clsThread
    {
        public static frmLprMain main = null;

        //정품 인증 여부
        public static bool Auth = false;
        public static frmLPRComm frm = null;
        public static string ImageSavePath = string.Empty;
        public static ClsStructure.RegStruct[] RegArray1 = new ClsStructure.RegStruct[4];
        public static ClsStructure.RegStruct[] RegArray2 = new ClsStructure.RegStruct[4];
        public static int Cam1RegCnt = 0;
        public static int Cam2RegCnt = 0;

        //인식 결과 후 처리
        public static void AfterRegPlateCam(int Camidx, ClsStructure.EnvStruct CamEnv)
        {
            string LogCamIDX = string.Format("CamIdx : {0}", Camidx);
            try
            {
                ClsStructure.IPCamera_Basic_Setting CamInfo = new ClsStructure.IPCamera_Basic_Setting();
                ClsStructure.Lpr_Info LprInfo = new ClsStructure.Lpr_Info();
                ClsStructure.RegStruct[] RegArray = new ClsStructure.RegStruct[4];
                Util.Logger.Log(string.Format("{0} AfterRegPlateCam Start", LogCamIDX));
                switch (Camidx)
                {
                    case 0:
                        CamInfo = CamEnv.CameraEnv.IPCamera1Info;
                        LprInfo = CamEnv.CommunicationEnv.Lpr1Info;
                        RegArray = RegArray1;
                        break;
                    case 1:
                        CamInfo = CamEnv.CameraEnv.IPCamera2Info;
                        LprInfo = CamEnv.CommunicationEnv.Lpr2Info;
                        RegArray = RegArray2;
                        break;
                }
                Util.Logger.Log(string.Format("{0} Capture Time {1}", LogCamIDX, RegArray[0].FirstCaptureTime));
                int CaptureCnt = 0;
                switch (CamInfo.CurrentInfo.BracketInfo.Use)
                {
                    case true:
                        CaptureCnt = CamInfo.BarkectCnt;
                        break;
                    default:
                        CaptureCnt = CamInfo.TriggerCnt;
                        break;
                }
                
                Util.Logger.Log(string.Format("Capture Count {0}", CaptureCnt));
                for (int i = 0; i < CaptureCnt; i++)
                {
                    Util.Logger.Log(string.Format("{0} 번째 정보 File Name {1} 캡쳐 시각 {2}", i + 1, RegArray[i].SourcePath, RegArray[i].FirstCaptureTime));
                }
                //regarray 차번 인식 갯수 확인
                //CaptureCnt = 0;
                int checkCnt = 0;
                int beforecnt = -1;
                try
                {
                    while (checkCnt != CaptureCnt)
                    {
                        checkCnt = 0;
                        foreach (ClsStructure.RegStruct item in RegArray)
                        {
                            if (!string.IsNullOrEmpty(item.PlateNo)) checkCnt++;
                        }

                        if (checkCnt >= CaptureCnt)
                            break;
                        //인식 완료 전에 빠져 나가서 수정 20170504 End

                        Thread.Sleep(100);
                        if ((DateTime.Now - Util.Function.DateTimeTryParse(RegArray[0].FirstCaptureTime)).TotalSeconds > 10)
                        {
                            Util.Logger.Log(string.Format("{0} 인식 모듈 처리 대기 시간 경과 대기 종료 최초 촬영 시각 {1}", LogCamIDX, RegArray[0].FirstCaptureTime));
                            break;
                        }
                    }
                }
                catch (Exception waitError)
                {
                    Util.Logger.Log(string.Format("인식 완료 대기 중 오류 {0}", waitError.Message));
                }
                Util.Logger.Log(string.Format("{0} capture Image Cnt {1}", LogCamIDX, CaptureCnt));
                ClsStructure.RegStruct ProcessReg = new ClsStructure.RegStruct();
                int FullRegIdx = -1;
                int PartRegIdx = -1;
                int NoRegIdx = -1;

                //자료 처리 여부 
                DateTime StartTime = DateTime.Now;

                //string[] AsIsPlateNo = new string[4] { string.Empty, string.Empty, string.Empty, string.Empty };

                string DatePath = string.Empty;
                if (CamEnv.CameraEnv.SockDataFormat.Equals((int)ClsStructure.SockFormat.Nexpa))
                    DatePath = string.Format(@"{0}\{1}\{2}", DateTime.Now.Year.ToString().PadLeft(4, '0'), DateTime.Now.Month.ToString().PadLeft(2, '0'), DateTime.Now.Day.ToString().PadLeft(2, '0'));
                else
                    DatePath = DateTime.Now.ToString("yyyyMMdd");

                int FullRegCnt = -1;
                int PartRegCnt = -1;
                int NoRegCnt = -1;

                for (int i = 0; i < CaptureCnt; i++)
                {
                    try
                    {
                        if (!Auth && RegArray[i].PlateNo != "No_Detection")
                        {
                            RegArray[i].PlateNo = clsFunction.MagicCarnum();
                        }
                        Util.Logger.Log(string.Format("{0} carno {1} img name {2} capture Time{3}", LogCamIDX, RegArray[i].PlateNo, RegArray[i].SourcePath, RegArray[i].FirstCaptureTime));
                        //미 인식
                        if (RegArray[i].PlateNo == "No_Detection")
                        {
                            if (NoRegIdx == -1)
                            {
                                NoRegIdx = i;
                                Util.Logger.Log(string.Format("{0} 미 인식 idx {1} 차량 번호 {2}", LogCamIDX, i, RegArray[i].PlateNo));
                            }
                            NoRegCnt++;
                            Util.Logger.Log(string.Format("{0} 미 인식 개수 증가 {1} 차량 번호 {2}", LogCamIDX, NoRegCnt, RegArray[i].PlateNo));
                        }
                        //부분 인식
                        else if (RegArray[i].PlateNo != null)
                        {
                            if (RegArray[i].PlateNo.Length < 7 || RegArray[i].PlateNo.ToUpper().IndexOf('X') > -1)
                            {
                                if (PartRegIdx == -1)
                                {
                                    PartRegIdx = i;
                                    Util.Logger.Log(string.Format("{0} 부분 인식 idx {1} 차량 번호 {2}", LogCamIDX, i, RegArray[i].PlateNo));
                                }
                                PartRegCnt++;
                                Util.Logger.Log(string.Format("{0} 부분 인식 개수 증가 {1} 차량 번호 {2}", LogCamIDX, PartRegCnt, RegArray[i].PlateNo));
                            }
                        }
                        //정 인식
                        else
                        {
                            if (FullRegIdx == -1)
                            {
                                FullRegIdx = i;
                                Util.Logger.Log(string.Format("{0} 정 인식 idx {1} 차량 번호 {2}", LogCamIDX, i, RegArray[i].PlateNo));
                            }
                            FullRegCnt++;
                            Util.Logger.Log(string.Format("{0} 정 인식 인식 개수 증가 {1} 차량 번호 {2}", LogCamIDX, FullRegCnt, RegArray[i].PlateNo));
                        }
                    }
                    catch (Exception regCheck_Error)
                    {
                        Util.Logger.Log(string.Format("regCheck_Error idx {0} {1}", i, regCheck_Error.Message));
                        if (NoRegIdx == -1)
                        {
                            NoRegIdx = i;
                        }
                        NoRegCnt++;
                    }
                }

                if (FullRegIdx > -1)
                    ProcessReg = RegArray[FullRegIdx];
                else if (PartRegIdx > -1)
                    ProcessReg = RegArray[PartRegIdx];
                else if (NoRegIdx > -1)
                    ProcessReg = RegArray[NoRegIdx];
                else
                    ProcessReg = RegArray[0];

                Util.Logger.Log(string.Format("{0} 인식 결과 {1} 인식속도: {2}ms Path {3}", LogCamIDX, ProcessReg.PlateNo, ProcessReg.term, ProcessReg.SourcePath));

                switch (ProcessReg.PlateNo)
                {
                    case null:
                    case "":
                        ProcessReg.PlateNo = "No_Detection";
                        break;
                }
                Util.Logger.Log("인식 정보 화면 출력");
                try
                {
                    if (Camidx == 0)
                    {
                        main.SetLabelText(main.lblCam1RegSpeed, String.Format("인식속도: {0}ms", ProcessReg.term));
                        main.SetLabelText(main.lblCam1RegResult, "인식결과: " + ProcessReg.PlateNo);
                    }
                    else
                    {
                        main.SetLabelText(main.lblCam2RegSpeed, String.Format("인식속도: {0}ms", ProcessReg.term));
                        main.SetLabelText(main.lblCam2RegResult, "인식결과: " + ProcessReg.PlateNo);
                    }
                    main.UpdateServerCard(Camidx, ProcessReg.PlateNo);   // 서버모드 카드에 차번 표시
                }
                catch (Exception DisPlayError)
                {
                    Util.Logger.Log(string.Format("DisPlayError {0}", DisPlayError.Message));
                }
                //이미지 저장
                string SourcePath = string.Empty;
                string TargetPath = string.Empty;
                if (CamEnv.CameraEnv.ImageSave.EtcSave)
                {

                    if (!Directory.Exists(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.EtcPath, DatePath)))
                        Directory.CreateDirectory(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.EtcPath, DatePath));
                    foreach (ClsStructure.RegStruct item in RegArray)
                    {
                        if (File.Exists(item.SourcePath))
                        {
                            try
                            {
                                //20161124 Start
                                if (File.Exists(Directory.GetCurrentDirectory() + "\\Back\\" + item.SourcePath))
                                {
                                    SourcePath = Directory.GetCurrentDirectory() + "\\Back\\" + item.SourcePath;
                                    Util.Logger.Log(LogCamIDX + " From Back Folder Image");
                                }
                                else if (File.Exists(Directory.GetCurrentDirectory() + item.SourcePath))
                                {
                                    SourcePath = item.SourcePath;
                                    Util.Logger.Log(LogCamIDX + " From Local Path Image");
                                }
                                TargetPath = string.Format("{0}\\{1}\\{2}_{3}_{4}", CamEnv.CameraEnv.ImageSave.EtcPath, DatePath, CamInfo.ChName, item.PlateNo, item.SourcePath.Substring(CamInfo.ChName.Length));
                                Util.Logger.Log(Util.Logger.Log_Level.Event_Log,
                                    string.Format("{0} 기타 이미지 저장 Source {1} Target {2} file size : {3}", LogCamIDX, SourcePath, TargetPath, item.Size));
                                clsFunction.SaveImage(SourcePath, TargetPath,
                                        item.Roi, item.Exposure.ToString(), item.PlateNo);
                                // ParkingWeb 이미지 서버 업로드 (fire-and-forget) — 기타 이미지
                                clsImageUploader.Enqueue(TargetPath, DatePath,
                                    string.Format("{0}_{1}_{2}", CamInfo.ChName, item.PlateNo, item.SourcePath.Substring(CamInfo.ChName.Length)));
                                //20161124 End
                                //Util.Logger.Log(Util.Logger.Log_Level.Event_Log,
                                //    string.Format("{0} 기타 이미지 저장 Source {1} Target {2}\\{3}\\{4}_{5}_{6} file size : {7}", LogCamIDX, item.SourcePath, CamEnv.CameraEnv.ImageSave.EtcPath,
                                //    DatePath, CamInfo.ChName, item.PlateNo, item.SourcePath.Substring(CamInfo.ChName.Length), item.Size));
                                //clsFunction.SaveImage(item.SourcePath, TargetPath,
                                //        item.Roi, item.Exposure.ToString(), item.PlateNo);
                            }
                            catch (Exception ETCSaveError)
                            {
                                Util.Logger.Log(Util.Logger.Log_Level.Event_Log, string.Format("ETCSaveError {0}", ETCSaveError.Message));
                            }
                        }
                    }
                }
#if SaveBigSize
                Util.Logger.Log(string.Format("{0} 이미지 저장 시 사이즈 큰 것 저장", LogCamIDX));
                long bigsize = -1;
                int sizeidx = 0;
                for (int i = 0; i < CaptureCnt; i++)
                {
                    Util.Logger.Log(string.Format("{0} {1} 파일 사이즈 {2}", LogCamIDX, RegArray[i].SourcePath, RegArray[i].Size));
                    //if (RegArray[i].PlateNo != "No_Detection")
                    //{
                    //20161124 Start
                    if (File.Exists(RegArray[i].SourcePath) || File.Exists(Directory.GetCurrentDirectory() + "\\Back\\" + RegArray[i].SourcePath))
                    {
                        if (bigsize < RegArray[i].Size)
                        {
                            bigsize = RegArray[i].Size;
                            sizeidx = i;
                        }
                    }
                    //20161124 End(if 문)
                    //}
                }
                string Fname = string.Empty;
                //20161124 Start
                //if (sizeidx == -1)
                //{
                //    Util.Logger.Log(string.Format("{0} 전체 미인식 {1} 파일 사이즈 {2}", LogCamIDX, ProcessReg.SourcePath, ProcessReg.Size));
                //    Fname = ProcessReg.SourcePath;
                //}
                //else
                //{
                //    Fname = RegArray[sizeidx].SourcePath;
                //    ProcessReg.SourcePath = Fname;
                //}
                if (File.Exists(Directory.GetCurrentDirectory() + "\\Back\\" + RegArray[sizeidx].SourcePath))
                {
                    Fname = Directory.GetCurrentDirectory() + "\\Back\\" + RegArray[sizeidx].SourcePath;
                    ProcessReg.SourcePath = Fname;
                }
                else if (File.Exists(RegArray[sizeidx].SourcePath))
                {
                    Fname = RegArray[sizeidx].SourcePath;
                    ProcessReg.SourcePath = Fname;
                }
                //Util.Logger.Log(string.Format("{0} image Save {1} to {2}", LogCamIDX, Fname, string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg",
                //    CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, Fname.Substring(CamInfo.ChName.Length, 14))));
                //if (!Directory.Exists(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath)))
                //    Directory.CreateDirectory(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath));
                //clsFunction.SaveImage(Fname,
                //                string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, Fname.Substring(CamInfo.ChName.Length, 14)),
                //                ProcessReg.PlateRoi, ProcessReg.Exposure.ToString(), ProcessReg.PlateNo);

                Util.Logger.Log(string.Format("{0} image Save {1} to {2}", LogCamIDX, Fname, string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg",
                    CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, Path.GetFileName(Fname).Substring(CamInfo.ChName.Length, 14))));
                if (!Directory.Exists(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath)))
                    Directory.CreateDirectory(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath));
                clsFunction.SaveImage(Fname,
                                string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, Path.GetFileName(Fname).Substring(CamInfo.ChName.Length, 14)),
                                ProcessReg.PlateRoi, ProcessReg.Exposure.ToString(), ProcessReg.PlateNo);
                // ParkingWeb 이미지 서버 업로드 (fire-and-forget) — BigSize 분기
                {
                    string _upName = string.Format("{0}_{1}_{2}.jpg", CamInfo.ChName, ProcessReg.PlateNo, Path.GetFileName(Fname).Substring(CamInfo.ChName.Length, 14));
                    string _upFull = string.Format("{0}\\{1}\\{2}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, _upName);
                    clsImageUploader.Enqueue(_upFull, DatePath, _upName);
                }
                string cartype = "";
                string rate = "";
                int irate = 0;
                //string[] SmallCar = new string[5] { "MATIZ", "SPARK", "MORNING", "RAY", "CLICK" };
                if (!string.IsNullOrEmpty(ProcessReg.CarType))
                {
                    //"__Genesis/G90/2018"
                    cartype = ProcessReg.CarType.Split('/')[1].ToUpper();
                    rate = ((int)ProcessReg.Confidence / 10 * 10).ToString();
                    int smallrate = ((int)ProcessReg.Confidence / 10 * 10);
                    ClsStructure.SmallCarRate fitem = CamEnv.CameraEnv.RegCarRate.Find(x => x.CarType == cartype);
                    if (!Directory.Exists(string.Format("{0}\\CarModel\\{1}\\{2}", CamEnv.CameraEnv.ImageSave.SavePath, cartype, rate)))
                        Directory.CreateDirectory((string.Format("{0}\\CarModel\\{1}\\{2}", CamEnv.CameraEnv.ImageSave.SavePath, cartype, rate)));
                    File.Copy(Fname, string.Format("{0}\\CarModel\\{1}\\{2}\\{3}_{4}", CamEnv.CameraEnv.ImageSave.SavePath, cartype, rate, (int)ProcessReg.Confidence, Path.GetFileName(ProcessReg.SourcePath)), true);

                    Util.Logger.Log(string.Format("차종명 {0} {1}%", cartype, rate));
                    if (!string.IsNullOrWhiteSpace(fitem.CarType))
                    {
                        if (smallrate >= fitem.Rate)
                        {
                            Util.Logger.Log("경차 처리");
                            if (Camidx == 0 && CamEnv.CameraEnv.IPCamera1Info.DioInPut.SmallCar)
                                irate = CamEnv.CameraEnv.IPCamera1Info.DioInPut.SmallPort;
                            else if (Camidx == 1 && CamEnv.CameraEnv.IPCamera2Info.DioInPut.SmallCar)
                                irate = CamEnv.CameraEnv.IPCamera2Info.DioInPut.SmallPort;
                        }
                    }
                }
                ProcessReg.SourcePath = Path.GetFileName(ProcessReg.SourcePath);
                //20161124 End
#else
                Util.Logger.Log(string.Format("CamIDX {0} image Save {1} to {2}", Camidx, ProcessReg.SourcePath, 
                string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(CamInfo.ChName.Length, 14))));
                if (!Directory.Exists(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath)))
                    Directory.CreateDirectory(string.Format("{0}\\{1}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath));
                Util.Logger.Log("CamIDX " + Camidx + " 캡쳐 이미지 저장");
                clsFunction.SaveImage(ProcessReg.SourcePath,
                                string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(CamInfo.ChName.Length, 14)),
                                ProcessReg.PlateRoi, ProcessReg.Exposure.ToString(), ProcessReg.PlateNo);
                // ParkingWeb 이미지 서버 업로드 (fire-and-forget) — 기본 분기
                {
                    string _upName = string.Format("{0}_{1}_{2}.jpg", CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(CamInfo.ChName.Length, 14));
                    string _upFull = string.Format("{0}\\{1}\\{2}", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, _upName);
                    clsImageUploader.Enqueue(_upFull, DatePath, _upName);
                }
#endif
                if (Camidx == 0)
                    main.Path1 = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(CamInfo.ChName.Length, 14));
                else
                    main.Path2 = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(CamInfo.ChName.Length, 14));
                //Console.WriteLine("Socket Send");
                // [동작모드] CAM(카메라서버) 모드에서도 자료처리 수행 — 전 모드 공통 DataProcess
                // (DataProcess 내부에서 설정(LprOpt.Normal/Period_SendData)에 따라 요금계산기 소켓 전송도 수행)
                {
                    // 공유락으로 직렬화 — 서버캠(ProcessServerCamResult)과 동일 락. 단일 DB연결(TCon) 동시사용 방지.
                    string Log;
                    lock(clsDataTransaction.ProcLock) {
                        Log = main.DataProcess.DataProcess(LprInfo.InOutType, CamEnv, Camidx, ProcessReg.PlateNo.ToString(),
                            string.Format("{0}_{1}_{2}.jpg", CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(CamInfo.ChName.Length, 14)),
                            ProcessReg.FirstCaptureTime, irate);
                        try { main.UpdateServerCardType(Camidx, ProcessReg.PlateNo, main.DataProcess.GetRegedCar(Camidx)); } catch { }   // 카드 일반/정기 태그
                    }
                    Util.Logger.Log(string.Format("{0} Log Msg : {1}", LogCamIDX, Log));

                    Thread.Sleep(1000);
                    if (frm != null)
                    {
                        bool imgDp = false;
                        frmLogging(string.Format("{4} {0} {1} 소요시간 {2}ms 차량번호 {3}", DateTime.Now.ToString("HH:mm:ss"), CamInfo.ChName, ProcessReg.term.ToString().PadLeft(5, ' '), ProcessReg.PlateNo, LogCamIDX));
                        string[] sp = Log.Split('\n');
                        foreach (string item in sp)
                        {
                            frmLogging(string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), item));
                            if (frm.pictureBox1.Image != null)
                                frm.pictureBox1.Image = null;
                            if (!imgDp)
                            {
                                imgDp = true;
                                string fname = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", CamEnv.CameraEnv.ImageSave.SavePath, DatePath, CamInfo.ChName, ProcessReg.PlateNo, ProcessReg.SourcePath.Substring(0, 14));
                                if (File.Exists(fname))
                                {
                                    // [수정] 이전 BackgroundImage Dispose + Image.FromFile 파일 락 회피
                                    // (기존: 매 차량 인식마다 Image/GDI 핸들 + 파일 핸들 누적 누수 → 장시간 운영 시 카메라 끊김/PC 저하)
                                    if (Camidx == 0)
                                    {
                                        Image oldBg = frm.pictureBox1.BackgroundImage;
                                        frm.pictureBox1.BackgroundImage = clsFunction.LoadImageNoLock(fname);
                                        if (oldBg != null) oldBg.Dispose();
                                    }
                                    else
                                    {
                                        Image oldBg = frm.pictureBox2.BackgroundImage;
                                        frm.pictureBox2.BackgroundImage = clsFunction.LoadImageNoLock(fname);
                                        if (oldBg != null) oldBg.Dispose();
                                    }
                                }
                            }
                        }
                    }
                    frmLprMain.Main.FullCheck();
                }
                Util.Logger.Log(string.Format("{0} File Delete", LogCamIDX));
                DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
                FileInfo[] rgFiles = di.GetFiles(CamInfo.ChName + "*.jpg");
                foreach (FileInfo fi in rgFiles)
                {
                    try
                    {
                        //if ((DateTime.Now - fi.CreationTime).TotalSeconds > 5)
                        Util.Logger.Log(string.Format("{0} File Delete {1}", LogCamIDX, fi.Name));
                        fi.Delete();
                    }
                    catch (Exception FileDelError)
                    {
                        Util.Logger.Log(string.Format("{0} File Delete {1} Error", LogCamIDX, fi.Name, FileDelError));
                    }
                }
                di = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\Back");
                rgFiles = di.GetFiles(CamInfo.ChName + "*.jpg");
                foreach (FileInfo fi in rgFiles)
                {
                    try
                    {
                        if ((DateTime.Now - fi.CreationTime).TotalDays > 7)
                        {
                            Util.Logger.Log(string.Format("{0} Back File Delete {1}", LogCamIDX, fi.Name));
                            fi.Delete();
                        }
                    }
                    catch (Exception FileDelError)
                    {
                        Util.Logger.Log(string.Format("{0} Back File Delete {1} Error", LogCamIDX, fi.Name, FileDelError));
                    }
                }
            }
            catch (Exception AfterRegPlateCam_Error)
            {
                Util.Logger.Log(string.Format("{2} AfterRegPlateCam_Error Cam{0} {1}", Camidx + 1, AfterRegPlateCam_Error.Message, LogCamIDX));
            }
            Util.Logger.Log(string.Format("{0} AfterRegPlateCam End", LogCamIDX));

            if (Camidx == 0)
                RegArray1 = new ClsStructure.RegStruct[4];
            else
                RegArray2 = new ClsStructure.RegStruct[4];

            if (Camidx == 0)
                clsThread.Cam1RegCnt = 0;
            else if (Camidx == 1)
                clsThread.Cam2RegCnt = 0;
        }

        //NgisWay Module
        // [수정] new Thread → ThreadPool — 트리거마다 OS 스레드 생성/스택 1MB 할당하던 부담 제거
        // (장시간 운영 시 스레드 폭증 → 컨텍스트 스위칭으로 PC 저하)
        public static void RegPlateNoNgisWay(int camindex, ClsStructure.RegStruct dr)
        {
            ThreadPool.QueueUserWorkItem(delegate(object _)
            {
                try { main.NgisWay.RegPlate(1, dr); }
                catch (Exception ex) { Util.Logger.Log("RegPlateNoNgisWay 예외: " + ex.Message); }
            });
        }

        public static void RegPlateNoNgisWay(int camindex, int idx)
        {
            ThreadPool.QueueUserWorkItem(delegate(object _)
            {
                try { main.NgisWay.RegPlate(camindex, idx); }
                catch (Exception ex) { Util.Logger.Log("RegPlateNoNgisWay 예외: " + ex.Message); }
            });
        }

        private static void frmLogging(string msg)
        {
            if (frm == null) return;
            try
            {
                frm.listBox1.Items.Add(msg);
                frm.listBox1.SelectedIndex = frm.listBox1.Items.Count - 1;
            }
            catch (Exception) { }
        }

        public static void RegPlateNoElwox(int camindex, int idx)
        {
            // [수정] new Thread → ThreadPool (동일 사유)
            ThreadPool.QueueUserWorkItem(delegate(object _)
            {
                try { regPlate(camindex, idx); }
                catch (Exception ex) { Util.Logger.Log("RegPlateNoElwox 예외: " + ex.Message); }
            });
        }

        private static void regPlate(int Camindex, int idx)
        {
            uint regid = main.getRegID(Camindex);
            uint result = 0;

            Util.Logger.Log(string.Format("CAM{0} regPlate 시작 RegId {1} 인식 완료", Camindex + 1, regid));
            ELANPRESULT epr = new ELANPRESULT();
            Stopwatch sw = new Stopwatch();

            try
            {
                sw.Reset();
                sw.Start();
                ClsStructure.RegStruct RegArray = new ClsStructure.RegStruct();
                switch (Camindex)
                {
                    case 0:
                        RegArray = RegArray1[idx];
                        break;
                    case 1:
                        RegArray = RegArray2[idx];
                        break;
                }

                ElanprPlateCandidates candis = new ElanprPlateCandidates();
                Rect rcPlateLoc = new Rect();
                string[] sp = RegArray.Roi.Split(',');
                rcPlateLoc.left = Util.Function.IntTryParse(sp[0]);
                rcPlateLoc.top = Util.Function.IntTryParse(sp[1]);
                rcPlateLoc.right = Util.Function.IntTryParse(sp[2]);
                rcPlateLoc.bottom = Util.Function.IntTryParse(sp[3]);

                Elanpr.Elanpr_SetMinMaxNumberPix(regid, 25, 75);
                Elanpr.Elanpr_SetPlateLocation(regid, rcPlateLoc);
                result = Elanpr.Elanpr_DoesExistNumberPlate(regid, RegArray.SourcePath, 25, 75);
                if (result.Equals(0))
                {
                    if (candis.nNumCandis > 1)
                    {
                        rcPlateLoc.left += Convert.ToInt16(candis.rcPlateCandis[4]);
                        rcPlateLoc.top += Convert.ToInt16(candis.rcPlateCandis[5]);
                        rcPlateLoc.right = Convert.ToInt16(candis.rcPlateCandis[6]) + Util.Function.IntTryParse(sp[0]);
                        rcPlateLoc.bottom = Convert.ToInt16(candis.rcPlateCandis[7]) + Util.Function.IntTryParse(sp[1]);

                        //Function.WriteLog(string.Format("THREAD ID {0} 번호판 절대 좌표 변환 Left:{1} Top:{2} Right:{3} Bottom:{4}", tid, rcPlateLoc.left, rcPlateLoc.top, rcPlateLoc.right, rcPlateLoc.bottom), @"Camera_" + DateTime.Now.ToShortDateString() + ".txt");
                        //result = Elanpr.Elanpr_SetPlateLocation(uEngineID, rcPlateLoc);
                        //Function.WriteLog(string.Format("THREAD ID {0} 좌표 설정", tid), @"Camera_" + DateTime.Now.ToShortDateString() + ".txt");
                        //result = Elanpr.Elanpr_RecognizePlate(uEngineID, path, ref epr);
                        //Function.WriteLog(string.Format("THREAD ID {0} 인식 완료", tid), @"Camera_" + DateTime.Now.ToShortDateString() + ".txt");

                        result = Elanpr.Elanpr_SetPlateLocation(regid, rcPlateLoc);
                        result = Elanpr.Elanpr_RecognizePlate(regid, RegArray.SourcePath, ref epr);
                    }

                    if (epr.strPlateNumber == null || epr.strPlateNumber == string.Empty)
                    {
                        rcPlateLoc.left = Util.Function.IntTryParse(sp[0]);
                        rcPlateLoc.top = Util.Function.IntTryParse(sp[1]);
                        rcPlateLoc.right = Util.Function.IntTryParse(sp[2]);
                        rcPlateLoc.bottom = Util.Function.IntTryParse(sp[3]);

                        result = Elanpr.Elanpr_SetPlateLocation(regid, rcPlateLoc);
                        result = Elanpr.Elanpr_RecognizePlate(regid, RegArray.SourcePath, ref epr);
                    }
                }
                sw.Stop();
                RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", rcPlateLoc.left, rcPlateLoc.top, rcPlateLoc.right, rcPlateLoc.bottom);
                RegArray.term = sw.ElapsedMilliseconds;
                if (string.IsNullOrEmpty(epr.strPlateNumber))
                    RegArray.PlateNo = "No_Detection";
                else
                    RegArray.PlateNo = epr.strPlateNumber;

                switch (Camindex)
                {
                    case 0:
                        RegArray1[idx] = RegArray;
                        break;
                    case 1:
                        RegArray2[idx] = RegArray;
                        break;
                }
            }
            catch (AccessViolationException)
            { }
            catch (Exception)
            { }
            Util.Logger.Log(string.Format("CAM{0} RegId {1} 인식 완료", Camindex + 1, regid));
            main.ReleaseRegID(regid);

            if (Camindex == 0)
                Cam1RegCnt++;
            else if (Camindex == 1)
                Cam2RegCnt++;
        }
    }
}
