using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Runtime.InteropServices;

namespace KyungsinLPR.iNova2 {
    /// <summary>
    /// An enum for IPCamera class's error code.
    /// </summary>
    public enum IPCamError
    {
        /// <summary>
        /// No error - the command was successfully executed.
        /// </summary>
        OK,

        /// <summary>
        /// Timeout 
        /// </summary>
        Timeout,

        /// <summary>
        /// Failed to decode the JPEG buffer
        /// </summary>
        DecodeFailure,

        /// <summary>
        /// The received JPEG buffer is broken. The buffer size is incorrect.
        /// </summary>
        BrokenBuffer_IllegalSize,

        /// <summary>
        /// The received JPEG buffer is broken. It doesn't have an EOI marker at the end of the buffer.
        /// </summary>
        BrokenBuffer_MissingEOI,

        /// <summary>
        /// The received UDP data is invalid due to missing packets.
        /// </summary>
        BrokenBuffer_MissingPackets,

        /// <summary>
        /// Stream port is not opened.
        /// </summary>
        StreamNotOpened,

        /// <summary>
        /// Error in socket operation
        /// </summary>
        SocketError,

        /// <summary>
        /// Command is not found in the camera.
        /// </summary>
        CommandNotFound,

        /// <summary>
        /// The passed value is not valid
        /// </summary>
        InvalidValue,

        /// <summary>
        /// The camera is not in the valid mode for the operation
        /// </summary>
        InvalidMode,

        /// <summary>
        /// The response from the camera is not valid
        /// </summary>
        InvalidResponse,

        /// <summary>
        /// The format of the command has errors.
        /// </summary>
        BadFormat,

        /// <summary>
        /// General operation failure.
        /// </summary>
        OperationFailure,

        /// <summary>
        /// The command is not implemented in the camera.
        /// </summary>
        NotImplemented,
        /// <summary>
        /// The password of the camera is default value.
        /// </summary>
        DefaultPassword
    }

    public enum Model
    {
        UNKNOWN,     // Unknown model
        iN_20,      // i-Nova1 (e2v, 2.0MP)
        iN2_32SC,   // i-Nova2 Standard (Sony, 3.2MP)
        iN2Z_32SC,  // i-Nova2 Zoom (Sony, 3.2MP)
        iN2_23SC,   // i-Nova2 Standard (Sony, 2.3MP)
        iN2_23SC_C, // i-Nova2 Compact (Sony, 2.3MP)
        iN2M_23SC,  // i-Nova2 Motor (Sony, 2.3MP)
        iN2M_23OC   // i-Nova2 Motor (Onsemi, 2.3MP)
    }

    /// <summary>
    /// Auto Luminance Control parameters.
    /// </summary>
    public class ALC
    {
        /// <summary>
        /// Enable Auto Exposure Control (AEC)
        /// </summary>
        public bool enableAEC = false;

        /// <summary>
        /// Enable Auto Gain Control (AGC)
        /// </summary>
        public bool enableAGC = false;

        /// <summary>
        /// Enable Auto Iris Control (AIC)
        /// </summary>
        public bool enableAIC = false;

        /// <summary>
        /// The target intensity value between 0 and 255.
        /// </summary>
        public int target = 128;

        /// <summary>
        /// Minimum exposure value in micro-seconds when AEC is enabled.
        /// </summary>
        public int minExposure = 23;

        /// <summary>
        /// Maximum exposure value in micro-seconds when AEC is enabled.
        /// </summary>
        public int maxExposure = 33 * 1000;

        /// <summary>
        /// Minimum gain value (the multiplication, not the dB) when AGC is enabled.
        /// </summary>
        public double minGain = 1.0;

        /// <summary>
        /// Maximum gain value (the multiplication, not the dB) when AGC is enabled.
        /// </summary>
        public double maxGain = 4.0;

        /// <summary>
        /// The proportional factor for the negative feedback loop of ALC control.
        /// </summary>
        public double p_factor = 0.25;
    };

    /// <summary>
    /// Auto White Balance Control parameters.
    /// </summary>
    public class AutoWhiteBalance
    {
        /// <summary>
        /// WhiteBalanceMode : Auto, AutoExt, Preset, Manual
        /// </summary>
        public int modeAWB = 0;

        /// <summary>
        /// Color R Gain means auto whitebalance red target value
        /// </summary>
        public int colorRGain = 25;

        /// <summary>
        /// Color G Gain means auto whitebalance green target value
        /// </summary>
        public int colorGGain = 25;

        /// <summary>
        /// Color B Gain means auto whitebalance blue target value
        /// </summary>
        public int colorBGain = 25;

        /// <summary>
        /// Color temperature mode, 0 : 3000K, 1 : 5000K, 2 : 8000K
        /// </summary>
        public int colorTemp= 0;

        /// <summary>
        /// Maual red gain
        /// </summary>
        public int RGain = 7;

        /// <summary>
        /// Manual blue gain
        /// </summary>
        public int BGain = 20;
    }

    /// <summary>
    /// A class to provide meta info of the image.
    /// </summary>
    public class MetaInfo
    {
        /// <summary>
        /// The type of the image
        /// </summary>
        public int Type;

        /// <summary>
        /// The size of the image
        /// </summary>
        public int Size;

        /// <summary>
        /// The frame count
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// The exposure value of the image in microseconds.
        /// </summary>
        public int Exposure;

        /// <summary>
        /// The gain value of the image.
        /// </summary>
        public double Gain;

        /// <summary>
        /// Hardware (external) trigger count. This is valid only in trigger mode.
        /// </summary>
        public int TriggerCount;

        public int ImageWidth;
        public int ImageHeight;

        /// <summary>
        /// The level of hardware (external) input trigger level. It is either High (1) or Low (0).
        /// </summary>
        public int TriggerLevel;

        public static MetaInfo CreateFromByteArray(byte[] array)
        {
            if (array.Length < 28) return null;

            var metainfo = new MetaInfo();
            metainfo.Type = FromByteArray(array, 0);
            metainfo.Size = FromByteArray(array, 4);
            metainfo.FrameCount = FromByteArray(array, 8);
            metainfo.Exposure = FromByteArray(array, 12);
            metainfo.Gain = (double)FromByteArray(array, 16) / 100;
            metainfo.TriggerCount = FromByteArray(array, 20);
            metainfo.ImageWidth = FromByteArray(array, 24);
            metainfo.ImageHeight = FromByteArray(array, 28);
            metainfo.TriggerLevel = FromByteArray(array, 32);
            return metainfo;
        }

        private static int FromByteArray(byte[] array, int offset)
        {
            return ( (array[offset + 0] << 24)
                    + (array[offset + 1] << 16)
                    + (array[offset + 2] << 8)
                    + array[offset + 3]);
        }
    }

    /// <summary>
    /// The class which provides the communication with Novitec IP Camera.
    /// </summary>
    public class IPCamera
    {
        /// <summary>
        /// Connect to the stream port of the camera. This must be called before calling GetImage and GetRawData commands.
        /// The UDP streaming outperforms TCP streaming in terms of maximum bandwidth while it is said that UDP communication
        /// is less reliable.
        /// </summary>
        /// <param name="ipAddress">IP Address of the camera to connect</param>
        /// <param name="useUDP">Use of UDP. If false, TCP is used.</param>
        /// <returns></returns>
        public IPCamError ConnectStreamPort(string ipAddress, bool useUDP = false)
        {
            m_isUDPStreaming = useUDP;

            if (useUDP)
                return ConnectStreamPortUDP(ipAddress);
            else
                return ConnectStreamPortTCP(ipAddress);
        }

        /// <summary>
        /// Close the stream port
        /// </summary>
        /// <returns></returns>
        public IPCamError DisconnectStreamPort()
        {
            if (m_isUDPStreaming)
            {
                if (m_sock_SRM_UDP == null) return IPCamError.StreamNotOpened;

                var data = Encoding.ASCII.GetBytes("DISCONNECT");
                m_sock_SRM_UDP.Send(data, data.Length);
                m_sock_SRM_UDP.Close();
                m_sock_SRM_UDP = null;
            }
            else
            {
                if (m_stream_SRM == null) return IPCamError.StreamNotOpened;

                m_stream_SRM.Close();
                m_stream_SRM = null;
                m_sock_SRM_TCP.Close();
                m_sock_SRM_TCP = null;
            }
            return IPCamError.OK;
        }

