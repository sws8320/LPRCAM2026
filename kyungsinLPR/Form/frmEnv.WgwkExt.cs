using System;

namespace KyungsinLPR
{
    /// <summary>
    /// frmEnv 확장 — WGWK-A05D(HTTP snapshot.cgi) 카메라 종류 선택 + 계정/비번 입력(카메라별).
    /// "카메라 종류" 콤보를 카메라1·2 각각(btnCam1/btnCam2로 전환) 따로 선택. WGWK 선택은 전역이 아니라
    /// 해당 카메라 CameraSource(=WGWK)로 저장 → USB와 동일하게 카메라별.
    /// 계정/비번은 카메라별로 입력·저장(기본 admin/123456). HTTP포트·스트림은 숨김·고정(80/메인).
    /// 호스트(IP)는 기존 카메라 IP 입력란 사용.
    /// </summary>
    public partial class frmEnv
    {
        // 카메라1/2 각각의 WGWK 상태 메모리 (콤보/입력란을 1·2가 공유하므로 외부 저장 필요)
        private bool _wgwkUse1 = false, _wgwkUse2 = false;
        private string _wgwkUser1 = "admin", _wgwkUser2 = "admin";
        private string _wgwkPass1 = "123456", _wgwkPass2 = "123456";

        private bool _wgwkExtInited = false;
        private bool _wgwkLoading = false; // 콤보/입력란 프로그램적 설정 중 이벤트 역기록 방지

        /// <summary>frmEnv_Load 끝(InitUsbExtension 뒤)에서 호출. 이벤트 연결 + 카메라별 상태 로드.</summary>
        private void InitWgwkExtension()
        {
            try
            {
                if (gbWgwk == null) return; // 구버전 디자이너 호환 가드

                if (!_wgwkExtInited)
                {
                    // HTTP포트·스트림은 고정 표시값(숨김 상태)
                    txtWgwkPort.Text = "80";
                    if (cmbWgwkStream.Items.Count > 0) cmbWgwkStream.SelectedIndex = 0; // 메인(1)
                    // 계정/비번은 편집 가능 — 변경 시 현재 카메라 메모리에 반영
                    txtWgwkUser.TextChanged += WgwkCred_Changed;
                    txtWgwkPass.TextChanged += WgwkCred_Changed;
                    _wgwkExtInited = true;
                }

                // ENV에서 카메라별 WGWK 사용여부 + 계정/비번 로드 (기본값 보정)
                _wgwkUse1 = env.CameraEnv.IPCamera1Info.CameraSource == (int)ClsStructure.CameraSourceType.WGWK;
                _wgwkUse2 = env.CameraEnv.IPCamera2Info.CameraSource == (int)ClsStructure.CameraSourceType.WGWK;
                _wgwkUser1 = string.IsNullOrWhiteSpace(env.CameraEnv.IPCamera1Info.WgwkUser) ? "admin" : env.CameraEnv.IPCamera1Info.WgwkUser;
                _wgwkPass1 = string.IsNullOrEmpty(env.CameraEnv.IPCamera1Info.WgwkPass) ? "123456" : env.CameraEnv.IPCamera1Info.WgwkPass;
                _wgwkUser2 = string.IsNullOrWhiteSpace(env.CameraEnv.IPCamera2Info.WgwkUser) ? "admin" : env.CameraEnv.IPCamera2Info.WgwkUser;
                _wgwkPass2 = string.IsNullOrEmpty(env.CameraEnv.IPCamera2Info.WgwkPass) ? "123456" : env.CameraEnv.IPCamera2Info.WgwkPass;

                ApplyWgwkComboForCam(GetCurrentCamIdx());
            }
            catch (Exception ex) { Util.Logger.Log("InitWgwkExtension 실패: " + ex.Message); }
        }

        /// <summary>콤보+계정/비번을 해당 카메라 상태로 표시 (프로그램적 설정 — 이벤트 역기록 안 함).</summary>
        private void ApplyWgwkComboForCam(int camIdx)
        {
            if (gbWgwk == null || cmbCameraType == null) return;
            try
            {
                _wgwkLoading = true;
                bool wgwk = (camIdx == 1) ? _wgwkUse1 : _wgwkUse2;
                if (wgwk)
                    cmbCameraType.SelectedIndex = 2; // WGWK-A05D
                else
                    cmbCameraType.SelectedIndex = (env.CameraEnv.iNovaType == 2) ? 1 : 0; // iNova1/iNova2

                txtWgwkUser.Text = (camIdx == 1) ? _wgwkUser1 : _wgwkUser2;
                txtWgwkPass.Text = (camIdx == 1) ? _wgwkPass1 : _wgwkPass2;
            }
            finally { _wgwkLoading = false; }
            UpdateWgwkVisible();
        }

