# Option(K) — 자체 CRNN(ONNX) 인식모듈 통합 가이드

LPR 인식모듈 선택지 **E/N/C** 에 **Option(K)** 추가. 자체 학습한 한국 번호판
CRNN 모델을 ONNX 로 인-프로세스 추론(Python 런타임 불필요).

> 빌드 대상: **KyungsinLPRx64**(x64). OnnxRuntime 네이티브가 x64 전용이라 x64 로만 동작.

---

## 1. 추가/변경된 파일

### 새 파일
- `KukjeLPR\Class\RegModule\CrnnOnnx.cs` — CRNN ONNX 추론 엔진(전처리+CTC 디코드)
- `KukjeLPR\OptionK\plate_crnn.onnx` — 학습 모델(ONNX, 단일 파일)
- `KukjeLPR\OptionK\plate_crnn.json` — charset/입력크기/정규화 메타(모델과 한 쌍)
- `KukjeLPR\dll\` 에 추가된 DLL:
  - `Microsoft.ML.OnnxRuntime.dll`(관리형) + `onnxruntime.dll`(네이티브 win-x64)
  - `System.Memory.dll`, `System.Buffers.dll`, `System.Numerics.Vectors.dll`,
    `System.Runtime.CompilerServices.Unsafe.dll` (OnnxRuntime 전이 의존, .NET Framework 용)

### 수정 파일
- `Class\RegModule\OptionK.cs` — **Tesseract → CRNN(ONNX) 으로 내부 교체**(인터페이스 동일)
- `Class\ClsStructure.cs` — `enum RegModule` 에 `OptionK` 추가(=3, 기존 값 보존)
- `Form\frmLprMain.cs` — dispatch 분기 추가(`RegModule==OptionK → OptionK.Reg(...)`, #if WIN64 내 8곳)
- `Form\frmEnv.cs` / `frmEnv.Designer.cs` — 설정 화면에 **Option(K) 라디오버튼** + 저장/로드
- `KyungsinLPRx64.csproj` — 위 .cs 컴파일 + 참조 + 모델/네이티브 dll 출력복사
- `app.config` — OnnxRuntime 전이 의존 **binding redirect**

> ⚠️ 다른 csproj(KyungsinLPR/64/86/x86)는 미배선. x64 외 빌드하려면 동일 4종(.cs 2개 Compile,
> OnnxRuntime+System.* 참조, 모델/네이티브 None 복사, app.config redirect)을 그 csproj에도 적용.

## 2. 빌드 방법 (Visual Studio, x64)

1. **`KyungsinLPRx64.sln`** 열기 → 구성 **x64 / Release**(또는 Debug).
2. 그대로 빌드(F6). 추가 NuGet 설치 불필요 — 필요한 DLL 은 모두 `dll\` 에 포함, csproj 가 참조함.
3. 출력 `bin\x64\Release\` 에 다음이 함께 생성되는지 확인:
   - `onnxruntime.dll`(루트), `Microsoft.ML.OnnxRuntime.dll`, `System.*` 의존 dll
   - `OptionK\plate_crnn.onnx`, `OptionK\plate_crnn.json`

> 빌드 에러 시 대안: VS NuGet 관리자에서 `Microsoft.ML.OnnxRuntime` 설치(전이 의존+redirect 자동).
> 그 경우 `dll\` 의 수동 추가분과 중복되지 않게 한쪽만 사용.

### 런타임 요건(배포 PC)
- Windows x64 + **VC++ 2019/2022 재배포 패키지**(onnxruntime.dll 의존). 없으면 설치.
- GPU 불필요(CPU 추론, 번호판 1장 ~150ms). GPU 쓰려면 §5 참고.

## 3. 사용 (운영)

1. 프로그램 실행 → **환경설정 → 인식모듈 선택(CAM) → Option(K)** 선택 → 저장.
   - (또는 INI `CAMERA/regmodule = 3` 직접 설정)
2. 평소처럼 운영. 캡처 JPG + 카메라 ROI 로 인식 → `PlateNo`/`PlateRoi` 기록.
3. 디버그: 인식 시 `LOG\OptionK\` 에 crop/검출 이미지 저장(ROI 점검용). 로그는 `[OptionK]` 태그.

## 4. 모델 업데이트 (인식률 개선분 반영)

모델 1쌍만 교체하면 끝(코드 수정 불필요). charset 도 json 에 있어 글자 추가/확장 자동 반영.

```
[학습 서버] 재학습 → 더 좋은 plate_crnn.pth
   → D:\license-plate-recognition\ 에서:  python export_onnx.py --model models\plate_crnn.pth --out out
   → out\plate_crnn.onnx + plate_crnn.json 생성(자동 검증: PyTorch와 일치 확인)
[배포 PC] bin\x64\Release\OptionK\ 의 두 파일을 교체 → 프로그램 재시작 → 즉시 반영
```
- 소스 재빌드로 반영하려면 `KukjeLPR\OptionK\` 의 두 파일 교체 후 재빌드.
- 입력크기·charset 이 바뀌어도(예: 62→64자, 강원 추가) json 이 함께 가므로 코드 그대로 동작.

## 5. (선택) GPU 사용
`OptionK.SetUseGpu(true)` 를 Initialize 전에 호출하면 CUDA EP 시도(없으면 CPU 폴백).
GPU 쓰려면 CUDA 지원 onnxruntime(예: Microsoft.ML.OnnxRuntime.Gpu)로 교체 필요.
LPR 배포 PC 는 보통 GPU 없음 → 기본 CPU 권장.

## 6. 동작 원리(요약)
- 입력 JPG → ROI crop → (검출 휴리스틱으로 번호판 박스 후보) → 각 후보 grayscale 크롭을
  CRNN 으로 인식 → 한국 번호판 형식 정규식으로 최적 후보 선택 → 실패 시 전체 ROI 폴백.
- CRNN 전처리: grayscale(BT.601) → bilinear resize 32×192 → /255 후 (-0.5)/0.5 → ONNX → CTC.
  (Python 학습 파이프라인과 수치 동일하게 포팅, 검증 완료)
