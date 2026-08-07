using System;
using KyungsinLPR.USB;

namespace KyungsinLPR
{
    /// <summary>
    /// frmEnv 확장 — 디자이너로 배치한 usbCamPanel(UsbCamSettingPanel)에 이벤트만 연결.
    /// 카메라 1번/2번 USB 상태 메모리 관리 + ENV 동기화.
    /// </summary>
    public partial class frmEnv
    {
        // 카메라1/2 각각의 USB 상태 메모리 (usbCamPanel을 1·2번이 공유하므로 외부 저장 필요)
        private bool _usbUse1 = false, _usbUse2 = false;
        private string _usbMoniker1 = "", _usbMoniker2 = "";
        private string _usbDeviceName1 = "", _usbDeviceName2 = "";
        private int _usbWidth1 = 0, _usbHeight1 = 0;
        private int _usbWidth2 = 0, _usbHeight2 = 0;

        private bool _usbExtInited = false;

        /// <summary>frmEnv_Load 끝(setEnv 끝)에서 호출. ENV에서 USB 상태 로드 + 이벤트 연결.</summary>
        private void InitUsbExtension()
        {
            // 디자이너 컨트롤 미존재 시 즉시 종료 (구버전 디자이너 호환 가드)
            if (usbCamPanel == null) return;

            // ENV에서 카메라1/2 USB 상태 로드
            _usbUse1 = env.CameraEnv.IPCamera1Info.CameraSource == (int)ClsStructure.CameraSourceType.USB;
            _usbMoniker1 = env.CameraEnv.IPCamera1Info.UsbMoniker ?? "";
            _usbDeviceName1 = env.CameraEnv.IPCamera1Info.UsbDeviceName ?? "";
            _usbWidth1 = env.CameraEnv.IPCamera1Info.UsbResolutionWidth;
            _usbHeight1 = env.CameraEnv.IPCamera1Info.UsbResolutionHeight;

            _usbUse2 = env.CameraEnv.IPCamera2Info.CameraSource == (int)ClsStructure.CameraSourceType.USB;
            _usbMoniker2 = env.CameraEnv.IPCamera2Info.UsbMoniker ?? "";
            _usbDeviceName2 = env.CameraEnv.IPCamera2Info.UsbDeviceName ?? "";
            _usbWidth2 = env.CameraEnv.IPCamera2Info.UsbResolutionWidth;
            _usbHeight2 = env.CameraEnv.IPCamera2Info.UsbResolutionHeight;

            if (!_usbExtInited)
            {
                usbCamPanel.UseChanged += UsbCamPanel_UseChanged;
                usbCamPanel.SelectRequested += UsbCamPanel_SelectRequested;
                _usbExtInited = true;
            }

            RefreshUsbExtensionForCam(1);
        }

        /// <summary>btnCam1_Click / btnCam2_Click 끝에서 호출 — 표시를 해당 카메라 상태로 갱신.</summary>
        private void RefreshUsbExtensionForCam(int camIdx)
        {
            if (usbCamPanel == null) return;
            bool use = (camIdx == 1) ? _usbUse1 : _usbUse2;
            string dev = (camIdx == 1) ? _usbDeviceName1 : _usbDeviceName2;
            int w = (camIdx == 1) ? _usbWidth1 : _usbWidth2;
            int h = (camIdx == 1) ? _usbHeight1 : _usbHeight2;

            usbCamPanel.IsUsbUsed = use;
            if (use) usbCamPanel.SetInfo(dev, w, h);
            else usbCamPanel.SetIdle();
        }

        private int GetCurrentCamIdx()
        {
            if (groupBox1 == null || string.IsNullOrEmpty(groupBox1.Text)) return 1;
            return groupBox1.Text.StartsWith("2") ? 2 : 1;
        }

        private void UsbCamPanel_UseChanged(object sender, EventArgs e)
        {
            int idx = GetCurrentCamIdx();
            bool use = usbCamPanel.IsUsbUsed;
            if (idx == 1) _usbUse1 = use; else _usbUse2 = use;
            if (use)
            {
                // USB 켜면 같은 카메라의 WGWK 해제 (상호배타) + 콤보를 iNova로 표시
                if (idx == 1) _wgwkUse1 = false; else _wgwkUse2 = false;
                ApplyWgwkComboForCam(idx);
            }
            RefreshUsbExtensionForCam(idx);
        }

        private void UsbCamPanel_SelectRequested(object sender, EventArgs e)
        {
            int idx = GetCurrentCamIdx();
            string moniker = idx == 1 ? _usbMoniker1 : _usbMoniker2;
            int w = idx == 1 ? _usbWidth1 : _usbWidth2;
            int h = idx == 1 ? _usbHeight1 : _usbHeight2;

            using (var dlg = new frmUsbCamSelect(moniker, w, h))
            {
                if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    if (idx == 1)
                    {
                        _usbMoniker1 = dlg.SelectedMoniker;
                        _usbDeviceName1 = dlg.SelectedDeviceName;
                        _usbWidth1 = dlg.SelectedWidth;
                        _usbHeight1 = dlg.SelectedHeight;
                    }
                    else
                    {
                        _usbMoniker2 = dlg.SelectedMoniker;
                        _usbDeviceName2 = dlg.SelectedDeviceName;
                        _usbWidth2 = dlg.SelectedWidth;
                        _usbHeight2 = dlg.SelectedHeight;
                    }
                    RefreshUsbExtensionForCam(idx);
                }
            }
        }

        /// <summary>저장 시점에 IPCamera1Info / IPCamera2Info 에 카메라별 소스(USB/WGWK/기본) 통합 반영.</summary>
        private void ApplyUsbStateToEnv()
        {
            // 카메라별 소스 우선순위: USB > WGWK > 기본(전역 iNova)
            env.CameraEnv.IPCamera1Info.CameraSource =
                _usbUse1 ? (int)ClsStructure.CameraSourceType.USB
              : _wgwkUse1 ? (int)ClsStructure.CameraSourceType.WGWK
              : (int)ClsStructure.CameraSourceType.Default;
            env.CameraEnv.IPCamera1Info.UsbMoniker = _usbMoniker1 ?? "";
            env.CameraEnv.IPCamera1Info.UsbDeviceName = _usbDeviceName1 ?? "";
            env.CameraEnv.IPCamera1Info.UsbResolutionWidth = _usbWidth1;
            env.CameraEnv.IPCamera1Info.UsbResolutionHeight = _usbHeight1;

            env.CameraEnv.IPCamera2Info.CameraSource =
                _usbUse2 ? (int)ClsStructure.CameraSourceType.USB
              : _wgwkUse2 ? (int)ClsStructure.CameraSourceType.WGWK
              : (int)ClsStructure.CameraSourceType.Default;
            env.CameraEnv.IPCamera2Info.UsbMoniker = _usbMoniker2 ?? "";
            env.CameraEnv.IPCamera2Info.UsbDeviceName = _usbDeviceName2 ?? "";
            env.CameraEnv.IPCamera2Info.UsbResolutionWidth = _usbWidth2;
            env.CameraEnv.IPCamera2Info.UsbResolutionHeight = _usbHeight2;
        }

        /// <summary>WGWK 선택 시 해당 카메라의 USB 해제 (상호배타). WgwkExt에서 호출.</summary>
        private void ClearUsbForCamFromWgwk(int camIdx)
        {
            if (camIdx == 1) _usbUse1 = false; else _usbUse2 = false;
            if (usbCamPanel != null && GetCurrentCamIdx() == camIdx)
            {
                usbCamPanel.IsUsbUsed = false;
                usbCamPanel.SetIdle();
            }
        }
    }
}