        /// <summary>btnCam1_Click / btnCam2_Click 끝에서 호출 — 콤보+계정/비번을 해당 카메라로 갱신.</summary>
        private void RefreshWgwkExtensionForCam(int camIdx)
        {
            ApplyWgwkComboForCam(camIdx);
        }

        /// <summary>계정/비번 변경 시 현재 카메라 메모리에 즉시 반영.</summary>
        private void WgwkCred_Changed(object sender, EventArgs e)
        {
            if (_wgwkLoading || gbWgwk == null) return;
            int idx = GetCurrentCamIdx();
            if (idx == 1) { _wgwkUser1 = txtWgwkUser.Text; _wgwkPass1 = txtWgwkPass.Text; }
            else { _wgwkUser2 = txtWgwkUser.Text; _wgwkPass2 = txtWgwkPass.Text; }
        }

        /// <summary>카메라 종류=WGWK-A05D(콤보 idx2) 일 때만 접속정보 그룹 표시.</summary>
        private void UpdateWgwkVisible()
        {
            if (gbWgwk == null) return;
            gbWgwk.Visible = (cmbCameraType != null && cmbCameraType.SelectedIndex == 2);
        }

        /// <summary>사용자가 콤보를 직접 바꿨을 때(cmbCameraType_SelectedIndexChanged) 호출 — 현재 카메라에만 적용.</summary>
        private void OnCameraTypeChanged()
        {
            if (_wgwkLoading) { UpdateWgwkVisible(); return; } // 프로그램적 변경은 무시
            if (cmbCameraType == null) return;
            // 서버캠 개별설정 모드: 전역(iNovaType)·cam1/2 상태 건드리지 않음. 소스는 저장 시 camsource로 기록.
            if (_serverCamIndex >= 0) { UpdateWgwkVisible(); return; }

            int idx = GetCurrentCamIdx();
            if (cmbCameraType.SelectedIndex == 2)
            {
                // 이 카메라만 WGWK. iNovaType(전역 iNova 종류)는 건드리지 않음
                if (idx == 1) _wgwkUse1 = true; else _wgwkUse2 = true;
                // USB와 상호배타 — 이 카메라 USB 해제
                ClearUsbForCamFromWgwk(idx);
            }
            else
            {
                // iNova1/iNova2 — 이 카메라 WGWK 해제 + 전역 iNova 종류 설정
                if (idx == 1) _wgwkUse1 = false; else _wgwkUse2 = false;
                env.CameraEnv.iNovaType = cmbCameraType.SelectedIndex + 1;
            }
            UpdateWgwkVisible();
        }

        /// <summary>저장 시점 — 카메라별 WGWK 계정/비번 반영. 포트80/메인은 고정. (CameraSource는 ApplyUsbStateToEnv에서 통합 결정)</summary>
        private void ApplyWgwkStateToEnv()
        {
            env.CameraEnv.IPCamera1Info.WgwkPort = 80;
            env.CameraEnv.IPCamera1Info.WgwkUser = string.IsNullOrWhiteSpace(_wgwkUser1) ? "admin" : _wgwkUser1;
            env.CameraEnv.IPCamera1Info.WgwkPass = string.IsNullOrEmpty(_wgwkPass1) ? "123456" : _wgwkPass1;
            env.CameraEnv.IPCamera1Info.WgwkStream = 1;

            env.CameraEnv.IPCamera2Info.WgwkPort = 80;
            env.CameraEnv.IPCamera2Info.WgwkUser = string.IsNullOrWhiteSpace(_wgwkUser2) ? "admin" : _wgwkUser2;
            env.CameraEnv.IPCamera2Info.WgwkPass = string.IsNullOrEmpty(_wgwkPass2) ? "123456" : _wgwkPass2;
            env.CameraEnv.IPCamera2Info.WgwkStream = 1;
        }
    }
}