        /// <summary>
        /// Connect to the command port of the camera.
        /// This must be called before issuing any other commands except GetImage and GetRawData commands.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public IPCamError ConnectCommandPort(string ipAddress)
        {
            if (m_sock_CMD != null)
                DisconnectCommandPort();

            else
            {
                try
                {
                    m_sock_CMD = new TcpClient(AddressFamily.InterNetwork);
                    //m_sock_CMD = new TcpClient(ipAddress, COMMAND_PORT);
                    if (m_sock_CMD != null)
                    {
                        var result = m_sock_CMD.BeginConnect(ipAddress, COMMAND_PORT, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                        if (!success) return IPCamError.Timeout;

                        m_sock_CMD.EndConnect(result);

                        m_sock_CMD.ReceiveBufferSize = RECV_BUF_SIZE;
                        m_sock_CMD.NoDelay = true;
                        m_stream_CMD = m_sock_CMD.GetStream();

                    }
                }
                catch (SocketException)
                {
                    return IPCamError.SocketError;
                }
            }

            return m_sock_CMD != null ? IPCamError.OK : IPCamError.StreamNotOpened;
        }

        /// <summary>
        /// Close the command port
        /// </summary>
        /// <returns></returns>
        public IPCamError DisconnectCommandPort()
        {
            if (m_stream_CMD == null) return IPCamError.StreamNotOpened;

            m_stream_CMD.Close();
            m_stream_CMD = null;
            m_sock_CMD.Close();
            m_sock_CMD = null;
            return IPCamError.OK;
        }

        /// <summary>
        /// Tells if the command port is open
        /// </summary>
        /// <returns></returns>
        public bool IsCommandPortConnected()
        {
            if (m_sock_CMD == null || m_sock_CMD.Client == null) return false;
            return m_sock_CMD.Connected;
        }

        /// <summary>
        /// Tells if the stream port is open
        /// </summary>
        /// <returns></returns>
        public bool IsStreamPortConnected()
        {
            if (m_isUDPStreaming)
            {
                return (m_sock_SRM_UDP != null);
            }
            else
            {
                if (m_sock_SRM_TCP == null || m_sock_SRM_TCP.Client == null) return false;
                return m_sock_SRM_TCP.Connected;
            }
        }

        /// <summary>
        /// Get the buffer size in bytes for the last image acquired with GetImage(). This must be useful
        /// to adjust the trade-off between the image quality and buffer size.
        /// </summary>
        /// <returns></returns>
        public int GetLastImageBufferSize()
        {
            return m_lastImageBufferSize;
        }

        public IPCamError GetImage_TCP(int timeout, out MetaInfo metaInfo)
        {
            IPCamError err;
            metaInfo = null;
            if (m_stream_SRM == null) return IPCamError.StreamNotOpened;

            m_stream_SRM.ReadTimeout = timeout;
            int bytesRead = 0;

            byte[] headerBytes = new byte[STREAM_HEADER_LENGTH];
            try
            {
                bytesRead = m_stream_SRM.Read(headerBytes, 0, 4);
            }
            catch (IOException)
            {
                //Console.WriteLine("socket exception " + ex.ToString());
                return IPCamError.Timeout;
            }
            if (bytesRead == 0) return IPCamError.Timeout;

            uint bufLen = (uint)((headerBytes[0] << 24)
                        + (headerBytes[1] << 16)
                        + (headerBytes[2] << 8)
                        + headerBytes[3]);
            if (bufLen > MAX_IMAGE_SIZE)
            {
                // dummy read to erase all broken data from the stream buffer.
                m_stream_SRM.ReadTimeout = 20;
                lock (m_recvBufLock)
                {
                    m_recvBuf = new byte[MAX_IMAGE_SIZE];
                    try
                    {
                        do
                        {
                            bytesRead = m_stream_SRM.Read(m_recvBuf, 0, m_recvBuf.Length);
                        } while (m_stream_SRM.DataAvailable);
                    }
                    catch (IOException) { }
                }
                return IPCamError.BrokenBuffer_IllegalSize;
            }

            if (bufLen == 2) // If it's '2', it isn't a buffer length but the metainfo's "Type" field indicating the content is YUV.
            {
                // read the rest of the metainfo.
                try
                {
                    bytesRead = m_stream_SRM.Read(headerBytes, 4, headerBytes.Length - 4); // skip the first "Type" field which is already read.
                }
                catch (IOException)
                {
                    //Console.WriteLine("socket exception " + ex.ToString());
                    return IPCamError.Timeout;
                }
                if (bytesRead == 0) return IPCamError.Timeout;

                metaInfo = MetaInfo.CreateFromByteArray(headerBytes);
                m_curMetaInfo = metaInfo;

                if (metaInfo.ImageWidth != 1920 || metaInfo.Size > 1024 * 1024 * 4)
                {
                    // dummy read to erase all broken data from the stream buffer.
                    m_stream_SRM.ReadTimeout = 20;
                    lock (m_recvBufLock)
                    {
                        m_recvBuf = new byte[MAX_IMAGE_SIZE];
                        bytesRead = m_stream_SRM.Read(m_recvBuf, 0, m_recvBuf.Length);
                    }
                    return IPCamError.BrokenBuffer_IllegalSize;
                }

                err = GetYUV_TCP(timeout, metaInfo);
                if (err != IPCamError.OK) return err;

            }
            else if (bufLen > 0)// It is a buffer length so it must be JPEG.
            {
                // JPEG stream on TCP doesn't have metainfo so we need to create a fake one.
                metaInfo = new MetaInfo();
                metaInfo.Type = 1; // JPEG
                metaInfo.Size = (int)bufLen;

                byte[] jpeg;
                err = GetJPG_TCP(timeout, out jpeg, metaInfo);
                if (err != IPCamError.OK) return err;

                // Check if the EOI marker exists at the end of the buffer
                if (jpeg[jpeg.Length - 1] != 0xd9 || jpeg[jpeg.Length - 2] != 0xff)
                {
                    return IPCamError.BrokenBuffer_MissingEOI;
                }

                using (MemoryStream jpegStrm = new MemoryStream(jpeg))
                {
                    JpegBitmapDecoder decoder;
                    try
                    {
                        decoder = new JpegBitmapDecoder(jpegStrm,
                                                        BitmapCreateOptions.None,
                                                        BitmapCacheOption.OnLoad);
                    }
                    catch (Exception)
                    {
                        return IPCamError.DecodeFailure;
                    }

                    m_bitmapSource = decoder.Frames[0];
                    return IPCamError.OK;
                }
            }
            else
            {
                return IPCamError.BrokenBuffer_IllegalSize;
            }

            return err;
        }

        private IPCamError GetJPG_TCP(int timeout, out byte[] jpegBuffer, MetaInfo metaInfo)
        {
            jpegBuffer = null;
            int bytesRead = 0;
            int totalBytesRead = 0;

            lock (m_recvBufLock)
            {
                m_recvBuf = new byte[metaInfo.Size];
                do
                {
                    //Thread.Sleep(3); // This seems to fix the issue with .NET 3.5 and trigger mode.
                    try
                    {
                        bytesRead = m_stream_SRM.Read(m_recvBuf,
                                                    totalBytesRead,
                                                    metaInfo.Size - totalBytesRead);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("socket exception " + ex.ToString());
                        return IPCamError.SocketError;
                    }
                    totalBytesRead += bytesRead;
                } while (totalBytesRead < metaInfo.Size);
            }

            m_lastImageBufferSize = metaInfo.Size;
            jpegBuffer = m_recvBuf;
            return IPCamError.OK;
        }

        private IPCamError GetYUV_TCP(int timeout, MetaInfo metaInfo)
        {
            int totalBytesRead = 0;
            int bytesRead = 0;

            lock (m_recvBufLock)
            {
                m_recvBuf = new byte[metaInfo.Size];
                m_YUV2RGB_Converter.InitYUVtoRGB_incremental(metaInfo.ImageWidth, metaInfo.ImageHeight, m_recvBuf);
                do
                {
                    try
                    {
                        bytesRead = m_stream_SRM.Read(m_recvBuf,
                                                    totalBytesRead,
                                                    metaInfo.Size - totalBytesRead);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("socket exception " + ex.ToString());
                        return IPCamError.SocketError;
                    }
                    totalBytesRead += bytesRead;
                    m_YUV2RGB_Converter.DoYUVtoRGB_incremental(totalBytesRead);
                } while (totalBytesRead < metaInfo.Size);

                m_lastImageBufferSize = metaInfo.Size;
            }

            return IPCamError.OK;
        }

        public IPCamError GetImage_UDP(int timeout, out MetaInfo metaInfo)
        {
            IPCamError err;
            metaInfo = null;

            try
            {
                m_sock_SRM_UDP.Client.ReceiveTimeout = timeout;
                byte[] recv_data = null;

                // Search for the header packet.
                do
                {
                    recv_data = m_sock_SRM_UDP.Receive(ref m_ep);
                } while (recv_data.Length != STREAM_HEADER_LENGTH);

                metaInfo = MetaInfo.CreateFromByteArray(recv_data);
                int imageSize = metaInfo.Size;
                if (imageSize > 1024 * 1024 * 4 || imageSize < 0)
                    return IPCamError.BrokenBuffer_IllegalSize;
                m_curMetaInfo = metaInfo;

                if (metaInfo.Type == 1) // JPEG?
                {
                    byte[] jpeg;
                    err = GetJPG_UDP(timeout, out jpeg, metaInfo);
                    if (err != IPCamError.OK) return err;

                    // Check if the EOI marker exists at the end of the buffer
                    if (jpeg[jpeg.Length - 1] != 0xd9 || jpeg[jpeg.Length - 2] != 0xff)
                    {
                        return IPCamError.BrokenBuffer_MissingEOI;
                    }

                    using (MemoryStream jpegStrm = new MemoryStream(jpeg))
                    {
                        JpegBitmapDecoder decoder;
                        try
                        {
                            decoder = new JpegBitmapDecoder(jpegStrm,
                                                            BitmapCreateOptions.None,
                                                            BitmapCacheOption.OnLoad);
                        }
                        catch (Exception)
                        {
                            return IPCamError.DecodeFailure;
                        }

                        m_bitmapSource = decoder.Frames[0];
                        return IPCamError.OK;
                    }
                }
                else if (metaInfo.Type == 2) // YUV
                {
                    err = GetYUV_UDP(timeout, metaInfo);
                    if (err != IPCamError.OK) return err;
                }
                else if (metaInfo.Type == 100) // PING
                {
                    // Do nothing for now.
                    return IPCamError.Timeout;
                }
            }
            catch (Exception)
            {
                // TODO: need to check other errors.
                return IPCamError.Timeout;
            }

            return IPCamError.OK;
        }

        private IPCamError GetJPG_UDP(int timeout, out byte[] jpegBuffer, MetaInfo metaInfo)
        {
            jpegBuffer = null;

            try
            {
                byte[] recv_data = null;

                jpegBuffer = new byte[metaInfo.Size];
                int receivedLen = 0;
                while (receivedLen < metaInfo.Size)
                {
                    recv_data = m_sock_SRM_UDP.Receive(ref m_ep);
                    if (recv_data.Length > metaInfo.Size - receivedLen)
                    {
                        return IPCamError.BrokenBuffer_IllegalSize;
                    }
                    Array.Copy(recv_data, 0, jpegBuffer, receivedLen, recv_data.Length);
                    receivedLen += recv_data.Length;
                }
                m_lastImageBufferSize = metaInfo.Size;
                m_recvBuf = jpegBuffer;
            }
            catch (Exception /*ex*/)
            {
                // TODO: need to check other errors.
                return IPCamError.Timeout;
            }
            return IPCamError.OK;
        }

        private IPCamError GetYUV_UDP(int timeout, MetaInfo metaInfo)
        {
            try
            {
                // Receive the image buffer.
                m_recvBuf = new byte[metaInfo.Size];
                m_YUV2RGB_Converter.InitYUVtoRGB_incremental(metaInfo.ImageWidth, metaInfo.ImageHeight, m_recvBuf);

                int receivedLen = 0;
                int packet_size = 1472;
                int last_uv_packet_size = (metaInfo.ImageWidth * metaInfo.ImageHeight / 2) % packet_size;

                while (receivedLen < metaInfo.Size)
                {
                    byte[] recv_data = m_sock_SRM_UDP.Receive(ref m_ep);

                    // The last packet has arrived but the total size doesn't match?
                    if (recv_data.Length < packet_size && receivedLen + recv_data.Length < metaInfo.Size
                        && !(receivedLen == (metaInfo.ImageWidth * metaInfo.ImageHeight / 2 - last_uv_packet_size) && recv_data.Length == last_uv_packet_size)) // excluding the last UV packet's case.
                    {
                        return IPCamError.BrokenBuffer_IllegalSize;
                    }

                    // Exceeded the expected size? We must have missed some packets.
                    if (recv_data.Length > metaInfo.Size - receivedLen)
                    {
                        return IPCamError.BrokenBuffer_IllegalSize;
                    }

                    if (recv_data.Length == 256 && (recv_data[0] == 0 && recv_data[1] == 0 && recv_data[2] == 0 && recv_data[3] == 2)) // frame header?
                    {
                        return IPCamError.BadFormat;
                    }
                    Array.Copy(recv_data, 0, m_recvBuf, receivedLen, recv_data.Length);
                    receivedLen += recv_data.Length;

                    m_YUV2RGB_Converter.DoYUVtoRGB_incremental(receivedLen);
                }
                m_lastImageBufferSize = metaInfo.Size;
            }
            catch (Exception)
            {
                return IPCamError.Timeout;
            }

            return IPCamError.OK;
        }

        protected BitmapSource m_bitmapSource;
        protected YUVtoRGB m_YUV2RGB_Converter = new YUVtoRGB();
        private MetaInfo m_curMetaInfo;

        /// <summary>
        /// Get an image from camera. The image is decoded to a bitmap so it is ready for image processing.
        /// Need to call ConnectStreamPort() before calling this method.
        /// </summary>
        /// <param name="timeout">Timeout value in milliseconds.</param>
        /// <param name="bitmap">(out) Bitmap of the image</param>
        /// <param name="metaInfo">(out) Metainfo of the image.</param>
        /// <returns>Error code</returns>
        public IPCamError GetImage(int timeout, out Bitmap bitmap, out MetaInfo metaInfo)
        {
            metaInfo = null;
            bitmap = null;
            IPCamError err;

            if (m_isUDPStreaming)
            {
                err = GetImage_UDP(timeout, out metaInfo);
                if (err != IPCamError.OK) return err;
            }
            else
            {
                err = SendPing();
                if (err != IPCamError.OK)
                    return err;

                err = GetImage_TCP(timeout, out metaInfo);
                if (err != IPCamError.OK) return err;

            }           

            if (metaInfo.Type == 1)
            {
                bitmap = IPCameraUtils.BitmapFromSource(m_bitmapSource);
            }
            else
            {
                bitmap = m_YUV2RGB_Converter.GetBitmap();
            }

            return err;
        }

        /// <summary>
        /// This is for compatibility with old SDK which didn't have MetaInfo
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public IPCamError GetImage(int timeout, out Bitmap bitmap)
        {
            MetaInfo metaInfo;
            return GetImage(timeout, out bitmap, out metaInfo);
        }

        /// <summary>
        /// Get an image from camera. The image is in BitmapSource object.
        /// Need to call ConnectStreamPort() before calling this method.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="bitmapSource"></param>
        /// <param name="metaInfo">(out) Metainfo of the image.</param>
        /// <returns></returns>
        public IPCamError GetImage(int timeout, out BitmapSource bitmapSource, out MetaInfo metaInfo)
        {
            metaInfo = null;
            bitmapSource = null;
            byte[] jpeg = null;
            IPCamError err;

            if (m_isUDPStreaming)
            {
                err = GetImage_UDP(timeout, out metaInfo);
                if (err != IPCamError.OK) return err;
            }
            else
            {
                err = SendPing();
                if (err != IPCamError.OK) return err;

                err = GetImage_TCP(timeout, out metaInfo);
                if (err != IPCamError.OK) return err;
            }

            m_curMetaInfo = metaInfo;

            if (metaInfo.Type == 1)
            {
                bitmapSource = m_bitmapSource;
            }
            else
            {
                bitmapSource = m_YUV2RGB_Converter.GetBitmapSource_YUVtoRGB_incremental();
            }

            return err;
        }

        /// <summary>
        /// The image acquisition function without meta info.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="bitmapSource"></param>
        /// <returns></returns>
        public IPCamError GetImage(int timeout, out BitmapSource bitmapSource)
        {
            MetaInfo metaInfo;
            return GetImage(timeout, out bitmapSource, out metaInfo);
        }

        /// <summary>
        /// For TCP streaming, client has to send Ping message to camera periodically to sustain the connection.
        /// Users don't need to call this as long as they use GetImage methods.
        /// Only needed for those directly call GetRawDataTCP.
        /// </summary>
        /// <returns></returns>
        public IPCamError SendPing()
        {
            if (m_stream_SRM == null) return IPCamError.StreamNotOpened;

            byte[] ping = { 80, 73, 78, 71 }; // "PING"
            try
            {
                m_stream_SRM.Write(ping, 0, 4);
                m_stream_SRM.Flush();
            }
            catch (Exception)
            {
                return IPCamError.SocketError;
            }

            return IPCamError.OK;
        }

        /// <summary>
        /// Set the trigger mode of the camera.
        /// </summary>
        /// <param name="mode">0: Free run, 1: One-shot trigger (grabs fixed (one or more) number of frames for each signal edge, 2: Mixed trigger (keeps grabbing while the signal is active, 3: Pseudo trigger.)</param>
        /// <param name="isActiveHi">Polarity of the trigger. True for rising edge, false for falling edge</param>
        /// <param name="minTriggerActiveWidth">Minimum trigger pulse width in microseconds of debouncer function. If the pulse's active duration is less than this, the trigger is ignored. Only available for one-shot trigger mode.</param>
        /// <param name="minTriggerInactiveWidth">Minimum trigger pulse width in microseconds of debouncer function. This is to ignore short pulses of inactive level. Only available for one-shot trigger mode.</param>
        /// <returns></returns>
        /// 
        public IPCamError SetTriggerMode(int mode, bool isActiveHi, int minTriggerActiveWidth, int minTriggerInactiveWidth)
        {
            string polarity = isActiveHi ? "H" : "L";
            string response;
            return SendCommand(String.Format("SetTriggerMode {0} {1} {2} {3}", mode, polarity, minTriggerActiveWidth, minTriggerInactiveWidth), out response);
        }

        public IPCamError SetTriggerMode(int mode, bool isActiveHi)
        {
            return SetTriggerMode(mode, isActiveHi, 0, 0);
        }

        /// <summary>
        /// Get the current trigger mode and the polarity of the trigger.
        /// </summary>
        /// <param name="mode">Trigger Mode</param>
        /// <param name="isActiveHi">true: Active High, false: Active Low</param>
        /// <param name="minActivePulse">Minimum trigger pulse width. Supported by firmware version 1.2.0 or later.</param>
        /// <param name="minInactivePulse">Minimum trigger pulse width. Supported by firmware version 1.3.0 or later.</param>
        /// <returns></returns>
        public IPCamError GetTriggerMode(out int mode, out bool isActiveHi, out int minActivePulse, out int minInactivePulse)
        {
            mode = 0;
            isActiveHi = false;
            minActivePulse = -1;
            minInactivePulse = -1;

            string resp;
            var ret = SendCommand("GetTriggerMode", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        mode = Convert.ToInt32(words[1]);
                        isActiveHi = words[2] == "H";

                        if (words.Length > 3)
                        {
                            minActivePulse = Convert.ToInt32(words[3]);
                        }
                        if (words.Length > 4)
                        {
                            minInactivePulse = Convert.ToInt32(words[4]);
                        }
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }

                    return ret;
                }
            }
            return ret;
        }


