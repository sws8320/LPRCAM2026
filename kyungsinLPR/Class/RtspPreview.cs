using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace KyungsinLPR {
    /// <summary>
    /// Evo SDK OpenLiveView 실패 시 FFmpeg P/Invoke로 RTSP H.264 스트림을 디코딩하여
    /// PictureBox.Image에 프레임을 직접 표시하는 폴백 프리뷰.
    /// 사용 DLL: avformat-62, avcodec-62, avutil-60, swscale-9 (Evo SDK 배포 포함)
    /// </summary>
    internal sealed class RtspPreview : IDisposable {

        // ── P/Invoke ────────────────────────────────────────────────────
        private const string AV_FORMAT = "avformat-62";
        private const string AV_CODEC  = "avcodec-62";
        private const string AV_UTIL   = "avutil-60";
        private const string SW_SCALE  = "swscale-9";

        [DllImport(AV_FORMAT, CallingConvention = CallingConvention.Cdecl)]
        private static extern void avformat_network_init();
        [DllImport(AV_FORMAT, CallingConvention = CallingConvention.Cdecl)]
        private static extern int avformat_open_input(ref IntPtr ps,
            [MarshalAs(UnmanagedType.LPStr)] string url, IntPtr fmt, ref IntPtr options);
        [DllImport(AV_FORMAT, CallingConvention = CallingConvention.Cdecl)]
        private static extern int avformat_find_stream_info(IntPtr ic, ref IntPtr options);
        [DllImport(AV_FORMAT, CallingConvention = CallingConvention.Cdecl)]
        private static extern int av_find_best_stream(IntPtr ic, int type,
            int wanted, int related, ref IntPtr decoderRet, int flags);
        [DllImport(AV_FORMAT, CallingConvention = CallingConvention.Cdecl)]
        private static extern int av_read_frame(IntPtr s, IntPtr pkt);
        [DllImport(AV_FORMAT, CallingConvention = CallingConvention.Cdecl)]
        private static extern void avformat_close_input(ref IntPtr s);

        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr avcodec_alloc_context3(IntPtr codec);
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern int avcodec_open2(IntPtr avctx, IntPtr codec, ref IntPtr options);
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern int avcodec_send_packet(IntPtr avctx, IntPtr avpkt);
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern int avcodec_receive_frame(IntPtr avctx, IntPtr frame);
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr av_packet_alloc();
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern void av_packet_unref(IntPtr pkt);
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern void av_packet_free(ref IntPtr pkt);
        [DllImport(AV_CODEC, CallingConvention = CallingConvention.Cdecl)]
        private static extern void avcodec_free_context(ref IntPtr avctx);

        [DllImport(AV_UTIL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr av_frame_alloc();
        [DllImport(AV_UTIL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void av_frame_free(ref IntPtr frame);
        [DllImport(AV_UTIL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void av_frame_unref(IntPtr frame);
        [DllImport(AV_UTIL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int av_dict_set(ref IntPtr pm,
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [MarshalAs(UnmanagedType.LPStr)] string value, int flags);
        [DllImport(AV_UTIL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void av_dict_free(ref IntPtr m);

        [DllImport(SW_SCALE, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr sws_getContext(int srcW, int srcH, int srcFmt,
            int dstW, int dstH, int dstFmt, int flags,
            IntPtr a, IntPtr b, IntPtr c);
        [DllImport(SW_SCALE, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sws_scale(IntPtr ctx,
            IntPtr[] srcSlice, int[] srcStride, int srcSliceY, int srcSliceH,
            IntPtr[] dst, int[] dstStride);
        [DllImport(SW_SCALE, CallingConvention = CallingConvention.Cdecl)]
        private static extern void sws_freeContext(IntPtr ctx);

        // FFmpeg 7.x x64 AVFrame 필드 오프셋
        private const int OFF_DATA   = 0;   // data[0..7]: 8 × IntPtr (64바이트)
        private const int OFF_LS     = 64;  // linesize[0..7]: 8 × int (32바이트)
        private const int OFF_WIDTH  = 104; // int
        private const int OFF_HEIGHT = 108; // int
        private const int OFF_FMT    = 116; // int (AVPixelFormat)

        // AVPacket.stream_index 오프셋 (FFmpeg 7.x x64)
        private const int OFF_PKT_SIDX = 36;

        private const int AVMEDIA_TYPE_VIDEO = 0;
        private const int AV_PIX_FMT_RGB24   = 2;
        private const int SWS_BILINEAR       = 2;

        // ── 필드 ────────────────────────────────────────────────────────
        private readonly PictureBox _pic;
        private readonly string     _url;
        private Thread        _thread;
        private volatile bool _running;
        private bool          _disposed;

        public RtspPreview(PictureBox picBox, string rtspUrl) {
            _pic  = picBox;
            _url  = rtspUrl;
            _running = true;
            _thread = new Thread(DecodeLoop) { IsBackground = true, Name = "RtspPreview" };
            _thread.Start();
        }

        private void DecodeLoop() {
            avformat_network_init();
            while(_running) {
                bool ok = TryDecode();
                if(!_running) break;
                if(!ok) {
                    Util.Logger.Log("RtspPreview: 5초 후 재연결");
                    Thread.Sleep(5000);
                }
            }
        }

        private bool TryDecode() {
            IntPtr fmtCtx   = IntPtr.Zero;
            IntPtr codecCtx = IntPtr.Zero;
            IntPtr pkt      = IntPtr.Zero;
            IntPtr frame    = IntPtr.Zero;
            IntPtr swsCtx   = IntPtr.Zero;
            IntPtr opts     = IntPtr.Zero;

            try {
                av_dict_set(ref opts, "rtsp_transport", "tcp", 0);  // UDP 패킷 손실 방지
                av_dict_set(ref opts, "stimeout", "5000000", 0);     // 연결 타임아웃 5초

                if(avformat_open_input(ref fmtCtx, _url, IntPtr.Zero, ref opts) != 0) {
                    Util.Logger.Log("RtspPreview: 연결 실패 " + _url);
                    return false;
                }
                av_dict_free(ref opts);

                IntPtr siOpts = IntPtr.Zero;
                if(avformat_find_stream_info(fmtCtx, ref siOpts) < 0) {
                    Util.Logger.Log("RtspPreview: 스트림 정보 실패");
                    return false;
                }
                av_dict_free(ref siOpts);

                IntPtr codecRef = IntPtr.Zero;
                int videoIdx = av_find_best_stream(fmtCtx, AVMEDIA_TYPE_VIDEO, -1, -1, ref codecRef, 0);
                if(videoIdx < 0 || codecRef == IntPtr.Zero) {
                    Util.Logger.Log("RtspPreview: 비디오 스트림 없음");
                    return false;
                }

                codecCtx = avcodec_alloc_context3(codecRef);
                if(codecCtx == IntPtr.Zero) return false;

                IntPtr cOpts = IntPtr.Zero;
                if(avcodec_open2(codecCtx, codecRef, ref cOpts) < 0) {
                    Util.Logger.Log("RtspPreview: 디코더 열기 실패");
                    return false;
                }
                av_dict_free(ref cOpts);

                pkt   = av_packet_alloc();
                frame = av_frame_alloc();
                if(pkt == IntPtr.Zero || frame == IntPtr.Zero) return false;

                Util.Logger.Log("RtspPreview: 디코드 시작 " + _url);

                int lastW = 0, lastH = 0, lastFmt = -1;

                while(_running) {
                    if(av_read_frame(fmtCtx, pkt) < 0) break;

                    // 비디오 스트림 패킷만 처리
                    if(Marshal.ReadInt32(pkt, OFF_PKT_SIDX) != videoIdx) {
                        av_packet_unref(pkt);
                        continue;
                    }

                    avcodec_send_packet(codecCtx, pkt);
                    av_packet_unref(pkt);

                    while(_running) {
                        if(avcodec_receive_frame(codecCtx, frame) < 0) break;

                        int w   = Marshal.ReadInt32(frame, OFF_WIDTH);
                        int h   = Marshal.ReadInt32(frame, OFF_HEIGHT);
                        int fmt = Marshal.ReadInt32(frame, OFF_FMT);

                        if(w > 0 && h > 0) {
                            // 해상도/포맷 변경 시 sws 컨텍스트 재생성
                            if(swsCtx == IntPtr.Zero || w != lastW || h != lastH || fmt != lastFmt) {
                                if(swsCtx != IntPtr.Zero) sws_freeContext(swsCtx);
                                swsCtx = sws_getContext(w, h, fmt, w, h, AV_PIX_FMT_RGB24,
                                    SWS_BILINEAR, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                                lastW = w; lastH = h; lastFmt = fmt;
                            }

                            if(swsCtx != IntPtr.Zero) {
                                var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                                var bd  = bmp.LockBits(new Rectangle(0, 0, w, h),
                                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                                // AVFrame data/linesize 배열 읽기
                                var srcPtrs   = new IntPtr[8];
                                var srcStride = new int[8];
                                for(int i = 0; i < 8; i++) {
                                    srcPtrs[i]   = Marshal.ReadIntPtr(frame, OFF_DATA + i * IntPtr.Size);
                                    srcStride[i] = Marshal.ReadInt32(frame,  OFF_LS   + i * 4);
                                }
                                var dstPtrs   = new IntPtr[] { bd.Scan0 };
                                var dstStride = new int[]    { bd.Stride };

                                sws_scale(swsCtx, srcPtrs, srcStride, 0, h, dstPtrs, dstStride);
                                bmp.UnlockBits(bd);

                                // UI 스레드에서 PictureBox 업데이트
                                if(!_pic.IsDisposed) {
                                    _pic.BeginInvoke(new Action<Bitmap>(UpdatePic), bmp);
                                } else {
                                    bmp.Dispose();
                                }
                            }
                        }

                        av_frame_unref(frame);
                    }
                }
                return true;
            } catch(Exception ex) {
                Util.Logger.Log("RtspPreview 예외: " + ex.Message);
                return false;
            } finally {
                if(swsCtx   != IntPtr.Zero) sws_freeContext(swsCtx);
                if(frame    != IntPtr.Zero) av_frame_free(ref frame);
                if(pkt      != IntPtr.Zero) av_packet_free(ref pkt);
                if(codecCtx != IntPtr.Zero) avcodec_free_context(ref codecCtx);
                if(fmtCtx   != IntPtr.Zero) avformat_close_input(ref fmtCtx);
                av_dict_free(ref opts);
            }
        }

        private void UpdatePic(Bitmap newBmp) {
            if(_pic.IsDisposed) { newBmp.Dispose(); return; }
            var old = _pic.Image;
            _pic.Image = newBmp;
            old?.Dispose();
        }

        public void Stop() { _running = false; }

        public void Dispose() {
            if(_disposed) return;
            _disposed = true;
            Stop();
            _thread?.Join(2000);
            var img = _pic.IsDisposed ? null : _pic.Image;
            if(img != null && !_pic.IsDisposed) {
                _pic.BeginInvoke(new Action(() => {
                    _pic.Image = null;
                    img.Dispose();
                }));
            }
        }
    }
}
