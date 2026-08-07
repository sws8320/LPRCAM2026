using System;
using System.Runtime.InteropServices;

namespace KyungsinLPR
{
    public enum PLATECATEGORY
    {
        UNKNOWN_PLATE = 0x0000,

        WHITE_PLATE = 0x1000,	// 대표 마스크(흰색, 노랑)
        WHITE_LONG = 0x1100,	// 신형 흰색 자가용 번호판 차량
        YELLOW_LONG_PROV,		// 신형 사업용 노랑 번호판 차량(지역번호 포함)
        WHITE_SHORT,			// 짧은 흰색 (구형) 번호판 차량
        YELLOW_SHORT,			// 구형 사업용 노랑 번호판 차량(화물차, 버스 포함)
        WHITE_LONG_INTERIM,		// 임시 번호판 123456 6자리.

        GREEN_PLATE = 0x2000,	// 대표 마스크(녹색)
        GREEN_OLD,				// 구형 녹색 번호판 차량
        GREEN_NEW,				// 신형 녹색 번호판 차량

        CONSTRUCTION_PLATE = 0x3000, // 건설중기 차량
    };

    public enum PLATETYPE
    {
        NUMBER_TYPE_ERROR = 0,
        NUMBER_TYPE_NORMAL,
        NUMBER_TYPE_OLD,
        NUMBER_TYPE_2004,
        NUMBER_TYPE_2005,
        NUMBER_TYPE_SPECIAL,
        NUMBER_TYPE_2006_LOCAL,

        NUMBER_TYPE_CONSTRUCTION,
        NUMBER_TYPE_INTERIM,
    };

    public enum ELANPRRECOGREPORT
    {
        NO = 0,	// 미인식
        OK,		// 인식
        NG,		// 부분인식
    };

    public struct ELANPRESULT
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string strPlateNumber;

