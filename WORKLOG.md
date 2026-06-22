# KukjeLPR (KyungsinLPR) 작업 로그

> 새로운 세션에서 Claude가 이 파일을 먼저 읽고 이어서 작업할 수 있도록, 수정 사항·결정·환경·다음 할 일을 누적 기록합니다.
> 날짜는 절대 날짜로만 기록 (YYYY-MM-DD).

---

## 🔖 세션 복원용 스냅샷 (최근 갱신: 2026-06-11)

**프로젝트 위치**: `D:\LPR-소스\LPRCAM\KukjeLPR`
**프로젝트 형식**: .NET Framework 4.7.2 C# WinForms (`KyungsinLPRx64.sln` / x64 메인)
**빌드 도구**: Visual Studio (msbuild 직접 호출보다 IDE 빌드 안정적)
**빌드 산출물**: `bin\x64\Release\KyungsinLPRx64.exe` (AssemblyName=KyungsinLPRx64)

**현재 집중 모듈**: USB(DirectShow/UVC) 카메라 2대 영상 표시 — `m_camera{1,2}_usb`

**참조 SDK**:
- `D:\1\relay_sdk\sdk_v2_0_0` (Dingtian 이더넷 릴레이 보드 — HTTP/TCP/UDP/Modbus/MQTT)
- AForge.NET 2.2.5 (USB 카메라 캡처) — `dll\AForge.{Video,Video.DirectShow}.dll`

---

## 진행 중 기능 (2026-06-04 ~ 2026-06-11)

### USB 카메라 2대 영상 표시·인식 통합

iNova 노비텍 IP카메라 외에 **DirectShow/UVC USB 카메라**를 카메라1·2 각각 독립적으로 선택 가능하게 통합.

#### 데이터 모델 — `Class/ClsStructure.cs`
- `enum CameraSourceType { Default=0, iNova1=1, iNova2=2, USB=3 }`
- `struct IPCamera_Basic_Setting`에 신규 필드:
  - `int CameraSource` — 카메라별 소스 (Default면 전역 iNovaType 적용 — 하위호환)
  - `string UsbMoniker` — DirectShow MonikerString (예: `@device:pnp:\\?\usb#vid_xxxx...`)
  - `string UsbDeviceName` — 표시용 (예: `"OBSBOT Tiny 4K"`)
  - `int UsbResolutionWidth`, `int UsbResolutionHeight` — 0이면 카메라 기본값

#### 신규 클래스 — `Class/USB/USBCamera.cs`
AForge `VideoCaptureDevice` 래퍼, **IPCamera와 동일한 메서드 시그니처**로 노출:
- `Init(moniker, name, w, h)` / `ConnectStreamPort()` / `DisconnectStreamPort()` / `IsStreamPortConnected()` / `IsCommandPortConnected()` (USB는 스트림=커맨드 동일)
- `GetImage(timeoutMs, out Bitmap)` — `requestTime` 이후 새 프레임 대기 → 없으면 1초 이내 마지막 프레임 폴백
- `SaveLastImage(path)` — 최신 프레임 클론 저장 (.jpg/.bmp/.png 자동 감지)
- `OnNewFrame` 이벤트 (외부 라이브 프리뷰용, 현재 미구독)
- 정적 `ListDevices()`, `ListResolutions(moniker)` — 환경설정 화면에서 사용
- `ResetCamera()` — stub (USB는 카메라 측 리셋 불가)
- **락 패턴**: `_frameLock` 짧게 보유, `_latestFrame` 갱신 시 이전 프레임 Dispose

#### 메인 폼 통합 — `Form/frmLprMain.cs`
- 멤버 추가: `m_camera{1,2}_usb` (29~30행)
- `StartGrabLoop{1,2}()` (147, 160행): `IsUsbCam(N)` 우선 분기 → `GrabLoop{N}_USB`
- `StartCamera()` (1437~):
  - `bothUsbCams` 판단 후 카메라2 USB는 `Thread.Sleep(2500)` 후 시작 — DirectShow isoch endpoint 경합 회피
  - USB 카메라는 `StartCamera_USB(N)`로 연결만 (그랩스레드 X), 그랩 스레드는 `StartCamera_iNova1/2()` 안의 `if(IsUsbCam(N) && Use) StartGrabLoop{N}()` 경유로 시작