        public IPCamError GetTriggerMode(out int mode, out bool isActiveHi)
        {
            int minActivePulse, minInactivePulse;
            return GetTriggerMode(out mode, out isActiveHi, out minActivePulse, out minInactivePulse);
        }

        /// <summary>
        /// Set Trigger Source : H/W or S/W
        /// </summary>
        /// <param name="isSWTrigger"> true: SW Trigger, false: HW Trigger</param>
        /// <returns></returns>
        public IPCamError SetTriggerSource(bool isSWTrigger)
        {
            string response;
            if (isSWTrigger)
                return SendCommand("SetTriggerSource 1", out response);
            else
                return SendCommand("SetTriggerSource 0", out response);
        }

        /// <summary>
        /// Get Trigger Source.
        /// </summary>
        /// <param name="isSWTrigger"> true: SW Trigger, false: HW Trigger</param>
        /// <returns></returns>
        public IPCamError GetTriggerSource(out bool isSWTrigger)
        {
            string resp;
            var ret = SendCommand("GetTriggerSource", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        isSWTrigger = Convert.ToInt32(words[1]) == 1;
                    }
                    catch (FormatException)
                    {
                        isSWTrigger = false;
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
            }
            isSWTrigger = false;
            return ret;
        }

        /// <summary>
        /// Set Trigger Source for i-Nova2 : HW + SW or SW or HW
        /// </summary>
        /// <param name="trigSrc"> 0: HW trigger only, 1: SW trigger only, 2: Both SW and HW</param>
        /// <returns></returns>
        public IPCamError SetTriggerSource2(int trigSrc)
        {
            string response;
            if (trigSrc == 0)
                return SendCommand("SetTriggerSource 0", out response);
            else if (trigSrc == 1)
                return SendCommand("SetTriggerSource 1", out response);
            else if (trigSrc == 2)
                return SendCommand("SetTriggerSource 2", out response);
            else
                return IPCamError.InvalidValue;
        }

        /// <summary>
        /// Get Trigger Source for i-Nova2.
        /// </summary>
        /// <param name="trigSrc"> 0: HW trigger only, 1: SW trigger only, 2: Both SW and HW</param>
        /// <returns></returns>
        public IPCamError GetTriggerSource2(out int trigSrc)
        {
            string resp;
            var ret = SendCommand("GetTriggerSource", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        trigSrc = Convert.ToInt32(words[1]);
                    }
                    catch (FormatException)
                    {
                        trigSrc = 2;
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
            }
            trigSrc = 2;
            return ret;
        }