        [MarshalAs(UnmanagedType.U4)]
        public ELANPRRECOGREPORT enumRECOGREPORT;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4 * 16)]   // RECT has 4 of long's
        public UInt32[] rcChar;

        [MarshalAs(UnmanagedType.U4)]
        public PLATECATEGORY catPlate;

        [MarshalAs(UnmanagedType.U4)]
        public PLATETYPE typePlate;
    };

    public struct IMAGE_INFO
    {
        public IntPtr img;
        public Int32 bytesperline;
        public Int32 bitsperpixel;
        public Int32 xsize;
        public Int32 ysize;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct Rect
    {
        [FieldOffset(0)]
        public int left;
        [FieldOffset(4)]
        public int top;
        [FieldOffset(8)]
        public int right;
        [FieldOffset(12)]
        public int bottom;
    };

    public struct ElanprPlateCandidates
    {
        public Int32 nNumCandis;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4 * 10)]
        public UInt32[] rcPlateCandis;
    };

    partial class Elanpr
    {
        //차량번호인식엔진(elanprTM )의 초기화
        //ELANPRENGINE_API HRESULT Elanpr_Initialize( DWORD *pdwID ) ;
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_Initialize(ref UInt32 pEngineID);

        //차량번호인식엔진(elanprTM )의 번호판 인식
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlate(DWORD dwEngineID, LPCSTR lpszFileName, LPELANPR_RECOG_RESULT pResult);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlate(UInt32 uEngineID, string pathName, ref ELANPRESULT result);

        //기본적인 기능은 Elanpr_RecognizePlate 과 같이 동작하며, 입력된 이미지를 가로, 세
        //로 각각 nScalePercent (%) 만큼 확대/축소하여 인식을 수행한다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlate Ext(DWORD dwEngineID, LPCSTR lpszFileName, int nScalePercent, LPELANPR_RECOG_RESULT pResult ) ;
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateExt(UInt32 uEngineID, string pathName, Int32 nScalePercent, ref ELANPRESULT result);

        //메모리에 존재하는 다양한 포맷의 버퍼 데이터를 입력하여 차량 이미지 인식을 수행한
        //다. 여러 이미지 포맷(JPEG, BMP, TIF, PNG 에 한해)을 지원한다. 영상 스캔에 성공하면
        //S_OK를 리턴하며 인식 결과값은 pResult 에 채워진다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlateBuffer(DWORD dwEngineID, LPBYTE pBufferImage, INT nBufferSize, LPELANPR_RECOG_RESULT pResult);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateBuffer(UInt32 dwEngineID, byte[] pBufferImage, int nBufferSize, ref ELANPRESULT pResult);

        //차량관제시스템을 구현하는 업체에 특화된 API로, CCTV 나 외부 데이터 등 Raw
        //Bitmap 이미지를 지원하는 함수이다. 현재는 오직 8-Bit 그레이 스케일의 Raw Bitmap
        //만 지원되며 영상 스캔에 성공하면 S_OK를 리턴하며 인식 결과값은 pResult 에 채워진
        //다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlateStruct(DWORD dwEngineID, LPIMAGE_INFO pImageInfo, LPELANPR_RECOG_RESULT pResult ) ;
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateStruct(UInt32 uEngineID, ref IMAGE_INFO imgInfo, ref ELANPRESULT result);

        //기본적인 기능은 Elanpr_RecognizePlateStruct 과 같이 동작하며, 입력된 이미지를 가
        //로, 세로 각각 nScalePercent (%) 만큼 확대/축소하여 인식을 수행한다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlateStruct Ext(DWORD dwEngineID, int nScalePercent, LPIMAGE_INFO pImageInfo, LPELANPR_RECOG_RESULT pResult );
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateStructExt(UInt32 uEngineID, Int32 nScalePercent, ref IMAGE_INFO imgInfo, ref ELANPRESULT result);

        //Recognize 계열 함수를 사용하여 인식이 성공한 후 그 인식된 숫자들의 평균 인식 정
        //확도를 반환한다. 인식 실패 후 호출하면 0값을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_GetRecogAccuracyInPercent(DWORD dwEngineID, double* pValAccuracyInPercent);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_GetRecogAccuracyInPercent(UInt32 uEngineID, ref UInt64 pValAccuracyInPercent);

        //Recognize 계열 함수를 사용하여 인식이 성공한 후 그 인식된 숫자 및 한글들의 각
        //세그먼트별 인식 정확도(매칭포인트)를 채워 반환한다. 인식 실패 후 호출하면 전부 0.0
        //이 채워져 반환된다.
        //(서울, 강원 등 지역번호판의 경우 pValAccuracy8Rooms[0]에 매칭포인트가 채워지며,
        //전국 번호판의 경우 pValAccuracy8Rooms[0] 은 0으로 채워짐)
        //ELANPRENGINE_API HRESULT Elanpr_FillRecogAccuracyArray(DWORD dwEngineID, float * pValAccuracy8Rooms);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_FillRecogAccuracyArray(UInt32 uEngineID, ref float pValAccuracy8Rooms);

        //차량관제시스템의 현장에 따라 차량 진입시 카메라에 완전히 평행하게 촬영되지 않는
        //곳이 있다. 현장 상황이 이러한 경우 이 API를 활용하여 각도를 적정하게 입력하면, 내
        //부적으로 영상을 회전, 영상 처리하여 인식을 수행한다. 주어진 엔진 아이디에만 유효하
        //며 기능을 무효화 하려면 iAnglePull 값을 0으로 하여 본 API 함수를 호출한다.
        //ELANPRENGINE_API HRESULT Elanpr_SetWarpingAngle(DWORD dwEngineID, int iAngleToPull);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetWarpingAngle(UInt32 uEngineID, int iAngleToPull);

        //차량번호판의 위치를 알 경우나, 관심 영역만을 인식 수행하게 되면 퍼포먼스에 도움
        //이 될 때가 있다. 이 경우 ROI ( Region Of Interest )를 사전 설정하여 인식을 호출할 수
        //있다. 한 번 설정하면 Recognize 수행할 때마다 같은 ROI 설정이 되어지므로 사용 후
        //무효화하는 것이 중요하다.
        //ELANPRENGINE_API HRESULT Elanpr_SetPlateLocation(DWORD dwEngineID, RECT rcPlateLocation);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetPlateLocation(UInt32 uEngineID, Rect rcPlateLocation);

        //주어진 이미지에서 차량번호판이 있는지 없는지를 빠르게 검지하고 싶을 때 사용한다.
        //번호판이 있을 경우 S_OK, 없을 경우 E_FAIL을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_DoesExistNumberPlate(DWORD dwEngineID, LPCSTR lpszFileName, int minNumPix, int maxNumPix);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_DoesExistNumberPlate(UInt32 uEngineID, string lpszFileName, int minNumPix, int maxNumPix);

        //주어진 이미지에서 차량번호판이 있는지 없는지를 빠르게 검지하고 싶을 때 사용한다.
        //번호판이 있을 경우 S_OK, 없을 경우 E_FAIL을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_DoesExistNumberPlateBuffer(DWORD dwEngineID, LPBYTE pImageBuffer, int nBufferSize, int minNumPix, int maxNumPix);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_DoesExistNumberPlateBuffer(UInt32 uEngineID, byte[] pImageBuffer, int nBufferSize, int minNumPix, int maxNumPix);

        //주어진 이미지에서 차량번호판이 있는지 없는지를 빠르게 검지하고 싶을 때 사용한다.
        //번호판이 있을 경우 S_OK, 없을 경우 E_FAIL을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_DoesExistNumberPlateStruct(DWORD dwEngineID, LPIMAGE_INFO pImageInfo, int minNumPix, int maxNumPix);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_DoesExistNumberPlateStruct(UInt32 uEngineID, ref IMAGE_INFO pImageBuffer, int minNumPix, int maxNumPix);

        //DoesExistNumberPlate 계열 함수를 호출한 직후 번호판 후보영역의 정보를 알고 싶을
        //때 호출한다. 차량번호판이 있을 때, 반환되는 RECT 정보를 가지고 ROI 인식을 수행할
        //수 있다.
        //ELANPRENGINE_API HRESULT Elanpr_RetrievePlateCandidates( DWORD dwEngineID, ELANPRPLATECANDIDATES *pPlateCandidates );
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RetrievePlateCandidates(UInt32 uEngineID, ref ElanprPlateCandidates pPlateCandidates);

        //차량번호판에서 차량번호를 인식할 때 최소 픽셀높이와 최대 픽셀높이를 세팅한다. 고
        //정형 카메라에서 이 Range를 적용하면 퍼포먼스에 효과적일 수 있다.
        //ELANPRENGINE_API HRESULT Elanpr_SetMinMaxNumberPix(DWORD dwEngineID, int minNumberPix, int maxNumberPix);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetMinMaxNumberPix(UInt32 uEngineID, int minNumberPix, int maxNumberPix);

        //차량번호판에서 차량번호를 인식할 때 속도 최적화를 적용할지 인식 품질 최적화를
        //적용할지를 결정한다. 0 ~ 100 의 값을 가지며, 100일 경우 학습된 데이터를 모두 이용
        //하여 CCTV 등 안좋은 이미지 상황에서도 동작하게 한다 (품질최적화). 0으로 갈수록 선
        //명한 이미지에서 속도의 최적화를 한다(인식속도최적화). Initialize 후 한 번만 호출하도
        //록 한다. 디폴트는 100 이다.
        //ELANPRENGINE_API HRESULT Elanpr_SetRecogQualityOpt(DWORD dwEngineID, int nRecogQualityPercent);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_ReduceNoiseOpt(UInt32 uEngineID, int bReduceNoiseOpt);

        //때로는 획득한 이미지가 조도, 역광 및 IR 동기조정 실패 등으로 퀄리티가 저하되어
        //있는 경우 적용할 수 있는 옵션이다. 일반적으로 PyrDown/PyrUp 알고리듬을 인식수행
        //전 이미지에 적용하여 인식한다. . 디폴트는 false 이다.
        //ELANPRENGINE_API HRESULT Elanpr_SetRecogQualityOpt(DWORD dwEngineID, int nRecogQualityPercent);
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetRecogQualityOpt(UInt32 uEngineID, int nRecogQualityPercent);

        //차량번호인식엔진(elanprTM )의 멀티번호판 인식*
        //*elanpr engine 2.0 부터는 한 이미지내 복수의 번호판 인식결과를 가져올 수 있는 API를
        //지원한다. 아래 멀티번호판 인식 함수의 대부분은 1.3항의 싱글번호판 인식과 호출규칙이나
        //동작은 비슷하다. Caller는 복수의 ELANPR_RECOG_RESULT 배열을 제공함으로써 한 이미
        //지내 복수개의 번호인식결과를 도출할 수 있다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlates( DWORD dwEngineID, LPCSTR lpszFileName, OUT INT* pVehicleCount, LPELANPR_RECOG_RESULT * ppResult );
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlates(UInt32 uEngineID, string lpszFileName, ref int pVehicleCount, ref ELANPRESULT ppResult);

        //기본적인 기능은 Elanpr_RecognizePlateMultiPlates 과 같이 동작하며, 입력된 이미지
        //를 가로, 세로 각각 nScalePercent (%) 만큼 확대/축소하여 인식을 수행한다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlatesExt( DWORD dwEngineID, LPCSTR lpszFileName, int nScalePercent, OUT INT* pVehicleCount, LPELANPR_RECOG_RESULT* ppResult );
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlatesExt(UInt32 uEngineID, string lpszFileName, int nScalePercent, ref int pVehicleCount, ref ELANPRESULT ppResult);

        //메모리에 존재하는 다양한 포맷의 버퍼 데이터를 입력하여 차량 이미지 인식을 수행한
        //다. 여러 이미지 포맷(JPEG, BMP, TIF, PNG 에 한해)을 지원한다. 영상 스캔에 성공하면
        //S_OK를 리턴하며 인식 결과값은 ppResult 배열에 채워진다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlatesBuffer( DWORD dwEngineID, LPBYTE pBufferImage, INT nBufferSize, OUT INT* pVehicleCount, LPELANPR_RECOG_RESULT* ppResult );
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlatesBuffer(UInt32 uEngineID, byte[] pBufferImage, int nScalePercent, ref int pVehicleCount, ref ELANPRESULT ppResult);

        //차량관제시스템을 구현하는 업체에 특화된 API로, CCTV 나 외부 데이터 등 Raw
        //Bitmap 이미지를 지원하는 함수이다. 현재는 오직 8-Bit 그레이 스케일의 Raw Bitmap
        //만 지원되며 영상 스캔에 성공하면 S_OK를 리턴하며 인식 결과값은 ppResult(배열) 에
        //채워진다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlatesStruct( DWORD dwEngineID, LPIMAGE_INFO pImageInfo, OUT INT* pVehicleCount, LPELANPR_ RECOG_RESULT* ppResult ) ;
        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlatesStruct(UInt32 uEngineID, ref IMAGE_INFO pImageInfo, ref int pVehicleCount, ref ELANPRESULT ppResult);


        [DllImport("elanpr-engine.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_Finalize(UInt32 uEngineID);
    }

    partial class Elanpr64
    {
        //차량번호인식엔진(elanprTM )의 초기화
        //ELANPRENGINE_API HRESULT Elanpr_Initialize( DWORD *pdwID ) ;
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_Initialize(ref UInt32 pEngineID);

        //차량번호인식엔진(elanprTM )의 번호판 인식
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlate(DWORD dwEngineID, LPCSTR lpszFileName, LPELANPR_RECOG_RESULT pResult);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlate(UInt32 uEngineID, string pathName, ref ELANPRESULT result);

        //기본적인 기능은 Elanpr_RecognizePlate 과 같이 동작하며, 입력된 이미지를 가로, 세
        //로 각각 nScalePercent (%) 만큼 확대/축소하여 인식을 수행한다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlate Ext(DWORD dwEngineID, LPCSTR lpszFileName, int nScalePercent, LPELANPR_RECOG_RESULT pResult ) ;
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateExt(UInt32 uEngineID, string pathName, Int32 nScalePercent, ref ELANPRESULT result);

        //메모리에 존재하는 다양한 포맷의 버퍼 데이터를 입력하여 차량 이미지 인식을 수행한
        //다. 여러 이미지 포맷(JPEG, BMP, TIF, PNG 에 한해)을 지원한다. 영상 스캔에 성공하면
        //S_OK를 리턴하며 인식 결과값은 pResult 에 채워진다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlateBuffer(DWORD dwEngineID, LPBYTE pBufferImage, INT nBufferSize, LPELANPR_RECOG_RESULT pResult);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateBuffer(UInt32 dwEngineID, byte[] pBufferImage, int nBufferSize, ref ELANPRESULT pResult);

        //차량관제시스템을 구현하는 업체에 특화된 API로, CCTV 나 외부 데이터 등 Raw
        //Bitmap 이미지를 지원하는 함수이다. 현재는 오직 8-Bit 그레이 스케일의 Raw Bitmap
        //만 지원되며 영상 스캔에 성공하면 S_OK를 리턴하며 인식 결과값은 pResult 에 채워진
        //다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlateStruct(DWORD dwEngineID, LPIMAGE_INFO pImageInfo, LPELANPR_RECOG_RESULT pResult ) ;
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateStruct(UInt32 uEngineID, ref IMAGE_INFO imgInfo, ref ELANPRESULT result);

        //기본적인 기능은 Elanpr_RecognizePlateStruct 과 같이 동작하며, 입력된 이미지를 가
        //로, 세로 각각 nScalePercent (%) 만큼 확대/축소하여 인식을 수행한다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizePlateStruct Ext(DWORD dwEngineID, int nScalePercent, LPIMAGE_INFO pImageInfo, LPELANPR_RECOG_RESULT pResult );
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizePlateStructExt(UInt32 uEngineID, Int32 nScalePercent, ref IMAGE_INFO imgInfo, ref ELANPRESULT result);

        //Recognize 계열 함수를 사용하여 인식이 성공한 후 그 인식된 숫자들의 평균 인식 정
        //확도를 반환한다. 인식 실패 후 호출하면 0값을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_GetRecogAccuracyInPercent(DWORD dwEngineID, double* pValAccuracyInPercent);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_GetRecogAccuracyInPercent(UInt32 uEngineID, ref UInt64 pValAccuracyInPercent);

        //Recognize 계열 함수를 사용하여 인식이 성공한 후 그 인식된 숫자 및 한글들의 각
        //세그먼트별 인식 정확도(매칭포인트)를 채워 반환한다. 인식 실패 후 호출하면 전부 0.0
        //이 채워져 반환된다.
        //(서울, 강원 등 지역번호판의 경우 pValAccuracy8Rooms[0]에 매칭포인트가 채워지며,
        //전국 번호판의 경우 pValAccuracy8Rooms[0] 은 0으로 채워짐)
        //ELANPRENGINE_API HRESULT Elanpr_FillRecogAccuracyArray(DWORD dwEngineID, float * pValAccuracy8Rooms);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_FillRecogAccuracyArray(UInt32 uEngineID, ref float pValAccuracy8Rooms);

        //차량관제시스템의 현장에 따라 차량 진입시 카메라에 완전히 평행하게 촬영되지 않는
        //곳이 있다. 현장 상황이 이러한 경우 이 API를 활용하여 각도를 적정하게 입력하면, 내
        //부적으로 영상을 회전, 영상 처리하여 인식을 수행한다. 주어진 엔진 아이디에만 유효하
        //며 기능을 무효화 하려면 iAnglePull 값을 0으로 하여 본 API 함수를 호출한다.
        //ELANPRENGINE_API HRESULT Elanpr_SetWarpingAngle(DWORD dwEngineID, int iAngleToPull);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetWarpingAngle(UInt32 uEngineID, int iAngleToPull);

        //차량번호판의 위치를 알 경우나, 관심 영역만을 인식 수행하게 되면 퍼포먼스에 도움
        //이 될 때가 있다. 이 경우 ROI ( Region Of Interest )를 사전 설정하여 인식을 호출할 수
        //있다. 한 번 설정하면 Recognize 수행할 때마다 같은 ROI 설정이 되어지므로 사용 후
        //무효화하는 것이 중요하다.
        //ELANPRENGINE_API HRESULT Elanpr_SetPlateLocation(DWORD dwEngineID, RECT rcPlateLocation);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetPlateLocation(UInt32 uEngineID, Rect rcPlateLocation);

        //주어진 이미지에서 차량번호판이 있는지 없는지를 빠르게 검지하고 싶을 때 사용한다.
        //번호판이 있을 경우 S_OK, 없을 경우 E_FAIL을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_DoesExistNumberPlate(DWORD dwEngineID, LPCSTR lpszFileName, int minNumPix, int maxNumPix);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_DoesExistNumberPlate(UInt32 uEngineID, string lpszFileName, int minNumPix, int maxNumPix);

        //주어진 이미지에서 차량번호판이 있는지 없는지를 빠르게 검지하고 싶을 때 사용한다.
        //번호판이 있을 경우 S_OK, 없을 경우 E_FAIL을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_DoesExistNumberPlateBuffer(DWORD dwEngineID, LPBYTE pImageBuffer, int nBufferSize, int minNumPix, int maxNumPix);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_DoesExistNumberPlateBuffer(UInt32 uEngineID, byte[] pImageBuffer, int nBufferSize, int minNumPix, int maxNumPix);

        //주어진 이미지에서 차량번호판이 있는지 없는지를 빠르게 검지하고 싶을 때 사용한다.
        //번호판이 있을 경우 S_OK, 없을 경우 E_FAIL을 반환한다.
        //ELANPRENGINE_API HRESULT Elanpr_DoesExistNumberPlateStruct(DWORD dwEngineID, LPIMAGE_INFO pImageInfo, int minNumPix, int maxNumPix);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_DoesExistNumberPlateStruct(UInt32 uEngineID, ref IMAGE_INFO pImageBuffer, int minNumPix, int maxNumPix);

        //DoesExistNumberPlate 계열 함수를 호출한 직후 번호판 후보영역의 정보를 알고 싶을
        //때 호출한다. 차량번호판이 있을 때, 반환되는 RECT 정보를 가지고 ROI 인식을 수행할
        //수 있다.
        //ELANPRENGINE_API HRESULT Elanpr_RetrievePlateCandidates( DWORD dwEngineID, ELANPRPLATECANDIDATES *pPlateCandidates );
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RetrievePlateCandidates(UInt32 uEngineID, ref ElanprPlateCandidates pPlateCandidates);

        //차량번호판에서 차량번호를 인식할 때 최소 픽셀높이와 최대 픽셀높이를 세팅한다. 고
        //정형 카메라에서 이 Range를 적용하면 퍼포먼스에 효과적일 수 있다.
        //ELANPRENGINE_API HRESULT Elanpr_SetMinMaxNumberPix(DWORD dwEngineID, int minNumberPix, int maxNumberPix);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetMinMaxNumberPix(UInt32 uEngineID, int minNumberPix, int maxNumberPix);

        //차량번호판에서 차량번호를 인식할 때 속도 최적화를 적용할지 인식 품질 최적화를
        //적용할지를 결정한다. 0 ~ 100 의 값을 가지며, 100일 경우 학습된 데이터를 모두 이용
        //하여 CCTV 등 안좋은 이미지 상황에서도 동작하게 한다 (품질최적화). 0으로 갈수록 선
        //명한 이미지에서 속도의 최적화를 한다(인식속도최적화). Initialize 후 한 번만 호출하도
        //록 한다. 디폴트는 100 이다.
        //ELANPRENGINE_API HRESULT Elanpr_SetRecogQualityOpt(DWORD dwEngineID, int nRecogQualityPercent);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_ReduceNoiseOpt(UInt32 uEngineID, int bReduceNoiseOpt);

        //때로는 획득한 이미지가 조도, 역광 및 IR 동기조정 실패 등으로 퀄리티가 저하되어
        //있는 경우 적용할 수 있는 옵션이다. 일반적으로 PyrDown/PyrUp 알고리듬을 인식수행
        //전 이미지에 적용하여 인식한다. . 디폴트는 false 이다.
        //ELANPRENGINE_API HRESULT Elanpr_SetRecogQualityOpt(DWORD dwEngineID, int nRecogQualityPercent);
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_SetRecogQualityOpt(UInt32 uEngineID, int nRecogQualityPercent);

        //차량번호인식엔진(elanprTM )의 멀티번호판 인식*
        //*elanpr engine 2.0 부터는 한 이미지내 복수의 번호판 인식결과를 가져올 수 있는 API를
        //지원한다. 아래 멀티번호판 인식 함수의 대부분은 1.3항의 싱글번호판 인식과 호출규칙이나
        //동작은 비슷하다. Caller는 복수의 ELANPR_RECOG_RESULT 배열을 제공함으로써 한 이미
        //지내 복수개의 번호인식결과를 도출할 수 있다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlates( DWORD dwEngineID, LPCSTR lpszFileName, OUT INT* pVehicleCount, LPELANPR_RECOG_RESULT * ppResult );
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlates(UInt32 uEngineID, string lpszFileName, ref int pVehicleCount, ref ELANPRESULT ppResult);

        //기본적인 기능은 Elanpr_RecognizePlateMultiPlates 과 같이 동작하며, 입력된 이미지
        //를 가로, 세로 각각 nScalePercent (%) 만큼 확대/축소하여 인식을 수행한다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlatesExt( DWORD dwEngineID, LPCSTR lpszFileName, int nScalePercent, OUT INT* pVehicleCount, LPELANPR_RECOG_RESULT* ppResult );
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlatesExt(UInt32 uEngineID, string lpszFileName, int nScalePercent, ref int pVehicleCount, ref ELANPRESULT ppResult);

        //메모리에 존재하는 다양한 포맷의 버퍼 데이터를 입력하여 차량 이미지 인식을 수행한
        //다. 여러 이미지 포맷(JPEG, BMP, TIF, PNG 에 한해)을 지원한다. 영상 스캔에 성공하면
        //S_OK를 리턴하며 인식 결과값은 ppResult 배열에 채워진다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlatesBuffer( DWORD dwEngineID, LPBYTE pBufferImage, INT nBufferSize, OUT INT* pVehicleCount, LPELANPR_RECOG_RESULT* ppResult );
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlatesBuffer(UInt32 uEngineID, byte[] pBufferImage, int nScalePercent, ref int pVehicleCount, ref ELANPRESULT ppResult);

        //차량관제시스템을 구현하는 업체에 특화된 API로, CCTV 나 외부 데이터 등 Raw
        //Bitmap 이미지를 지원하는 함수이다. 현재는 오직 8-Bit 그레이 스케일의 Raw Bitmap
        //만 지원되며 영상 스캔에 성공하면 S_OK를 리턴하며 인식 결과값은 ppResult(배열) 에
        //채워진다.
        //ELANPRENGINE_API HRESULT Elanpr_RecognizeMultiPlatesStruct( DWORD dwEngineID, LPIMAGE_INFO pImageInfo, OUT INT* pVehicleCount, LPELANPR_ RECOG_RESULT* ppResult ) ;
        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_RecognizeMultiPlatesStruct(UInt32 uEngineID, ref IMAGE_INFO pImageInfo, ref int pVehicleCount, ref ELANPRESULT ppResult);


        [DllImport("elanpr-engine-x64.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 Elanpr_Finalize(UInt32 uEngineID);
    }
}