- `StartCamera_iNova{1,2}()` (1467, 1485행): `!IsUsbCam(N)` 가드로 iNova ConnectStreamPort 스킵
- `StopCamera()` (1514~1515행): USB Disconnect 명시
- 노출 제어 스레드 (`tExposure`, 1349~1360행): `IsUsbCam(1) && IsUsbCam(2)`이면 생성 안 함 (둘 다 USB면 자동노출)
- Watchdog (`ImageSaveTermCheck`, 4844~4884행): USB 그랩 스레드 죽으면 `StartGrabLoop{N}()` 재기동

#### partial 분할 — `Form/frmLprMain.Usb.cs`
- `IsUsbCam(chIdx)` / `GetCamSource(chIdx)` (Default일 때 전역 iNovaType 폴백)
- `StartCamera_USB(chIdx)`:
  - FAVEngine(동영상) 모드면 메시지박스 출력 후 false 반환 (USB는 스트로브 전용)
  - moniker 비어있으면 메시지박스 후 false
  - `cam.Init` → `ConnectStreamPort` 호출, 로그 기록
- `GrabLoop1_USB`/`GrabLoop2_USB`:
  - 1초 timeout `GetImage` → 프리뷰 `SetBitmap` + (Capture시) `SaveLastImage` → `clsThread.RegArray{N}[CapCnt]` 채움 → `CoreLogic.Reg(ch, CapCnt, bRegCarType)` (WIN64 스트로브)
  - errCnt > 50 → `DisconnectStreamPort` + 200ms + `ConnectStreamPort` 재시도

#### 환경설정 UI — UserControl + 다이얼로그 + partial 확장
- `Form/UsbCamSettingPanel.cs/.Designer.cs/.resx` — UserControl (체크박스 "USB 카메라 사용" + 버튼 "USB 장치 선택..." + 라벨)
  - `IsUsbUsed` 프로퍼티, `UseChanged` / `SelectRequested` 이벤트
  - `SetInfo(deviceName, w, h)` / `SetIdle()` 표시 갱신
- `Form/frmUsbCamSelect.cs` — 다이얼로그 (장치 콤보 + 해상도 콤보 + 새로고침)
  - `USBCamera.ListDevices/ListResolutions` 호출, 결과를 `SelectedMoniker/DeviceName/Width/Height`로 노출
- `Form/frmEnv.UsbExt.cs` — partial 확장:
  - 카메라1·2 USB 상태를 멤버에 별도 보관 (`_usbUse{1,2}` 등) — `usbCamPanel`은 1·2가 공유하므로 외부 저장 필요
  - `InitUsbExtension()` — `frmEnv_Load` 끝(setEnv 끝, 216행)에서 호출, ENV에서 로드 + 이벤트 연결
  - `RefreshUsbExtensionForCam(idx)` — `btnCam{1,2}_Click` 끝(979, 1126행)에서 호출
  - `GetCurrentCamIdx()` — `groupBox1.Text.StartsWith("2")` 로 카메라 식별 (btnCam{1,2}_Click이 "1번/2번 카메라 설정"으로 텍스트 설정)
  - `ApplyUsbStateToEnv()` — `btnEnvSave_Click` 안 `func.SetEnv(env)` 직전(1239행)에 호출, IPCamera{1,2}Info에 USB 상태 반영
- `Form/frmEnv.Designer.cs` — `usbCamPanel` (UsbCamSettingPanel) 정적 배치 (tabCam, Location 423,21 / Size 315x35)

#### INI 영속화 — `Class/clsFunction.cs`
`[CAMERA]` 섹션에 카메라별 키 (Read 77~81/152~156, Write 535~539/598~602):
- `cam{1,2}source` (int), `cam{1,2}usbmoniker`, `cam{1,2}usbname`, `cam{1,2}usbwidth`, `cam{1,2}usbheight`