        /// <summary>
        /// Set the output of GPIO pin.
        /// </summary>
        /// <param name="value">true: high level, false: low level</param>
        /// <returns></returns>
        ///           
        public IPCamError SetGPIO(bool value)
        {
            string response;
            if (value)
                return SendCommand("SetGPIO 11 H", out response);
            else
                return SendCommand("SetGPIO 11 L", out response);
        }

        public IPCamError GetGPIO(out bool gpio)
        {
            gpio = false;
            string resp;
            var ret = SendCommand("GetGPIO", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    gpio = words[1] == "L" ? false : true;
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the GPIO output port type. This is for i-Nova2 only.
        /// </summary>
        /// <param name="port">The output port of the GPIO connector. 1 for EXT-OUT1 and 2 for EXT-OUT2.</param>
        /// <param name="type">The output type for the specified port. 0 for strobe output and 1 for GPIO output.</param>
        /// <returns></returns>
        public IPCamError SetOutputPort(int port, int type)
        {
            string response;

            if (port != 1 && port != 2)
                return IPCamError.InvalidValue;

            if (type != 0 && type != 1)
                return IPCamError.InvalidValue;

            return SendCommand(string.Format("SetOutputPort {0} {1}", port, type), out response);
        }

        /// <summary>
        /// Retrieve the output port type information. See also SetOutputPort method.
        /// </summary>
        /// <param name="out1_type">The output type of EXT-OUT1.</param>
        /// <param name="out2_type">The output type of EXT-OUT2.</param>
        /// <returns></returns>
        public IPCamError GetOutputPort(out int out1_type, out int out2_type)
        {
            out1_type = -1;
            out2_type = -1;

            string resp;
            var ret = SendCommand("GetOutputPort", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Length > 2)
                {
                    out1_type = (words[1] == "0") ? 0 : 1;
                    out2_type = (words[2] == "0") ? 0 : 1;
                    return ret;
                }
                else
                    return ret;
            }

            return ret;
        }


        /// <summary>
        /// Set the total gain of the camera.
        /// </summary>
        /// <param name="value">The multiplier value of the gain.</param>
        /// <returns></returns>
        public IPCamError SetTotalGain(double value)
        {
            if (value < 1) return IPCamError.InvalidValue;

            string response;
            return SendCommand(string.Format("SetTotalGain {0:F4}", value), out response);
        }

        /// <summary>
        /// Set the analog gain of the camera.
        /// </summary>
        /// <param name="value">The analog gain value. It has to be between 0 and 7.</param>
        /// <returns></returns>
        public IPCamError SetAnalogGain(int value)
        {
            if (value < 0) return IPCamError.InvalidValue;
            if (value > 7) return IPCamError.InvalidValue;

            string response;
            return SendCommand("SetAnalogGain " + value, out response);
        }

        /// <summary>
        /// Get the Analog gain of the camera.
        /// </summary>
        /// <param name="analog_gain">The Analog gain value. It has to be between 0 and 6.</param>
        /// <returns></returns>
        public IPCamError GetAnalogGain(out int analog_gain)
        {
            string resp;
            var ret = SendCommand("GetAnalogGain", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    int AGain = 0;
                    try
                    {
                        AGain = Convert.ToInt32(words[1]);
                    }
                    catch (Exception)
                    {
                        analog_gain = 0;
                        return ret;
                    }
                    if (AGain > 6) analog_gain = 6;
                    else if (AGain < 0) analog_gain = 0;
                    else analog_gain = AGain;
                    return ret;
                }
                else
                {
                    analog_gain = 0;
                    return IPCamError.InvalidResponse;
                }
            }
            else
            {
                analog_gain = 0;
                return IPCamError.InvalidResponse;
            }
        }

