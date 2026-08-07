using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace KyungsinLPR {
    /// <summary>
    /// Evo <b>6버전</b>(구형 EvoEngineSDK) 인식 경로.
    ///
    /// 전제: 한 LPR PC에는 EvoEngineSDK가 <b>V6 또는 V7 중 하나만</b> 설치된다(동시 사용 없음).
    /// 따라서 엔진 DLL(EvoCSAPI.dll + native EvoCAPI/EvoEngine/openvino…)은 <b>실행폴더(루트)에 버전에 맞게 1세트만</b> 둔다.
    /// V6 PC → v6 DLL을 루트에 복사 후 환경설정 V6 선택. V7 PC → v7 DLL을 루트에 두고 V7 선택.
    ///
    /// 신형 <see cref="CoreLogic"/>(v7)은 컴파일타임 v7 API로 직접 호출한다. 이 클래스는 v6 API가
    /// 핸들타입(IntPtr)·메서드명(GetNumber/GetString)·초기화 시그니처(ccodes 4인자)까지 달라,
    /// <b>루트에 로드된 EvoCSAPI.dll을 리플렉션</b>으로 호출한다(컴파일타임 v7 참조에 묶이지 않도록).
    /// V6 선택 시 v7 CoreLogic은 호출되지 않으므로 v7 API 미스매치 문제는 없다.
    /// </summary>
    public static class CoreLogicV6 {
        private static readonly object obj = new object();
        private static Assembly _asm;
        private static Type tFunc, tSSE, tLPI, tEngine, tDDI, tRect;
        private static readonly IntPtr[] handSSE = new IntPtr[2] { IntPtr.Zero, IntPtr.Zero };
        private static bool _ready = false;

        public static bool IsReady { get { return _ready; } }

        /// <summary>v6 엔진 Data 경로: 실행폴더\Data 우선, 없으면 설치 SDK Data(현장 v6 모델/라이선스).</summary>
        private static string GetEvoDataDir() {
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (Directory.Exists(local) && File.Exists(Path.Combine(local, "EvoEngine.lic")))
                return local;
            return @"C:\Program Files\EvoEngineSDK\Data";
        }

        /// <summary>루트에 로드(또는 로드 가능)된 EvoCSAPI 어셈블리를 가져온다.</summary>
        private static Assembly GetEvoAssembly() {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                if (a.GetName().Name == "EvoCSAPI") return a;
            return Assembly.Load("EvoCSAPI");   // 실행폴더의 EvoCSAPI.dll 로드
        }

        public static bool Initialize() {
            try {
                lock (obj) {
                    if (_ready) return true;

                    _asm = GetEvoAssembly();
                    tFunc = _asm.GetType("Evo.Func");
                    tSSE = _asm.GetType("Evo.SSEngine");
                    tLPI = _asm.GetType("Evo.LPI");
                    tEngine = _asm.GetType("Evo.Engine");
                    tDDI = _asm.GetType("Evo.DDI");
                    tRect = _asm.GetType("Evo.Rect");
                    if (tFunc == null || tSSE == null || tLPI == null || tEngine == null || tRect == null) {
                        Util.Logger.Log("[CoreLogicV6] EvoCSAPI 타입 로드 실패 (실행폴더 EvoCSAPI.dll 확인)");
                        return false;
                    }

                    // 로드된 래퍼가 실제로 V6(구형 API)인지 확인 — Func.Initialize 인자수 v6=4 / v7=3
                    MethodInfo miInit = tFunc.GetMethod("Initialize");
                    if (miInit == null || miInit.GetParameters().Length != 4) {
                        int pc = (miInit == null) ? -1 : miInit.GetParameters().Length;
                        Util.Logger.Log("[CoreLogicV6] 실행폴더 EvoCSAPI.dll이 V6가 아닙니다 (Func.Initialize 인자수=" + pc
                            + ", V6=4). → 이 PC에 V6 엔진 DLL(EvoCSAPI.dll+native)을 실행폴더에 복사하고 다시 실행하세요.");
                        return false;
                    }

                    string language = (CoreLogic.cc == CoreLogic.THA) ? "THA" : "KOR";
                    string dataDir = GetEvoDataDir();
                    Util.Logger.Log("[CoreLogicV6] Evo 6버전 초기화 시작 — DataDir=" + dataDir + " lang=" + language);

                    // Func.Initialize(string ccodes, string drmd, string dataDir, out IntPtr ctxDDI)
                    object[] ia = new object[] { language, null, dataDir, IntPtr.Zero };
                    int rc = (int)miInit.Invoke(null, ia);
                    if (rc != 0) {
                        Util.Logger.Log("[CoreLogicV6] Func.Initialize 실패 rc=" + rc);
                        return false;
                    }
                    IntPtr ctxDDI = (IntPtr)ia[3];
                    try { tDDI.GetMethod("Free").Invoke(null, new object[] { ctxDDI }); } catch { }

                    // SSEngine 1개 생성해 두 채널이 공유 (인식은 lock(obj)로 직렬화 → 1 인스턴스로 충분).
                    //   2개 만들면 라이선스 인스턴스 초과(rc=106 EVORC_LIC_OVER_MAX_INST) 발생 가능.
                    // ddd(DNN Device Descriptor) = "DMDF:Device" 형식. v6는 null 불가(rc=50 EVORC_INV_ARG). 기본 "FP32:CPU".
                    string ddd = GetV6Ddd();
                    MethodInfo mCreate = tSSE.GetMethod("Create", Type.EmptyTypes);
                    MethodInfo mInit = tSSE.GetMethod("Init", new[] { typeof(IntPtr), typeof(string), typeof(string) });
                    IntPtr h = (IntPtr)mCreate.Invoke(null, null);
                    if (h == IntPtr.Zero) {
                        Util.Logger.Log("[CoreLogicV6] SSEngine.Create 실패 rc=" + GetLastRC());
                        return false;
                    }
                    int irc = (int)mInit.Invoke(null, new object[] { h, language, ddd });
                    if (irc != 0 && ddd != "FP32:CPU") {
                        Util.Logger.Log("[CoreLogicV6] SSEngine.Init ddd=" + ddd + " 실패 rc=" + irc + " → FP32:CPU 재시도");
                        irc = (int)mInit.Invoke(null, new object[] { h, language, "FP32:CPU" });
                        if (irc == 0) ddd = "FP32:CPU";
                    }
                    if (irc != 0) {
                        Util.Logger.Log("[CoreLogicV6] SSEngine.Init 실패 rc=" + irc + " ddd=" + ddd);
                        return false;
                    }
                    handSSE[0] = h;
                    handSSE[1] = h;   // 공유
                    Util.Logger.Log("[CoreLogicV6] SSEngine.Init OK (2채널 공유 1엔진) ddd=" + ddd);
                    _ready = true;
                    Util.Logger.Log("[CoreLogicV6] 초기화 완료 (Evo 6버전 로컬 인식)");
                    return true;
                }
            } catch (Exception ex) {
                Util.Logger.Log("[CoreLogicV6] Initialize 예외: " + ex);
                return false;
            }
        }

        private static int GetLastRC() {
            try { return (int)tFunc.GetMethod("GetLastRC").Invoke(null, null); } catch { return -1; }
        }

        /// <summary>환경설정 CoreType → v6 DNN Device Descriptor("DMDF:Device"). 기본 FP32:CPU. MYRIAD는 FP16만 지원.</summary>
        private static string GetV6Ddd() {
            try {
                switch ((ClsStructure.CoreType)frmLprMain.ENV.CameraEnv.CoreType) {
                    case ClsStructure.CoreType.GPU: return "FP32:GPU";
                    case ClsStructure.CoreType.MyriadVPU: return "FP16:MYRIAD";
                    default: return "FP32:CPU";
                }
            } catch { return "FP32:CPU"; }
        }

        /// <summary>v7 <c>CoreLogic.Reg</c>과 동일 시그니처 — 백그라운드 스레드로 인식.</summary>
        public static void Reg(int camIndex, int idx, bool bRegCarType) {
            Thread t = new Thread(delegate () { RegPlateNo(camIndex, idx); });
            t.IsBackground = true;
            t.Start();
        }

        private static void RegPlateNo(int camIndex, int idx) {
            try {
                if (!_ready) { Util.Logger.Log("[CoreLogicV6] 미초기화 상태 — 인식 생략"); return; }
                ClsStructure.RegStruct RegArray = (camIndex == 0) ? clsThread.RegArray1[idx] : clsThread.RegArray2[idx];
                RegArray.PlateNo = "";
                RegArray.CarType = "";
                var roi = (camIndex == 0)
                    ? frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi
                    : frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi;
                DateTime dt = DateTime.Now;
                lock (obj) {
                    // searchRect(ROI) 설정 — Engine.GetParamSearchRect(out Rect) → 좌표 채움 → SetParamSearchRect(ref Rect)
                    object rect = Activator.CreateInstance(tRect);
                    object[] ga = new object[] { handSSE[idx], rect };
                    tEngine.GetMethod("GetParamSearchRect").Invoke(null, ga);
                    rect = ga[1];
                    SetRectField(rect, "x", roi.X);
                    SetRectField(rect, "y", roi.Y);
                    SetRectField(rect, "width", roi.Width);
                    SetRectField(rect, "height", roi.Height);
                    tEngine.GetMethod("SetParamSearchRect").Invoke(null, new object[] { handSSE[idx], rect });

                    // SSEngine.Run(IntPtr, string) → IntPtr ctxLPI
                    MethodInfo mRun = tSSE.GetMethod("Run", new[] { typeof(IntPtr), typeof(string) });
                    IntPtr ctxLPI = (IntPtr)mRun.Invoke(null, new object[] { handSSE[idx], RegArray.SourcePath });
                    RegArray.term = (long)(DateTime.Now - dt).TotalMilliseconds;

                    if (ctxLPI != IntPtr.Zero) {
                        object[] na = new object[] { ctxLPI, (uint)0 };
                        tLPI.GetMethod("GetNumber").Invoke(null, na);
                        uint num = (uint)na[1];
                        int maxSize = 0;
                        for (uint i = 0; i < num; i++) {
                            StringBuilder sb = new StringBuilder(512);
                            tLPI.GetMethod("GetString").Invoke(null, new object[] { ctxLPI, i, sb });
                            object rb = Activator.CreateInstance(tRect);
                            object[] pa = new object[] { ctxLPI, i, rb };
                            tLPI.GetMethod("GetPosition").Invoke(null, pa);
                            rb = pa[2];
                            int x = GetRectField(rb, "x"), y = GetRectField(rb, "y");
                            int w = GetRectField(rb, "width"), hh = GetRectField(rb, "height");
                            int size = w * hh;
                            if (size > maxSize) {
                                maxSize = size;
                                RegArray.PlateNo = sb.ToString();
                                RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", x, y, w, hh);
                            } else {
                                Util.Logger.Log("[CoreLogicV6] X 작은 사이즈 인식 결과 버림 : " + sb);
                            }
                        }
                        // Engine.FreeLPI(IntPtr hSSE, ref IntPtr ctxLPI)
                        try { tEngine.GetMethod("FreeLPI").Invoke(null, new object[] { handSSE[idx], ctxLPI }); } catch { }
                    } else {
                        Util.Logger.Log("[CoreLogicV6] SSEngine.Run 실패 rc=" + GetLastRC() + " path=" + RegArray.SourcePath);
                        RegArray.PlateNo = "No_Detection";
                        RegArray.PlateRoi = "0,0,0,0";
                    }

                    if (camIndex == 0) clsThread.RegArray1[idx] = RegArray;
                    else clsThread.RegArray2[idx] = RegArray;
                    Util.Logger.Log(string.Format("[CoreLogicV6] 인식 결과 : {0} {1}", RegArray.PlateNo, RegArray.PlateRoi));
                }
            } catch (Exception ex) {
                Util.Logger.Log("[CoreLogicV6] RegPlateNo 예외: " + ex.Message);
            }
        }

        public static void Release() {
            try {
                lock (obj) {
                    if (!_ready) return;
                    IntPtr h = handSSE[0];   // 2채널 공유 1엔진 — 1번만 해제
                    if (h != IntPtr.Zero) {
                        try { tSSE.GetMethod("Deinit").Invoke(null, new object[] { h }); } catch { }
                        try { tSSE.GetMethod("Destroy").Invoke(null, new object[] { h }); } catch { }
                    }
                    handSSE[0] = IntPtr.Zero;
                    handSSE[1] = IntPtr.Zero;
                    _ready = false;
                }
            } catch { }
        }

        private static void SetRectField(object rect, string f, int v) {
            FieldInfo fi = tRect.GetField(f);
            if (fi != null) fi.SetValue(rect, Convert.ChangeType(v, fi.FieldType));
        }

        private static int GetRectField(object rect, string f) {
            FieldInfo fi = tRect.GetField(f);
            return fi != null ? Convert.ToInt32(fi.GetValue(rect)) : 0;
        }
    }
}