#### 빌드 — csproj 5개 + 의존 dll
- AForge dll 3종 등록: `AForge.dll`, `AForge.Video.dll`, `AForge.Video.DirectShow.dll` (`dll\` 폴더)
- AForge.Imaging.dll, AForge.Math.dll은 Update 후속 복사 단계(`_UpdateDllFiles`)에 추가
- USB 파일 5개 모두 등록: `Class\USB\USBCamera.cs`, `Form\frmUsbCamSelect.cs`, `Form\UsbCamSettingPanel.cs/.Designer.cs/.resx`, `Form\frmEnv.UsbExt.cs`, `Form\frmLprMain.Usb.cs`

---

## 사용 방법

### USB 카메라 설정 화면 (frmEnv → 카메라 설정 탭)
1. 카메라 1번 또는 2번 버튼 클릭 → groupBox1.Text가 "1번/2번 카메라 설정"으로 바뀜
2. 우측 상단 `usbCamPanel`의 **"USB 카메라 사용"** 체크 → `btnSelect` 활성화
3. **"USB 장치 선택..."** 클릭 → `frmUsbCamSelect` 다이얼로그
   - 장치 콤보에서 OBSBOT/Logitech/내장 웹캠 등 UVC 장치 선택
   - 해상도 콤보에서 원하는 해상도 (비워두면 카메라 기본값)
4. 확인 → 패널에 장치명·해상도 표시
5. **저장** 클릭 → `ApplyUsbStateToEnv` → INI `[CAMERA]` 섹션에 기록

### 동작 흐름 (USB 카메라1 = 입구, USB 카메라2 = 출구 가정)
1. `StartCamera()` → `IsUsbCam(1)` true → `StartCamera_USB(1)` → AForge `VideoCaptureDevice` 시작
2. `bothUsbCams` true → 2.5초 sleep → `StartCamera_USB(2)`
3. `iNovaType==1/2` → `StartCamera_iNova1/2()` → IsUsbCam 가드 통과 → `StartGrabLoop{1,2}()` → `GrabLoop{N}_USB` 스레드
4. `GrabLoop{N}_USB`가 1초 timeout `GetImage` → `SetBitmap(PicLpr{N}Image, bitmap)`로 메인 폼 프리뷰 갱신
5. 차량 검지(LoopOn) → `Capture{N} = true` → 다음 GrabLoop 루프에서 `SaveLastImage` + `CoreLogic.Reg`

---

## 알려진 이슈/주의 사항

- **`btnCam2_Click` 1113~1121행**: `Cam2.GetTriggerImageCount` 등 iNova SDK 직접 호출 — USB만 사용 시 `Cam2` 미연결 → 예외 가능 (try/catch 없음). 테스트 필요.
- **USB 디바이스 분리**: `Device_VideoSourceError`는 로그만, 실제 재연결은 GrabLoop의 `errCnt > 50`에 의존. DirectShow는 분리/재연결 시 핸들이 dirty 상태로 남을 수 있음.
- **`ListResolutions`**: `VideoCaptureDevice` 생성 후 Dispose 안 함 (영향 미미, 환경설정 화면에서만 호출).
- **`SaveLastImage`**: GetImage 클론 외에 `_latestFrame` 한 번 더 클론 → 메모리 살짝 비효율 (안전성은 OK).

---

## 미해결/다음 후보

- [ ] btnCam2_Click iNova SDK 호출에 try/catch 또는 IsUsbCam 가드 추가
- [ ] USB 카메라 라이브 프리뷰 (`OnNewFrame` 이벤트 활용) — 환경설정 화면에서 즉시 영상 확인
- [ ] USB 디바이스 분리/재연결 시 DirectShow 핸들 누수 검증
- [ ] DINGTIAN 입력 폴링 → TCP 60001 자동 푸시 전환 (이전 세션 미해결)
- [ ] DINGTIAN 다채널(16ch/32ch) 모델 지원 (`LastInput[8]` 배열 확장)

---

## 마지막으로 완료된 기능 (2026-05-06 세션)

### 1. DINGTIAN 이더넷 릴레이 보드 통합 (출력 + 입력)
KJC1000 / REALSYS 시리얼 보드와 별개로 이더넷 릴레이 보드 지원 추가.
원본 ParkingWeb 무인정산기에 같은 작업을 한 다음 LPR 소스에도 동일 패턴 이식.

#### 데이터 모델 — `Class/ClsStructure.cs`
- `enum DeviceList { KJC1000, REALSYS, DINGTIAN }` — 신규 항목
- `struct Dev_Setting`에 신규 필드:
  - `string IpAddress` — DINGTIAN 보드 IP
  - `int NetPort` — TCP/UDP 포트 (기본 60001)

#### 신규 클래스 — `Class/ClsDingtian.cs`
원본 SerialDevice.dll의 `ClsKJC_1000` / `ClsRealSys`와 호환되는 시그니처 (`RelayOn(port,delay,keep)`, `eventInput InEvent`).
- **출력**: TCP 60001 → UDP 60001 폴백, ASCII `"1{ch}"` ON / `"2{ch}"` OFF
  - TCP `BeginConnect`+500ms timeout, 실패 시 `PreferUdp=true` 학습 → 이후 UDP 직행 (응답 즉시화)
- **입력**: HTTP `GET /input.cgi` 폴링 (100ms 주기, 별도 백그라운드 Thread)
  - 응답 형식 `&0&0&8&v1&v2&...&v8&` (8채널 보드, SDK `input_cgi_parse.php` 알고리즘)
  - HTML/JS에 둘러싸인 응답도 정규식 `&\d+(?:&\d+)+&` 으로 패턴 추출
  - **active-low**: 보드 응답 `0` = 차량 검지(ON), `1` = 차량 없음(OFF) — REALSYS와 동일 의미
  - **1-based 발행**: 보드 raw `ch0~ch7` → 외부 InEvent에 `ch+1 (1~8)`로 발행 → KJC1000과 동일한 LoopPort 매핑
- **HTTP 최적화** (응답성):
  - `KeepAlive=true` + `Pipelined=true` → TCP 핸드셰이크 재사용
  - `ServicePointManager.Expect100Continue=false`, `UseNagleAlgorithm=false`, `DefaultConnectionLimit=16`
  - Timeout 500ms — 폴링 주기 100ms이므로 짧게
- 진단 로그: 폴링 시작 / 첫 raw 응답 / 추출 패턴 / HTTP 실패(1·2·3회+30회마다) / 채널 변화

#### INI — `Class/clsFunction.cs`
- 읽기/쓰기에 `dioip`, `dionetport` 추가 (`COMMON` 섹션)

#### 시리얼/이더넷 디스패치 — `Class/clsSerialPort.cs`
- `public ClsDingtian Dingtian = null` 멤버 추가
- 생성자 DIO 설정 영역에 DINGTIAN 분기:
  - 시리얼 포트 안 열고 `new ClsDingtian(ip, netPort)` + `StartInputPolling(100)` + `InEvent += DIOINPUT`
- 신규 헬퍼 `IsDingtian()`
- `GateOpen(DevIdx)`, `TestGateOpen(Port)`, `IsolatedGateOpen()` 모두 DINGTIAN 분기 추가
  - DINGTIAN은 `DioPort.IsOpen` 검사 우회 (시리얼 미사용)

#### 설정 화면 — `Form/frmEnv.cs`
- 동적 생성 `_gbDingtian` GroupBox ("DINGTIAN 이더넷 설정")
  - `BuildDingtianControls()` — frmEnv_Load 첫 줄에서 호출
  - 위치: groupBox6 우측 (groupBox6.X + width + 10, Y동일)
  - 크기: 220 × groupBox6.Height
  - 내용: 보드 IP, TCP 포트, 안내문 (출력 TCP/UDP 60001 / 입력 HTTP /input.cgi)
  - **groupBox6 본체는 그대로 유지** — 그 아래 gbPass(y=166)가 있어 height 확장 불가
- `setEnv` (Load): `_txtDioIp`, `_txtDioNetPort` 채우기 + `UpdateDioFieldsByType()`
- `saveEnv` (Save): IP/Port 저장
- `cmbDioType_SelectedIndexChanged`에 DINGTIAN 분기 추가:
  - 8채널 1~8로 모든 콤보(cmbLoop/cmbSmallCar/Gate1/Gate2/Add/Fixed/Isolate*) 채움 — 입력/출력 모두 1-based 통일
- `UpdateDioFieldsByType()`: DINGTIAN이면 시리얼/프로토콜/보드타입 비활성 + IP/Port 활성, 그 외 반대

#### 메인 폼 — `Form/frmLprMain.cs`
- `LoopDetect(int Port, bool Up)`의 KJC1000 분기에 DINGTIAN도 통합 (InEvent 시그니처 동일)
- **중요 버그 수정**: `LoopOn` 이벤트 구독 게이트가 `if(!SerialPort.Equals(""))` 였음.
  - DINGTIAN은 시리얼 미사용으로 빈 시리얼포트 → 게이트 false → 구독 자체가 안 됨 → 입력이 LoopDetect까지 도달 못 함
  - 수정: `isDingtianBoard` 추가 조건 + DINGTIAN 분기에서도 `SerialDev.LoopOn += LoopDetect` 등록

#### 빌드 — csproj 5개에 등록
`KyungsinLPR.csproj`, `KyungsinLPR64.csproj`, `KyungsinLPR86.csproj`, `KyungsinLPRx64.csproj`, `KyungsinLPRx86.csproj`
모두 `<Compile Include="Class\ClsDingtian.cs" />` 추가

---

## 사용 방법

### 보드 설정
1. SDK의 `ipfinder_v2_5.exe`로 보드 IP 확인 (또는 보드 LCD/웹 UI)
2. 무인정산기/LPR PC와 동일 LAN 연결
3. 보드 입력 핀에 차량 검지 루프 접점 결선 (active-low: 차량 진입 시 회로 닫힘)
4. 보드 출력 릴레이에 차단기 GATE OPEN 입력 결선

### LPR 설정 화면 (frmEnv → 차단기 설정 탭)
1. **DIO 종류** = `DINGTIAN` 선택 → 시리얼 컨트롤 자동 비활성, 우측 "DINGTIAN 이더넷 설정" 활성
2. **보드 IP** = 예 `192.168.1.100`
3. **TCP 포트** = `60001`
4. **Gate1/Gate2 포트** = 차단기 결선된 출력 릴레이 1~8
5. **카메라1/2 루프포트** = 차량 검지 루프 결선된 입력 핀 1~8 (1=ch0, 4=ch3)

### 동작 흐름
- 차량이 루프 위 진입 → 보드 입력 회로 닫힘(0) → HTTP /input.cgi 응답 변화 → 100ms 내 감지 → `InEvent(port, true)` → DIOINPUT → LoopOn → LoopDetect(port, true) → 카메라 트리거(Capture)
- 차량 통과 → 보드 입력 열림(1) → `InEvent(port, false)` → LoopDetect(port, false) → Loop Off
- 차단기 개방: `SerialDev.GateOpen(DevIdx)` → `Dingtian.RelayOn(port, delay, keep)` → TCP/UDP "1{port}" → keep ms → "2{port}"

---

## 참고 사항

- **REALSYS 시리얼 보드는 SerialDevice.dll에 캡슐화** — 프로토콜 비공개. 필요 시 IL 분석.
- **KJC1000 시리얼 ASCII 프로토콜** (참고용, ParkingWeb에서 IL 분석으로 발견):
  - 초기화: `kjrab`, ON: `kjr{port}a`, OFF: `kjr{port}b`
- **응답이 너무 느린 경우**: 폴링 주기를 줄이기보다 `KeepAlive` 미지원 보드일 가능성 → 매번 새 TCP. 펌웨어 업데이트 또는 ServicePointManager 튜닝 검토.
- **DINGTIAN 보드는 TCP 60001 거부 모델 있음** — `_dingtianPreferUdp` 학습 후 UDP 직행 (이미 구현됨).

## 미해결/추가 작업 후보

- [ ] DINGTIAN 입력 폴링 → TCP 60001 자동 푸시(보드의 입력 변화 알림 기능)로 전환 시 응답성 더 향상 가능 (SDK 매뉴얼 추가 조사 필요)
- [ ] frmEnv.Designer.cs를 직접 수정해 DINGTIAN GroupBox를 디자이너에 보이도록 (현재는 런타임 동적 생성 → VS 디자이너에서는 안 보임. 실행은 정상)
- [ ] 폴링 주기를 환경설정에서 사용자가 변경할 수 있게 INI 노출
- [ ] DINGTIAN 보드 다채널(16ch/32ch) 모델 지원 — `LastInput[8]` 배열 크기 확장
