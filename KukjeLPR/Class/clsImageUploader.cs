using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace KyungsinLPR
{
    /// <summary>
    /// 인식된 차량 이미지를 ParkingWeb 이미지 서버로 HTTP 업로드.
    /// 백그라운드 큐 + 재시도 — LPR 인식 흐름은 절대 블록하지 않음.
    /// Setting.ini의 [UPLOAD] 섹션 사용:
    ///   enabled=true
    ///   serverurl=http://192.168.1.3:18100
    ///   apikey=lprcam-2025-secret
    /// </summary>
    public static class clsImageUploader
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private static BlockingCollection<UploadItem> _queue;
        private static Thread _worker;
        private static volatile bool _running;

        public static bool   Enabled   { get; private set; }
        public static string ServerUrl { get; private set; } = "";
        public static string ApiKey    { get; private set; } = "";

        private struct UploadItem
        {
            public string LocalPath;
            public string DateDir;
            public string FileName;
            public int    Retry;
        }

        /// <summary>환경설정 화면에서 값 변경 후 호출 — INI 갱신 + 워커 재기동.</summary>
        public static void SaveIni(bool enabled, string serverUrl, string apiKey)
        {
            try
            {
                Util.Function.IniWriteValue("UPLOAD", "enabled", enabled.ToString());
                Util.Function.IniWriteValue("UPLOAD", "serverurl", (serverUrl ?? "").Trim().TrimEnd('/'));
                Util.Function.IniWriteValue("UPLOAD", "apikey", (apiKey ?? "").Trim());
            }
            catch (Exception e)
            {
                Util.Logger.Log("[Uploader] INI 저장 실패: " + e.Message);
            }
            Reload();
        }

        /// <summary>INI 다시 읽어 워커 재기동 (Start와 동일하지만 기존 워커 중지 후 재시작).</summary>
        public static void Reload()
        {
            try { Stop(); } catch { }
            Start();
        }

        public static void Start()
        {
            try
            {
                string en  = Util.Function.IniReadValue("UPLOAD", "enabled") ?? "";
                Enabled    = en.Equals("true", StringComparison.OrdinalIgnoreCase) || en == "1";
                ServerUrl  = (Util.Function.IniReadValue("UPLOAD", "serverurl") ?? "").Trim().TrimEnd('/');
                ApiKey     = (Util.Function.IniReadValue("UPLOAD", "apikey") ?? "").Trim();
                // '원격 차번인식만 사용(이미지 업로드 안함)' → 서버+LPR 같은 PC 공존 시 이미지 중복저장 방지.
                //  인식(OptionK)은 그대로, 이미지 업로드만 비활성.
                string nu = Util.Function.IniReadValue("OPTIONK", "remote_noupload") ?? "";
                if (nu.Equals("true", StringComparison.OrdinalIgnoreCase) || nu == "1")
                {
                    Enabled = false;
                    Util.Logger.Log("[Uploader] '원격 차번인식만 사용' 설정 → 이미지 업로드 비활성");
                }
            }
            catch (Exception e)
            {
                Util.Logger.Log("[Uploader] INI 읽기 실패: " + e.Message);
                Enabled = false;
            }

            if (!Enabled || string.IsNullOrEmpty(ServerUrl))
            {
                Util.Logger.Log(string.Format("[Uploader] 비활성 (enabled={0}, url={1})", Enabled, ServerUrl));
                return;
            }

            _queue   = new BlockingCollection<UploadItem>(boundedCapacity: 1000);
            _running = true;
            _worker  = new Thread(WorkerLoop) { IsBackground = true, Name = "LprImageUploader" };
            _worker.Start();
            Util.Logger.Log("[Uploader] 시작: " + ServerUrl);
        }

        public static void Stop()
        {
            _running = false;
            try { _queue?.CompleteAdding(); } catch { }
            try { _worker?.Join(2000); } catch { }
            Util.Logger.Log("[Uploader] 중지");
        }

        /// <summary>SaveImage 직후 호출. fire-and-forget — 실패해도 LPR 동작 영향 없음.</summary>
        /// <param name="localPath">방금 저장된 로컬 jpg 전체 경로 (예: D:\image\20260520\CH01_1234가1234_xxx.jpg)</param>
        /// <param name="dateDir">날짜 폴더 (예: 20260520)</param>
        /// <param name="fileName">파일명만 (예: CH01_1234가1234_xxx.jpg)</param>
        public static void Enqueue(string localPath, string dateDir, string fileName)
        {
            if (!Enabled || _queue == null) return;
            if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(dateDir) || string.IsNullOrEmpty(fileName)) return;

            try
            {
                _queue.Add(new UploadItem
                {
                    LocalPath = localPath,
                    DateDir   = dateDir,
                    FileName  = fileName,
                    Retry     = 0
                });
            }
            catch (Exception e)
            {
                Util.Logger.Log("[Uploader] Enqueue 실패: " + e.Message);
            }
        }

        private static void WorkerLoop()
        {
            try
            {
                foreach (var item in _queue.GetConsumingEnumerable())
                {
                    if (!_running) break;

                    bool ok = false;
                    try { ok = UploadOne(item).GetAwaiter().GetResult(); }
                    catch (Exception ex) { Util.Logger.Log("[Uploader] WorkerLoop 예외: " + ex.Message); }

                    if (!ok && item.Retry < 3)
                    {
                        Thread.Sleep(1000 * (item.Retry + 1));  // 1s, 2s, 3s 백오프
                        var retried = item;
                        retried.Retry++;
                        try { _queue.Add(retried); } catch { }
                    }
                }
            }
            catch (Exception e)
            {
                Util.Logger.Log("[Uploader] WorkerLoop 종료: " + e.Message);
            }
        }

        private static async Task<bool> UploadOne(UploadItem item)
        {
            try
            {
                if (!File.Exists(item.LocalPath))
                {
                    Util.Logger.Log("[Uploader] 파일 없음: " + item.LocalPath);
                    return false;
                }

                using (var content = new MultipartFormDataContent())
                using (var fs = File.OpenRead(item.LocalPath))
                {
                    var fileContent = new StreamContent(fs);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    content.Add(fileContent, "file", item.FileName);
                    content.Add(new StringContent(item.DateDir),  "dateDir");
                    content.Add(new StringContent(item.FileName), "fileName");
                    content.Add(new StringContent(ApiKey),        "apiKey");

                    string url = ServerUrl + "/api/lpr/upload";
                    using (var resp = await _http.PostAsync(url, content))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            Util.Logger.Log(string.Format("[Uploader] OK: {0}/{1}", item.DateDir, item.FileName));
                            return true;
                        }
                        else
                        {
                            string body = await resp.Content.ReadAsStringAsync();
                            Util.Logger.Log(string.Format("[Uploader] 실패 {0} (retry={1}): {2}",
                                (int)resp.StatusCode, item.Retry, body));
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Util.Logger.Log(string.Format("[Uploader] 예외 (retry={0}): {1}", item.Retry, e.Message));
                return false;
            }
        }
    }
}
