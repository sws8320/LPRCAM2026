using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;   // JavaScriptSerializer (System.Web.Extensions, .NET 내장)
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace KyungsinLPR
{
    /// <summary>
    /// 학습된 CRNN+CTC 번호판 인식기(ONNX) 추론 엔진.
    /// D:\license-plate-recognition\export_onnx.py 로 만든 plate_crnn.onnx + plate_crnn.json 사용.
    ///
    /// 입력 크롭(단일 라인 번호판) → grayscale(BT.601) → bilinear resize(WxH) →
    /// /255 후 (-mean)/std 정규화 → ONNX → CTC greedy decode → 문자열.
    /// (Python crnn_recognizer.py 의 전처리/디코딩과 동일하게 맞춤)
    ///
    /// charset 은 모델 파일이 아니라 json 에 들어있다 → 모델 교체 시 json 도 함께 교체.
    /// </summary>
    public sealed class CrnnOnnx : IDisposable
    {
        private InferenceSession _session;
        private string _charset;          // index i(0-based) → 문자, CTC class = i+1, blank=0
        private int _imgH = 32, _imgW = 192;
        private double _scale = 1.0 / 255.0, _mean = 0.5, _std = 0.5;
        private string _inputName = "input", _outputName = "logits";

        public string Charset { get { return _charset; } }
        public int InputH { get { return _imgH; } }
        public int InputW { get { return _imgW; } }
        public bool Ready { get { return _session != null; } }

        // JavaScriptSerializer 는 public 프로퍼티에 매핑된다.
        public class MetaNorm { public double scale { get; set; } public double mean { get; set; } public double std { get; set; } }
        public class Meta
        {
            public string charset { get; set; }
            public int img_h { get; set; }
            public int img_w { get; set; }
            public string input_name { get; set; }
            public string output_name { get; set; }
            public MetaNorm normalize { get; set; }
        }

        /// <summary>onnx + json 이 들어있는 폴더에서 로드. useGpu=true 면 CUDA EP 시도(실패 시 CPU).</summary>
        public void Load(string onnxPath, string jsonPath, bool useGpu)
        {
            var meta = new JavaScriptSerializer().Deserialize<Meta>(File.ReadAllText(jsonPath, Encoding.UTF8));
            _charset = meta.charset;
            _imgH = meta.img_h; _imgW = meta.img_w;
            _inputName = meta.input_name; _outputName = meta.output_name;
            if (meta.normalize != null)
            {
                _scale = meta.normalize.scale; _mean = meta.normalize.mean; _std = meta.normalize.std;
            }

            SessionOptions so = null;
            if (useGpu)
            {
                try { so = SessionOptions.MakeSessionOptionWithCudaProvider(0); }
                catch { so = null; }   // CUDA EP 없으면 CPU 폴백
            }
            _session = so != null ? new InferenceSession(onnxPath, so)
                                  : new InferenceSession(onnxPath);
        }

        /// <summary>크롭 한 장 → (번호판문자열, 신뢰도 0~1). 실패 시 ("",0).</summary>
        public KeyValuePair<string, float> Read(Bitmap crop)
        {
            if (_session == null || crop == null)
                return new KeyValuePair<string, float>("", 0f);

            float[] input = Preprocess(crop);   // [1,1,H,W]
            var tensor = new DenseTensor<float>(input, new[] { 1, 1, _imgH, _imgW });
            var feeds = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, tensor) };

            using (var results = _session.Run(feeds))
            {
                var outVal = results.First();
                var logits = outVal.AsTensor<float>();          // [1, T, C] (log_softmax)
                var dims = logits.Dimensions.ToArray();
                int T = dims[1], C = dims[2];
                var flat = logits.ToArray();                     // row-major [T*C]
                return CtcGreedyDecode(flat, T, C);
            }
        }

        // ---- 전처리: grayscale(BT.601) → bilinear resize → 정규화 -----------------
        private float[] Preprocess(Bitmap src)
        {
            // 1) 원본을 grayscale 버퍼(원본 크기)로
            int sw = src.Width, sh = src.Height;
            byte[] gray = ToGray(src, out sw, out sh);

            // 2) bilinear resize → (_imgW x _imgH)  (cv2.resize 기본 INTER_LINEAR 와 동일)
            float[] outBuf = new float[_imgH * _imgW];
            double fx = (double)sw / _imgW, fy = (double)sh / _imgH;
            for (int y = 0; y < _imgH; y++)
            {
                double sy = (y + 0.5) * fy - 0.5; if (sy < 0) sy = 0;
                int y0 = (int)Math.Floor(sy); double wy = sy - y0;
                int y1 = Math.Min(y0 + 1, sh - 1); if (y0 > sh - 1) y0 = sh - 1;
                for (int x = 0; x < _imgW; x++)
                {
                    double sx = (x + 0.5) * fx - 0.5; if (sx < 0) sx = 0;
                    int x0 = (int)Math.Floor(sx); double wx = sx - x0;
                    int x1 = Math.Min(x0 + 1, sw - 1); if (x0 > sw - 1) x0 = sw - 1;

                    double v00 = gray[y0 * sw + x0], v01 = gray[y0 * sw + x1];
                    double v10 = gray[y1 * sw + x0], v11 = gray[y1 * sw + x1];
                    double top = v00 + (v01 - v00) * wx;
                    double bot = v10 + (v11 - v10) * wx;
                    double v = top + (bot - top) * wy;           // 0~255

                    outBuf[y * _imgW + x] = (float)((v * _scale - _mean) / _std);
                }
            }
            return outBuf;
        }

        /// <summary>Bitmap → grayscale byte[](row-major, w*h). BT.601 luma(=cv2 BGR2GRAY).</summary>
        private static byte[] ToGray(Bitmap src, out int w, out int h)
        {
            w = src.Width; h = src.Height;
            byte[] gray = new byte[w * h];
            // 24bpp 로 통일 후 LockBits
            using (var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(bmp))
                    g.DrawImage(src, new Rectangle(0, 0, w, h));
                var rect = new Rectangle(0, 0, w, h);
                var bd = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    int stride = bd.Stride;
                    byte[] buf = new byte[stride * h];
                    Marshal.Copy(bd.Scan0, buf, 0, buf.Length);
                    for (int y = 0; y < h; y++)
                    {
                        int rs = y * stride, ro = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            int p = rs + x * 3;       // B,G,R 순(24bppRgb는 BGR 메모리 배열)
                            byte b = buf[p], gr = buf[p + 1], r = buf[p + 2];
                            gray[ro + x] = (byte)((r * 299 + gr * 587 + b * 114) / 1000);
                        }
                    }
                }
                finally { bmp.UnlockBits(bd); }
            }
            return gray;
        }

        // ---- CTC greedy decode (blank=0, class c→charset[c-1]) + 신뢰도 ----------
        private KeyValuePair<string, float> CtcGreedyDecode(float[] logp, int T, int C)
        {
            var sb = new StringBuilder();
            int prev = -1;
            double confSum = 0; int confCnt = 0;
            for (int t = 0; t < T; t++)
            {
                int baseIdx = t * C;
                int best = 0; float bestVal = logp[baseIdx];
                for (int c = 1; c < C; c++)
                {
                    float v = logp[baseIdx + c];
                    if (v > bestVal) { bestVal = v; best = c; }
                }
                if (best != 0)                       // blank 아니면 신뢰도 집계
                {
                    confSum += Math.Exp(bestVal);    // log_softmax → prob
                    confCnt++;
                }
                if (best != prev && best != 0)
                {
                    int ci = best - 1;               // class → charset index
                    if (ci >= 0 && ci < _charset.Length) sb.Append(_charset[ci]);
                }
                prev = best;
            }
            float conf = confCnt > 0 ? (float)(confSum / confCnt) : 0f;
            return new KeyValuePair<string, float>(sb.ToString(), conf);
        }

        public void Dispose()
        {
            try { _session?.Dispose(); } catch { }
            _session = null;
        }
    }
}