        /// <summary>
        /// Get the Digital gain of the camera.
        /// </summary>
        /// <param name="digital_gain">The Digital gain value.</param>
        /// <returns></returns>
        public IPCamError GetDigitalGain(out double digital_gain)
        {
            string resp;
            var ret = SendCommand("GetDigitalGain", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        digital_gain = Convert.ToDouble(words[1]);
                    }
                    catch (FormatException)
                    {
                        digital_gain = 1.0;
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    digital_gain = 1;
                    return IPCamError.InvalidResponse;
                }
            }
            else
            {
                digital_gain = 1;
                return IPCamError.InvalidResponse;
            }
        }

        /// <summary>
        /// Get the Total gain of the camera.
        /// </summary>
        /// <param name="total_gain">The Digital gain value.</param>
        /// <returns></returns>
        public IPCamError GetTotalGain(out double total_gain)
        {
            string resp;
            var ret = SendCommand("GetTotalGain", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        total_gain = Convert.ToDouble(words[1]);
                        if (total_gain == 0.0)
                        {
                            return IPCamError.InvalidResponse; // something is wrong.
                        }
                    }
                    catch (FormatException)
                    {
                        total_gain = 0.0;
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    total_gain = 1;
                    return IPCamError.InvalidResponse; // error
                }
            }
            else
            {
                total_gain = 1;
                return IPCamError.InvalidResponse; // Error
            }
        }

        /// <summary>
        /// Set the digital gain of the camera.
        /// </summary>
        /// <param name="value">The digital gain value.</param>
        /// <returns></returns>
        public IPCamError SetDigitalGain(double value)
        {
            string resp;
            return SendCommand("SetDigitalGain " + value, out resp);
        }

        /// <summary>
        /// Set the black level.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public IPCamError SetBlackLevel(int value)
        {
            if (value < -128) return IPCamError.InvalidValue;
            if (value > 127) return IPCamError.InvalidValue;

            string resp;
            return SendCommand("SetBlackLevel " + value, out resp);
        }

        /// <summary>
        /// Set the trigger image count. This determines the number of frames to be sent when a trigger is fired.
        /// The default value is 1.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public IPCamError SetTriggerImageCount(int num)
        {
            string resp;
            return SendCommand("SetTrigImgNum " + num, out resp);
        }

        /// <summary>
        /// Get the current trigger image count.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public IPCamError GetTriggerImageCount(out int num)
        {
            num = 0;
            string resp;
            var ret = SendCommand("GetTrigImgNum", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        num = Convert.ToInt32(words[1]);
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Fire a software trigger on camera. This should be used in trigger modes (one-shot, multi-shot).
        /// </summary>
        /// <returns></returns>
        public IPCamError SetForcedTrigger()
        {
            string resp;
            return SendCommand("SetForcedTrigger ON", out resp);
        }

        /// <summary>
        /// Set the bracket mode.
        /// In One Shot trigger mode, bracket mode's gains are fixed to first one's setting.
        /// </summary>
        /// <param name="isBrk">Enables/disables bracket mode.</param>
        /// <param name="bracketCount">The number of channels (1-4)</param>
        /// <returns></returns>
        public IPCamError SetBracketMode(bool isBrk, int bracketCount)
        {
            string resp;
            if (isBrk == true)
                return SendCommand("SetBracketMode ON " + bracketCount, out resp);
            else
                return SendCommand("SetBracketMode OFF", out resp);
        }

        /// <summary>
        /// Get the bracket mode.
        /// </summary>
        /// <param name="isbrkMode">(out)Tells if the bracket mode is enabled.</param>
        /// <param name="bracketCount">(out)The number of bracket channels.</param>
        /// <returns></returns>
        public IPCamError GetBracketMode(out bool isbrkMode, out int bracketCount)
        {
            isbrkMode = false;
            bracketCount = 0;
            string resp;
            var ret = SendCommand("GetBracketMode", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 2)
                {
                    try
                    {
                        isbrkMode = Convert.ToInt32(words[1]) == 1 ? true : false; // bracket Mode or Not
                        bracketCount = Convert.ToInt32(words[2]);                  // bracket Number
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;

        }

        /// <summary>
        /// Set camera's bracket settings. This method is for i-Nova1 only.
        /// </summary>
        /// <param name="ch">Channel number (0-3)</param>
        /// <param name="exposure">Exposure value in micro seconds</param>
        /// <param name="Again">Analog gain value.</param>
        /// <param name="DGain">Digital gain value.</param>
        /// <returns></returns>
        public IPCamError SetBracketInfo(int ch, int exposure, int Again, double DGain)
        {
            string resp;
            return SendCommand("SetBracketInfo " + ch + " " + exposure + " " + Again + " " + DGain, out resp);
        }

        /// <summary>
        /// Set camera's bracket settings. This method is for i-Nova2 only.
        /// </summary>
        /// <param name="ch">Channel number (0-3)</param>
        /// <param name="exposure">Exposure value in micro seconds</param>
        /// <param name="gain">Gain value.</param>
        /// <returns></returns>
        public IPCamError SetBracketInfo2(int ch, int exposure, int gain)
        {
            string resp;
            return SendCommand("SetBracketInfo " + ch + " " + exposure + " " + gain, out resp);
        }

        /// <summary>
        /// Get camera's bracket settings. This method is for i-Nova only.
        /// </summary>
        /// <param name="ch">Channel number (0-3)</param>
        /// <param name="exposure">(out)Exposure value in micro seconds</param>
        /// <param name="Again">(out)Analog gain value</param>
        /// <param name="Dgain">(out)Digital gain value</param>
        /// <returns></returns>
        public IPCamError GetBracketInfo(int ch, out int exposure, out int Again, out double Dgain)
        {
            Dgain = exposure = Again = 0;
            string resp;
            var ret = SendCommand("GetBracketInfo " + ch, out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 4) /////
                {
                    try
                    {
                        exposure = Convert.ToInt32(words[2]);
                        Again = Convert.ToInt32(words[3]);
                        Dgain = Convert.ToDouble(words[4]);
                    }
                    catch (Exception)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Get camera's bracket settings. This method is for i-Nova2 only.
        /// </summary>
        /// <param name="ch">Channel number (0-3)</param>
        /// <param name="exposure">(out)Exposure value in micro seconds</param>
        /// <param name="gain">(out)Gain value</param>
        /// <returns></returns>
        public IPCamError GetBracketInfo2(int ch, out int exposure, out int gain)
        {
            gain = exposure = 0;
            string resp;
            var ret = SendCommand("GetBracketInfo " + ch, out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 3)
                {
                    try
                    {
                        exposure = Convert.ToInt32(words[2]);
                        gain = Convert.ToInt32(words[3]);
                    }
                    catch (Exception)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the JPEG quality value manually. This is useful only when JPEG CBR (constant bit rate) is disabled.
        /// </summary>
        /// <param name="value">Quality value of JPEG between 1 and 63. Lower value gives better quality with larger image data.</param>
        /// <returns></returns>
        public IPCamError SetJPEGQuality(int value)
        {
            if (value < 1 || value > 99) return IPCamError.InvalidValue;

            string resp;
            return SendCommand("SetJPEGQuality " + value, out resp);
        }

        /// <summary>
        /// Get the current JPEG quality value.
        /// </summary>
        /// <param name="quality">Output JPEG quality value</param>
        /// <returns></returns>
        public IPCamError GetJPEGQuality(out int quality)
        {
            quality = 0;
            string resp;
            var ret = SendCommand("GetJPEGQuality", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        quality = Convert.ToInt32(words[1]);
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the camera to JPEG CBR (constant bit rate) mode.
        /// With this mode enabled, the camera decides the optimal JPEG quality value based on the
        /// specified bitrate setting.
        /// </summary>
        /// <param name="enable">Enable or disable</param>
        /// <param name="bitrate">Bitrate value to be applied. The value is in Mbps. (megabits per second)</param>
        /// <returns></returns>
        public IPCamError SetJPEGCBR(bool enable, double bitrate)
        {
            string resp;
            if (enable)
                return SendCommand("SetJPEGCBR ON " + bitrate, out resp);
            else
                return SendCommand("SetJPEGCBR OFF", out resp);
        }

        /// <summary>
        /// Get the current JPEG CBR status and the bit rate.
        /// </summary>
        /// <param name="enable">True if JPEG CBR is enabled.</param>
        /// <param name="bitrate">When JPEG CBR is enabled, this represents the current bit rate.</param>
        /// <returns></returns>
        public IPCamError GetJPEGCBR(out bool enable, out double bitrate)
        {
            enable = false;
            bitrate = 0;
            string resp;
            var ret = SendCommand("GetJPEGCBR", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    enable = words[1] == "Enabled";
                    try
                    {
                        bitrate = Convert.ToDouble(words[2]);
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set H.264 quality value.
        /// Please note that H.264 streaming has to be received via RTSP.
        /// </summary>
        /// <param name="value">Quality value of H.264 between 1 and 51. Lower value gives better quality with larger image data.</param>
        /// <returns></returns>
        public IPCamError SetH264Quality(int value)
        {
            if (value < 1 || value > 51) return IPCamError.InvalidValue;

            string resp;
            return SendCommand("SetH264Quality " + value, out resp);
        }

        /// <summary>
        /// Get the H.264 quality value.
        /// </summary>
        /// <param name="quality"></param>
        /// <returns></returns>
        public IPCamError GetH264Quality(out int quality)
        {
            quality = 0;
            string resp;
            var ret = SendCommand("GetH264Quality", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        quality = Convert.ToInt32(words[1]);
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the white balance of the camera.
        /// </summary>
        /// <param name="blueGain">The gain of blue channel relative to green, in decibel. </param>
        /// <param name="redGain">The gain of red channel relative to green, in decibel.</param>
        /// <returns></returns>
        public IPCamError SetWhiteBalance(double blueGain, double redGain)
        {
            if (blueGain < -20 || blueGain > 20) return IPCamError.InvalidValue;
            if (redGain < -20 || redGain > 20) return IPCamError.InvalidValue;

            double multiplier_R = Math.Pow(10, redGain / 20);
            double multiplier_B = Math.Pow(10, blueGain / 20);

            string cmd = string.Format("SetBlueRedGain {0:F4} {1:F4}", multiplier_B, multiplier_R);
            string resp;
            return SendCommand(cmd, out resp);
        }

        /// <summary>
        /// Get the current white balance (red/blue gains) of the camera.
        /// </summary>
        /// <param name="blueGain">Blue gain</param>
        /// <param name="redGain">Red gain</param>
        /// <returns></returns>
        public IPCamError GetWhiteBalance(out double blueGain, out double redGain)
        {
            blueGain = redGain = 0;
            string resp;
            var ret = SendCommand("GetBlueRedGain", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 3)
                {
                    try
                    {
                        blueGain = Math.Log(Convert.ToDouble(words[1]), 10) * 20;
                        redGain = Math.Log(Convert.ToDouble(words[2]), 10) * 20;
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the exposure (shutter speed) of the camera.
        /// </summary>
        /// <param name="microseconds">The exposure value in micro seconds.</param>
        /// <returns></returns>
        public IPCamError SetExposure(int microseconds)
        {
            if (microseconds < 0 || microseconds > 10 * 1000 * 1000) return IPCamError.InvalidValue;

            string resp;
            return SendCommand("SetExposure " + microseconds, out resp);
        }

        /// <summary>
        /// Get the manual exposure value in microseconds.
        /// </summary>
        /// <param name="microseconds"></param>
        /// <returns></returns>
        public IPCamError GetExposure(out int microseconds)
        {
            microseconds = 0;
            string resp;
            var ret = SendCommand("GetExposure", out resp);
            if (ret != IPCamError.OK) return ret;

            try
            {
                microseconds = Convert.ToInt32(resp.Substring(3));
            }
            catch (FormatException)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Set the frame rate of the camera.
        /// </summary>
        /// <param name="frameRate">The number of frames per second.</param>
        /// <returns></returns>
        public IPCamError SetFrameRate(double frameRate)
        {
            if (frameRate > 30 || frameRate < 0.1) return IPCamError.InvalidValue;

            string resp;
            return SendCommand("SetFrameRate " + frameRate, out resp);
        }

        /// <summary>
        /// Get the current frame rate value.
        /// </summary>
        /// <param name="fps"></param>
        /// <returns></returns>
        public IPCamError GetFrameRate(out double fps)
        {
            fps = 0;
            string resp;
            var ret = SendCommand("GetFrameRate", out resp);
            if (ret != IPCamError.OK) return ret;

            try
            {
                fps = Convert.ToDouble(resp.Substring(3));
            }
            catch (FormatException)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Set Flash mode (Enable/Disable/Auto)
        /// In Auto mode, the flash status is automatically determined based on the exposure value in auto exposure.
        /// </summary>
        /// <param name="mode"> Flash out mode. 0: Disable flash, 1: Enable flash, 2: Enable auto flash.</param>
        /// <param name="isActiveHi">Polarity of flash output.</param>
        /// <returns></returns>
        public IPCamError SetFlash(int mode, bool isActiveHi)
        {
            string resp;
            return SendCommand(string.Format("SetFlash {0} {1}", mode, isActiveHi ? "H" : "L"), out resp);
        }

        /// <summary>
        /// Get the current flash mode and the polarity of the flash.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="isActiveHi"></param>
        /// <returns></returns>
        public IPCamError GetFlash(out int mode, out bool isActiveHi)
        {
            mode = 0;
            isActiveHi = false;
            string resp;
            var ret = SendCommand("GetFlash", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        mode = Convert.ToInt32(words[1]);
                    }
                    catch (FormatException)
                    {
                        isActiveHi = false;
                        mode = 0;
                        return IPCamError.InvalidResponse;
                    }
                    isActiveHi = words[2] == "H";
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set auto flash parameters.
        /// The exposure values calculated by auto exposure are used to determine the flash status. 
        /// When the exposure is longer than the value specified in maxExposure, it is considered to be dark enough 
        /// and the flash is enabled (night mode).
        /// In night mode, when the exposure is shorter than the value specified in minExposure, it is considered to be
        /// bright enough and the flash is disabled (day mode).
        /// Optionally, filter switch and color/mono mode can be switched together with enabling and disabling flash.
        /// </summary>
        /// <param name="maxExposure">Set the exposure value to switch to the night mode.</param>
        /// <param name="minExposure">Set the exposure value to switch to the day mode.</param>
        /// <param name="controlFilterSwitch">Remove filter during night mode. This is necessary when using infra-red light for flash.</param>
        /// <param name="controlMono">Set to monochrome mode during night mode.</param>
        /// <returns></returns>
        public IPCamError SetAutoFlash(int maxExposure, int minExposure, bool controlFilterSwitch, bool controlMono)
        {
            string resp;
            return SendCommand(string.Format("SetAutoFlash {0} {1} {2} {3}",
                maxExposure,
                minExposure,
                controlFilterSwitch ? "ON" : "OFF",
                controlMono ? "ON" : "OFF"),
                out resp);
        }

        /// <summary>
        /// Get auto flash parameters.
        /// </summary>
        /// <param name="maxExposure"></param>
        /// <param name="minExposure"></param>
        /// <param name="controlFilterSwitch"></param>
        /// <param name="controlMono"></param>
        /// <returns></returns>
        public IPCamError GetAutoFlash(out int maxExposure, out int minExposure, out bool controlFilterSwitch, out bool controlMono)
        {
            maxExposure = 0;
            minExposure = 0;
            controlFilterSwitch = false;
            controlMono = false;
            string resp;
            var ret = SendCommand("GetAutoFlash", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 4)
                {
                    try
                    {
                        maxExposure = Convert.ToInt32(words[1]);
                        minExposure = Convert.ToInt32(words[2]);
                        if (words[3] == "ON")
                            controlFilterSwitch = true;
                        if (words[4] == "ON")
                            controlMono = true;
                    }
                    catch (FormatException)
                    {
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the delay amount when the flash is turned on.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public IPCamError SetFlashOnDelay(int num)
        {
            string resp;
            return SendCommand("SetFlashOnDelay " + num, out resp);
        }

        /// <summary>
        /// Get the flash-on delay value.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        public IPCamError GetFlashOnDelay(out int delay)
        {
            delay = 0;
            string resp;
            var ret = SendCommand("GetFlashOnDelay", out resp);
            if (ret == IPCamError.OK)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        delay = Convert.ToInt32(words[1]);
                    }
                    catch (FormatException)
                    {
                        return IPCamError.InvalidResponse;
                    }
                    return ret;
                }
                else
                {
                    return IPCamError.InvalidResponse;
                }
            }
            return ret;
        }

        /// <summary>
        /// Set the delay amount when the flash is turned off.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public IPCamError SetFlashOffDelay(int num)
        {
            string resp;
            return SendCommand("SetFlashOffDelay " + num, out resp);
        }

        /// <summary>
        /// Set Auto Luminance Control parameters.
        /// </summary>
        /// <param name="alc">The ALC information to be applied. See ALC class for detail.</param>
        /// <returns></returns>
        public IPCamError SetALC(ALC alc)
        {
            string command = string.Format("SetALC {0} {1} {2} {3} {4} {5} {6} {7} {8}",
                alc.enableAEC ? "ON" : "OFF",
                alc.enableAGC ? "ON" : "OFF",
                alc.target,
                alc.minExposure,
                alc.maxExposure,
                alc.minGain,
                alc.maxGain,
                alc.p_factor,
                alc.enableAIC ? "ON" : "OFF"
                );

            string resp;
            return SendCommand(command, out resp);
        }

        /// <summary>
        /// Get Auto Luminance Control parameters.
        /// </summary>
        /// <param name="alc">The current ALC information in the camera. See ALC class for detail.</param>
        /// <returns></returns>
        public IPCamError GetALC(out ALC alc)
        {
            alc = new ALC();

            string resp;
            var ret = SendCommand("GetALC", out resp);

            if (ret != IPCamError.OK) return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                alc.enableAEC = values[1] == "ON";
                alc.enableAGC = values[2] == "ON";
                if (values.Length >= 10)
                    alc.enableAIC = values[9] == "ON";
                alc.target = Convert.ToInt32(values[3]);
                alc.minExposure = Convert.ToInt32(values[4]);
                alc.maxExposure = Convert.ToInt32(values[5]);
                alc.minGain = Convert.ToDouble(values[6]);
                alc.maxGain = Convert.ToDouble(values[7]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Set ALC/AWB area on the image to calculate the average intensity/white-balance values.
        /// CAUTION: This function is preliminary and is subject to change.
        /// </summary>
        /// <param name="x">The horizontal start (left) position of the area.</param>
        /// <param name="y">The vertical start (top) position of the area.</param>
        /// <param name="width">The horizontal size of the area.</param>
        /// <param name="height">The vertical size of the area.</param>
        /// <returns></returns>
        public IPCamError SetALCArea(int x, int y, int width, int height)
        {
            string command = string.Format("SetALCArea {0} {1} {2} {3}", x, y, width, height);

            string resp;
            return SendCommand(command, out resp);
        }

        /// <summary>
        /// Get ALC/AWB area.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public IPCamError GetALCArea(out int x, out int y, out int width, out int height)
        {
            x = y = width = height = 0;

            string resp;
            var ret = SendCommand("GetALCArea", out resp);

            if (ret != IPCamError.OK) return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                x = Convert.ToInt32(values[1]);
                y = Convert.ToInt32(values[2]);
                width = Convert.ToInt32(values[3]);
                height = Convert.ToInt32(values[4]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Set the monochrome mode
        /// </summary>
        /// <param name="mode">0: Disable (Color mode), 1: Enable (Black and white mode)</param>
        /// <returns></returns>
        public IPCamError SetMonochrome(int mode)
        {
            string command = string.Format("SetMonochrome {0}", mode);
            string resp;
            return SendCommand(command, out resp);
        }

        public IPCamError GetMonochrome(out int mode)
        {
            mode = 0;
            string resp;
            var ret = SendCommand("GetMonochrome", out resp);
            if (ret != IPCamError.OK)
                return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                mode = Convert.ToInt32(values[1]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Set Auto White Balace mode. This function is for color model only.
        /// </summary>
        /// <param name="mode">0: Disable AWB, 1: Enable AWB(continuous), 2: Execute one-shot AWB.</param>
        /// <returns></returns>
        public IPCamError SetAWB(int mode)
        {
            var command = string.Format("SetAWB {0}", mode);
            string resp;
            return SendCommand(command, out resp);
        }

        /// <summary>
        /// This is for compatibility with old SDK.
        /// Set Auto White Balance mode. This function is for i-Nova2 color model only.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="color_gain"></param>
        /// <param name="color_temperature"></param>
        /// <param name="red_gain"></param>
        /// <param name="blue_gain"></param>
        /// <returns></returns>
        public IPCamError SetAWB2(int mode, int color_gain, int color_temperature, int red_gain, int blue_gain)
        {
            AutoWhiteBalance awb = new AutoWhiteBalance();
            awb.modeAWB = mode;
            awb.colorRGain = color_gain;
            awb.colorGGain = color_gain;
            awb.colorBGain = color_gain;
            awb.colorTemp = color_temperature;
            awb.RGain = red_gain;
            awb.BGain = blue_gain;

            return SetAWB2(awb);
        }

        /// <summary>
        /// Set Auto White Balance mode. This function is for i-Nova2 color model only.
        /// </summary>
        /// <param name="awb">The AutoWhiteBalance information to be applied. See AutoWhiteBalance class for detail.</param>
        /// <returns></returns>
        public IPCamError SetAWB2(AutoWhiteBalance awb)
        {
            // mode, R, G, B, Tmp, r, b
            var command = string.Format("SetAWB {0} {1} {2} {3} {4} {5} {6}",
                awb.modeAWB,
                awb.colorRGain,
                awb.colorGGain,
                awb.colorBGain,
                awb.colorTemp,
                awb.RGain,
                awb.BGain
                );

            string resp;
            return SendCommand(command, out resp);
        }

        /// <summary>
        /// Get Auto White Balance mode.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public IPCamError GetAWB(out int mode)
        {
            mode = 0;
            string resp;
            var ret = SendCommand("GetAWB", out resp);
            if (ret != IPCamError.OK)
                return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                mode = Convert.ToInt32(values[1]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Get Auto White Balance information. This function is for i-Nova2 color model only.
        /// </summary>
        /// <param name="awb">The AutoWhiteBalance information to be applied. See AutoWhiteBalance class for detail.</param>
        /// <returns></returns>
        public IPCamError GetAWB2(out AutoWhiteBalance awb)
        {
            awb = new AutoWhiteBalance();

            string resp;
            var ret = SendCommand("GetAWB", out resp);
            if (ret != IPCamError.OK)
                return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                awb.modeAWB = Convert.ToInt32(values[1]);
                awb.colorRGain = Convert.ToInt32(values[2]);
                awb.colorGGain = Convert.ToInt32(values[3]);
                awb.colorBGain = Convert.ToInt32(values[4]);

                awb.colorTemp = Convert.ToInt32(values[5]);
                awb.RGain = Convert.ToInt32(values[6]);
                awb.BGain = Convert.ToInt32(values[7]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Set color suppression threshold. This function is for i-Nova2 color model only.
        /// </summary>
        /// <param name="csup">Color suppression threshold.
        /// Recommended value for day time is 116 and night time is 80.
        /// Default value is 116.
        /// </param>
        /// <returns></returns>
        public IPCamError SetCsupTH(byte csup)
        {
            var command = string.Format("SetCsupTH {0}", csup);

            string resp;
            return SendCommand(command, out resp);
        }

        /// <summary>
        /// Get color suppression threshold. This function is for i-Nova2 color model only.
        /// </summary>
        /// <param name="csup">
        /// <returns></returns>
        public IPCamError GetCsupTH(out byte csup)
        {
            csup = 116; // default csup setting
            var command = string.Format("GetCsupTH");

            string resp;
            var ret = SendCommand(command, out resp);

            if (ret == IPCamError.OK)
            {
                string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                csup = Convert.ToByte(values[1]);
            }

            return ret;
        }

        public IPCamError SetWDR(int mode, int value)
        {
            var command = string.Format("SetWDR {0} {1}", mode, value);
            string resp;
            return SendCommand(command, out resp);
        }

        public IPCamError GetWDR(out int mode, out int value)
        {
            mode = 0;
            value = 0;
            string resp;
            var ret = SendCommand("GetWDR", out resp);
            if (ret != IPCamError.OK)
                return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                mode = Convert.ToInt32(values[1]);
                value = Convert.ToInt32(values[2]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        public IPCamError GetSmartBracket(out bool enable, out double[] ev_sequence)
        {
            enable = false;
            ev_sequence = new double[8];
            string resp;
            var ret = SendCommand("GetSmartBracket", out resp);
            if (ret != IPCamError.OK)
                return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                enable = Convert.ToInt32(values[1]) == 1;
                for (int i = 2; i < values.Length; i++)
                    ev_sequence[i - 2] = Convert.ToDouble(values[i]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        public IPCamError SetSmartBracket(bool enable, double[] ev_sequence)
        {
            string command = "SetSmartBracket ";
            command += enable ? "1 " : "0 ";
            for (int i = 0; i < ev_sequence.Length; i++)
                command += string.Format("{0:F1} ", ev_sequence[i]);
            string resp;
            return SendCommand(command, out resp);
        }

        /// <summary>
        /// Write a value to the register in camera's image sensor. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IPCamError WriteSensorRegister(int addr, int value)
        {
            string resp;
            return SendCommand("WriteSensorRegister " + addr + " " + value, out resp); ;
        }

        /// <summary>
        /// Write a value to the register in camera's ISP. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IPCamError WriteISPRegister(int addr, int value)
        {
            string resp;
            return SendCommand("WriteISPRegister " + addr + " " + value, out resp); ;
        }

        /// <summary>
        /// Read the register value in camera's image sensor. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IPCamError ReadSensorRegister(int addr, out int value)
        {
            value = 0;
            string resp;
            var ret = SendCommand("ReadSensorRegister " + addr, out resp);

            if (ret != IPCamError.OK) return ret;
            try
            {
                value = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                return ret;
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
        }

        /// <summary>
        /// Read the register value in camera's ISP. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IPCamError ReadISPRegister(int addr, out int value)
        {
            value = 0;
            string resp;
            var ret = SendCommand("ReadISPRegister " + addr, out resp);

            if (ret != IPCamError.OK) return ret;
            try
            {
                value = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                return ret;
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
        }

        /// <summary>
        /// Save the last image which is already received.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool SaveLastImage(string path)
        {
            if (m_curMetaInfo == null || m_curMetaInfo.Type == 1)
            {
                try
                {
                    lock (m_recvBufLock)
                    {
                        File.WriteAllBytes(path, m_recvBuf);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else // YUV
            {
                var bmp = m_YUV2RGB_Converter.GetBitmap(false);
                path = path.Substring(0, path.LastIndexOf('.')) + ".bmp";
                bmp.Save(path, ImageFormat.Bmp);
            }

            return true;
        }

        /// <summary>
        /// Readjust the zoom lens positions. (i-Nova2 Zoom only)
        /// When the camera lost the actual positions of zoom and focus, the user can let the camera to
        /// rescan the home position and then move back to the position which was last saved.
        /// This procedure may take a while (up to 1 minute) so please use this only when necessary.
        /// The camera can lose the lens positions when the position is moved but not saved, or
        /// it could happen due to mechanical vibration or the change of the camera pose.
        /// </summary>
        /// <returns></returns>
        public IPCamError ReadjustZoom()
        {
            string resp;
            var ret = SendCommand("ReadjustZoom", out resp);
            return ret;
        }

        /// <summary>
        /// Moves the zoom and focus positions of the lens. (i-Nova2 Zoom and i-Nova2S Motor models only)
        /// In i-Nova2 Zoom, the positions are absolute positions while in i-Nova2S Motor, they are relative positions from the current.
        /// </summary>
        /// <param name="zoom">Zoom position</param>
        /// <param name="focus">Focus position</param>
        /// <returns></returns>
        public IPCamError SetZoomFocusPosition(double zoom, double focus)
        {
            var command = string.Format("SetZoomFocusPosition {0} {1}", zoom, focus);
            string resp;
            var ret = SendCommand(command, out resp);
            return ret;
        }

        /// <summary>
        /// Get the zoom and focus positions of the lens. (i-Nova2 Zoom only)
        /// </summary>
        /// <param name="zoom">The current zoom position</param>
        /// <param name="focus">The current focus position</param>
        /// <returns></returns>
        public IPCamError GetZoomFocusPosition(out int zoom, out int focus)
        {
            zoom = focus = 0;
            var command = string.Format("GetZoomFocusPosition");
            string resp;
            var ret = SendCommand(command, out resp);

            if (ret != IPCamError.OK) return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                zoom = Convert.ToInt32(values[1]);
                focus = Convert.ToInt32(values[2]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        public IPCamError GetZoomFocusPositionError(out int zoomError, out int focusError)
        {
            zoomError = focusError = 0;
            var command = string.Format("GetZoomFocusPositionError");
            string resp;
            var ret = SendCommand(command, out resp);

            if (ret != IPCamError.OK) return ret;

            string[] values = resp.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                zoomError = Convert.ToInt32(values[1]);
                focusError = Convert.ToInt32(values[2]);
            }
            catch (Exception)
            {
                return IPCamError.InvalidResponse;
            }
            return ret;
        }

        /// <summary>
        /// Get the firmware version
        /// </summary>
        /// <returns></returns>
        public IPCamError GetFirmwareVersion(out string firmwareVersion)
        {
            firmwareVersion = null;
            string resp;
            var ret = SendCommand("GetFirmwareVersion", out resp);
            if (ret != IPCamError.OK)
                return ret;

            string version = resp.Substring(3); // skip "OK ".
            firmwareVersion = version.Trim();

            // Check if the version of i-Nova2 is older or equal to 0.5
            string[] splitVer = firmwareVersion.Split('.');
            if ((int.Parse(splitVer[0].Substring(8)) == 0) && (int.Parse(splitVer[1]) < 9)) m_version05 = true;
            else m_version05 = false;

            return IPCamError.OK;
        }

        /// <summary>
        /// Get the camera system information.
        /// </summary>
        /// <param name="info">The system information. Currently, this contains only camera's uptime since booting.</param>
        /// <returns></returns>
        public IPCamError GetSystemInfo(out string info)
        {
            info = string.Empty;
            string resp;
            var ret = SendCommand("GetSystemInfo", out resp);
            if (ret != IPCamError.OK) return ret;

            info = resp.Substring(3);
            return ret;
        }

        /// <summary>
        /// Get the serial number of the camera.
        /// </summary>
        /// <returns></returns>
        public IPCamError GetSerialNumber(out string serial)
        {
            IPCamError res = IPCamError.OK;
            serial = null;
            string resp;
            var ret = SendCommand("GetSerialNumber", out resp);
            if (ret != IPCamError.OK) return ret;

            serial = resp.Substring(3); // skip "OK ".
            serial = serial.Substring(0, serial.Length - 2); // remove CRLF.

            // Identify the camera model from serial number.
            try
            {
                var modelstr = serial.Substring(0, 4);

                if (modelstr.StartsWith("I20") || modelstr.StartsWith("S20"))
                    m_model = Model.iN_20;
                else if (modelstr.StartsWith("I30") || modelstr.StartsWith("S30"))
                    m_model = Model.iN2_32SC;
                else if (modelstr.StartsWith("I2ZS"))
                    m_model = Model.iN2Z_32SC;
                else if (modelstr.StartsWith("I2SS"))
                    m_model = Model.iN2_23SC;
                else if (modelstr.StartsWith("I2CS"))
                    m_model = Model.iN2_23SC_C;
                else if (modelstr.StartsWith("I2MS"))
                    m_model = Model.iN2M_23SC;
                else if (modelstr.StartsWith("I2MO"))
                    m_model = Model.iN2M_23OC;

                // for the old firmwares of new models
                else if (modelstr.StartsWith("I31"))
                    m_model = Model.iN2Z_32SC;
                else if (modelstr.StartsWith("I32"))
                    m_model = Model.iN2_23SC;
                else if (modelstr.StartsWith("I33"))
                    m_model = Model.iN2_23SC_C;
                else if (modelstr.StartsWith("I34") || modelstr.StartsWith("I35"))
                    m_model = Model.iN2M_23SC;

                // none of the above
                else
                    m_model = Model.UNKNOWN;
            }
            catch (FormatException)
            {
                m_model = Model.UNKNOWN;
            }

            return res;
        }

        /// <summary>
        /// Save the current camera settings to the camera so that the settings are retained after
        /// power-cycling the camera.
        /// CAUTION: Please avoid calling this too frequently, such as once in every minute.
        /// Saving setting causes erase-and-write cycle on the flash memory inside the camera which has a physical limit.
        /// </summary>
        /// <returns></returns>
        public IPCamError SaveSetting()
        {
            string resp;
            return SendCommand("SaveSetting", out resp);
        }

        /// <summary>
        /// Reset the camera. After calling this method, reconnection is needed to communicate with the camera again.
        /// </summary>
        /// <returns></returns>
        public IPCamError ResetCamera()
        {
            string resp;
            SendCommand("ResetCamera", out resp);
            // Camera would restart now so there would be no response.
            return IPCamError.OK;
        }

        /// <summary>
        /// Restore the factory-default setting. After calling this, the camera will reboot. (Need reconnection for further operation.)
        /// </summary>
        /// <returns></returns>
        public IPCamError RestoreDefaultSetting()
        {
            string resp;
            return SendCommand("RestoreDefaultSetting", out resp);

            // Camera would restart now so there would be no response.
            //return true;
        }

        /// <summary>
        /// Get the current trigger count in the camera.
        /// </summary>
        /// <returns></returns>
        public IPCamError GetTriggerCount(out int count)
        {
            count = -1;
            string resp;
            var ret = SendCommand("GetTriggerCount", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    count = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Reset the trigger count.
        /// </summary>
        /// <returns></returns>
        public IPCamError ResetTriggerCount()
        {
            string resp;
            var ret = SendCommand("ResetTriggerCount", out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Set the filter switch status. (i-Nova2 only)
        /// </summary>
        /// <param name="status">Status value. 0: Off, 1: On.</param>
        /// <returns></returns>
        public IPCamError SetFilterSwitch(int status)
        {
            string resp;
            var ret = SendCommand("SetFilterSwitch " + status, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get the current filter switch status. (i-Nova2 only)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public IPCamError GetFilterSwitch(out int val)
        {
            val = -1;
            string resp;
            var ret = SendCommand("GetFilterSwitch", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set the iris value. (For i-Nova2 Standard and Zoom only)
        /// For i-Nova2 Standard, this method is used only for starting iris calibration if a DC iris lens is attached to the camera.
        /// For i-Nova2 Zoom models, set the value to specify the aperture size. 
        /// Please note that the specification can be changed in the future release of the SDK.
        /// </summary>
        /// <param name="value">(i-Nova2 Standard) 1 for starting iris calibration. (i-Nova2 Zoom) A value of the aperture size. (0 - 1023)</param>
        /// <returns></returns>
        public IPCamError SetIris(int value)
        {
            string resp;
            var ret = SendCommand("SetIris " + value, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Set the iris value. (For i-Nova2 Motor models only)
        /// Please note that the specification can be changed in the future release of the SDK.
        /// </summary>
        /// <param name="value">Iris position value between 0 and 18 where 0 indicates full-close and 18 for full-open.</param>
        /// <returns></returns>
        public IPCamError SetIrisAbs(int value)
        {
            string resp;
            var ret = SendCommand("SetIris 0 " + value, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get the iris value. (i-Nova2 Zoom only)
        /// For i-Nova2 Zoom models, get the current value of the aperture size. 
        /// Please note that the specification can be changed in the future release of the SDK.
        /// </summary>
        /// <param name="val">The value of the aperture size.</param>
        /// <returns></returns>
        public IPCamError GetIris(out int val)
        {
            val = -1;
            string resp;
            var ret = SendCommand("GetIris", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set OSD (On Screen Display) mode. (i-Nova2 only)
        /// </summary>
        /// <param name="type">OSD type value. (0: Off, 1: Frame count, 2: Date/Time, 3: JPEG, 4: H.264) </param>
        /// <returns></returns>
        public IPCamError SetOSD(int type)
        {
            string resp;
            var ret = SendCommand("SetOSD " + type, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get the current OSD mode. (i-Nova2 only)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public IPCamError GetOSD(out int val)
        {
            val = -1;
            string resp;
            var ret = SendCommand("GetOSD", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set Gamma value. (i-Nova2 only)
        /// </summary>
        /// <param name="gamma">Gamma value to set. Set this to 1 to achieve linear response.</param>
        /// <returns></returns>
        public IPCamError SetGamma(float gamma)
        {
            string resp;
            var ret = SendCommand("SetGamma " + gamma, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get the current Gamma value. (i-Nova2 only)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public IPCamError GetGamma(out double val)
        {
            val = -1;
            string resp;
            var ret = SendCommand("GetGamma", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    // string[] values = resp.Split(new char[] {' ', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
                    //fval = Convert.ToDouble(values[1]); // skip "OK ".

                    val = Convert.ToDouble(resp.Substring(3)); // skip "OK ".

                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set sharpness value. (i-Nova2 only)
        /// </summary>
        /// <param name="val">
        /// parameter can be 0 ~ 10.
        /// </param>
        /// <returns></returns>
        public IPCamError SetSharpness(int val)
        {
            string resp;
            var ret = SendCommand("SetSharpness " + val, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get sharpness value. (i-Nova2 only)
        /// </summary>
        /// <param name="val">
        /// parameter can be 0 ~ 10.
        /// </param>
        /// <returns></returns>
        public IPCamError GetSharpness(out int val)
        {
            string resp; val = -1;
            var ret = SendCommand("GetSharpness", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set mirror status. (i-Nova2 only)
        /// </summary>
        /// <param name="status">Status value. 0: Off, 1: On.</param>
        /// <returns></returns>
        public IPCamError SetMirror(int status)
        {
            string resp;
            var ret = SendCommand("SetMirror " + status, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get mirror status. (i-Nova2 only)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public IPCamError GetMirror(out int val)
        {
            val = -1;
            string resp;
            var ret = SendCommand("GetMirror", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set flip status. (i-Nova2 only)
        /// </summary>
        /// <param name="status">Status value. 0: Off, 1: On.</param>
        /// <returns></returns>
        public IPCamError SetFlip(int status)
        {
            string resp;
            var ret = SendCommand("SetFlip " + status, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get flip status. (i-Nova2 only)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public IPCamError GetFlip(out int val)
        {
            val = -1; string resp;
            var ret = SendCommand("GetFlip", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Set the video format. (i-Nova2 only)
        /// </summary>
        /// <param name="format">The video format. 0: JPEG, 1: Uncompressed</param>
        /// <returns></returns>
        public IPCamError SetVideoFormat(int format)
        {
            string resp;
            var ret = SendCommand("SetVideoFormat " + format, out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get the current video format. (i-Nova2 only)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public IPCamError GetVideoFormat(out int val)
        {
            val = -1;
            string resp;
            var ret = SendCommand("GetVideoFormat", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    val = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        // TODO: For testing purpose only
        public IPCamError SetRGBGain(double r, double g, double b)
        {
            string resp;
            var ret = SendCommand(string.Format("SetRGBGain {0:F2} {1:F2} {2:F2}", r, g, b), out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// (ONSemi model only)
        /// Set the debayer compensation gain value which adjusts the digital gain values
        ///  on the sensor to compensate the imbalance between red, green and blue channels under the strong near-IR lighting condition.
        /// </summary>
        /// <remarks>
        /// Higher value (max. 3) applies higher gains on green and blue channels against red.
        /// </remarks>
        /// <param name="index">0: gain = 1.0, 1: gain = 2.0, *2: gain = 3.0, 3: gain = 4.0</param>
        /// <returns></returns>
        public IPCamError SetDebayerCompGain(int index)
        {
            string resp;
            var ret = SendCommand(string.Format("SetDebayerCompGain {0}", index), out resp);
            if (ret == IPCamError.OK)
                if (resp.StartsWith("OK"))
                    return IPCamError.OK;
                else
                    return IPCamError.InvalidResponse;
            else
                return ret;
        }

        /// <summary>
        /// Get the value index of debayer artifact compensation gain. (ONSemi only)
        /// </summary>
        /// <param name="index">0: gain = 1.0, 1: gain = 2.0, *2: gain = 3.0, 3: gain = 4.0</param>
        /// <returns></returns>
        public IPCamError GetDebayerCompGain(out int index)
        {
            index = 2;
            string resp;
            var ret = SendCommand("GetDebayerCompGain", out resp);
            if (ret == IPCamError.OK)
            {
                if (resp.StartsWith("OK"))
                {
                    index = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                    return IPCamError.OK;
                }
                else
                    return IPCamError.InvalidResponse;
            }
            else
                return ret;
        }

        /// <summary>
        /// Detect if the i-Nova2 camera is using older firmware.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool IsVersion05()
        {
            return m_version05;
        }

        /// <summary>
        /// Get the latest JPEG buffer being received.
        /// </summary>
        /// <returns></returns>
        public byte[] GetLatestBuffer()
        {
            byte[] buf = null;
            lock (m_recvBufLock)
            {
                buf = (byte[])m_recvBuf.Clone();
            }
            return buf;
        }

        //
        // Private methods
        //
        private object m_sendLock = new object();

        private IPCamError SendCommand(string str, out string response)
        {
            response = string.Empty;

            if (m_stream_CMD == null)
            {
                return IPCamError.StreamNotOpened;
            }

            str += "\r\n";

            var recvBuf = new byte[1024];
            int bytesRead = 0;
            lock (m_sendLock)
            {
                try
                {
                    // Send request
                    byte[] bytes = Encoding.ASCII.GetBytes(str);
                    m_stream_CMD.Write(bytes, 0, bytes.Length);
                    m_stream_CMD.Flush();

                    // Receive response
                    m_stream_CMD.ReadTimeout = 1000; // wait longer for the first packet.
                    bytesRead = m_stream_CMD.Read(recvBuf, 0, recvBuf.Length);
                    if (bytesRead == 0) // This happens when disconnected by camera.
                        return IPCamError.OperationFailure;
                }
                catch (IOException ioex)
                {
                    return IPCamError.SocketError;
                }
            }

            response = System.Text.Encoding.Default.GetString(recvBuf, 0, bytesRead);

            if (response.StartsWith("NG CommandNotFound")) return IPCamError.CommandNotFound;
            else if (response.StartsWith("NG BadFormat")) return IPCamError.BadFormat;
            else if (response.StartsWith("NG InvalidMode")) return IPCamError.InvalidMode;
            else if (response.StartsWith("NG InvalidValue")) return IPCamError.InvalidValue;
            else if (response.StartsWith("NG DefaultPassword")) return IPCamError.DefaultPassword;
            else if (response.StartsWith("NG")) return IPCamError.OperationFailure;
            else                                return IPCamError.OK;
        }

        private IPCamError ConnectStreamPortUDP(string ipAddress)
        {
            try
            {
                if (m_sock_SRM_UDP != null)
                {
                    m_sock_SRM_UDP.Close();
                    m_sock_SRM_UDP = null;
                }

                int recv_port = STREAM_PORT;
                while (recv_port < STREAM_PORT + 100)
                {
                    try
                    {
                        m_sock_SRM_UDP = new UdpClient(recv_port /* 0 */, AddressFamily.InterNetwork);
                        string message = string.Format("CONNECT {0}", recv_port);
                        byte[] data = Encoding.ASCII.GetBytes(message);
                        m_ep = new IPEndPoint(IPAddress.Parse(ipAddress), STREAM_PORT); // specify the source port of the returning stream packets.
                        //var send_ep = new IPEndPoint(IPAddress.Parse(ipAddress), STREAM_PORT);
                        m_sock_SRM_UDP.Send(data, data.Length, m_ep /*send_ep*/);
                        m_sock_SRM_UDP.Connect(m_ep);
                    }
                    catch (SocketException sockex)
                    {
                        if (sockex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        {
                            // try a different port number.
                            recv_port++;
                            continue;
                        }
                        else
                        {
                            // other errors?
                            System.Windows.Forms.MessageBox.Show(sockex.ToString());
                            return IPCamError.SocketError;
                        }
                    }
                    // no error - exit the loop
                    break;
                }

                //m_sock_SRM_UDP.Send(data, data.Length, m_ep);
                //m_ep.Address = IPAddress.Any;
                //m_sock_SRM_UDP.Client.ReceiveBufferSize = MAX_IMAGE_SIZE;
                // DEBUG
                m_sock_SRM_UDP.Client.ReceiveBufferSize = 6 * 1024 * 1024; // TODO: Does this improve errors???

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                return IPCamError.SocketError;
            }

            return IPCamError.OK;
        }

        private IPCamError ConnectStreamPortTCP(string ipAddress)
        {
            if (m_sock_SRM_TCP != null)
                DisconnectStreamPort();

            try
            {
                m_sock_SRM_TCP = new TcpClient(AddressFamily.InterNetwork);
                //m_sock_SRM = new TcpClient(ipAddress, STREAM_PORT);
                if (m_sock_SRM_TCP != null)
                {
                    var result = m_sock_SRM_TCP.BeginConnect(ipAddress, STREAM_PORT, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (!success) return IPCamError.Timeout;

                    m_sock_SRM_TCP.EndConnect(result);

                    m_sock_SRM_TCP.ReceiveBufferSize = 3 * 1024 * 1024;// RECV_BUF_SIZE;
                    m_sock_SRM_TCP.NoDelay = true;
                    m_stream_SRM = m_sock_SRM_TCP.GetStream();
                }
            }
            catch (SocketException)
            {
                return IPCamError.SocketError;
            }

            return m_stream_SRM != null ? IPCamError.OK : IPCamError.StreamNotOpened;
        }
        

        public bool IsInova2()
        {
            return m_model != Model.iN_20;
        }
        
        public bool IsInova2_Standard()
        {
            return m_model == Model.iN2_32SC || m_model == Model.iN2_23SC;
        }

        public bool IsInova2_Zoom()
        {
            return m_model == Model.iN2Z_32SC;
        }

        public bool IsInova2_Compact()
        {
            return m_model == Model.iN2_23SC_C;
        }
        
        public bool IsInova2_Motor()
        {
            return m_model == Model.iN2M_23SC || m_model == Model.iN2M_23OC;
        }

        public bool IsInova2_Motor_ONSemi()
        {
            return m_model == Model.iN2M_23OC;
        }

        public Model GetModel()
        {
            return m_model;
        }

        //
        // Private member variables
        //
        const int RECV_BUF_SIZE = 64 * 1024;
        const int MAX_IMAGE_SIZE = 512 * 1024;
        const int STREAM_PORT = 1334;
        const int COMMAND_PORT = 1335;
        const int STREAM_HEADER_LENGTH = 256;

        private TcpClient m_sock_SRM_TCP;
        private UdpClient m_sock_SRM_UDP;
        private IPEndPoint m_ep; // for UDP only
        private NetworkStream m_stream_SRM; // for TCP only
        private TcpClient m_sock_CMD;
        private NetworkStream m_stream_CMD;
        private Model m_model = Model.UNKNOWN; // GetSerialNumber must be called before use.
        protected bool m_isUDPStreaming;
        private bool m_version05 = false; // i-Nova2 Version being older or equal to 0.5
        private byte[] m_recvBuf = new byte[MAX_IMAGE_SIZE];
        private int m_lastImageBufferSize = 0;
        private object m_recvBufLock = new object();
    }
}
