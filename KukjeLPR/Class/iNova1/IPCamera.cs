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

namespace KyungsinLPR
{
    /// <summary>
    /// An enum for IPCamera class's error code.
    /// </summary>
    public enum IPCamError
    {
        /// <summary>
        /// No error
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
        /// Stream port is not opened.
        /// </summary>
        StreamNotOpened,

        /// <summary>
        /// Error in socket operation
        /// </summary>
        SocketError
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

    public class MetaInfo
    {
        public int Type;
        public int Size;
        public int FrameCount;
        public int Exposure;
        public double Gain;
        public int TriggerCount;

        public static MetaInfo CreateFromByteArray(byte[] array)
        {
            if (array.Length < 20) return null;

            var metainfo = new MetaInfo();
            metainfo.Type = FromByteArray(array, 0);
            metainfo.Size = FromByteArray(array, 4);
            metainfo.FrameCount = FromByteArray(array, 8);
            metainfo.Exposure = FromByteArray(array, 12);
            metainfo.Gain = (double)FromByteArray(array, 16) / 100;
            metainfo.TriggerCount = FromByteArray(array, 20);
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
        public bool ConnectStreamPort(string ipAddress, bool useUDP = false)
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
        public bool DisconnectStreamPort()
        {
            if (m_isUDPStreaming)
            {
                if (m_sock_SRM_UDP == null) return false;

                var data = Encoding.ASCII.GetBytes("DISCONNECT");
                m_sock_SRM_UDP.Send(data, data.Length);
                m_sock_SRM_UDP.Close();
                m_sock_SRM_UDP = null;
            }
            else
            {
                if (m_stream_SRM == null) return false;

                m_stream_SRM.Close();
                m_stream_SRM = null;
                m_sock_SRM_TCP.Close();
                m_sock_SRM_TCP = null;
            }
            return true;
        }

        /// <summary>
        /// Connect to the command port of the camera.
        /// This must be called before issuing any other commands except GetImage and GetRawData commands.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public bool ConnectCommandPort(string ipAddress)
        {
            if (m_sock_CMD != null)
                DisconnectCommandPort();

            try
            {
                m_sock_CMD = new TcpClient(AddressFamily.InterNetwork);
                //m_sock_CMD = new TcpClient(ipAddress, COMMAND_PORT);
                if (m_sock_CMD != null)
                {
                    var result = m_sock_CMD.BeginConnect(ipAddress, COMMAND_PORT, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (!success) return false;

                    m_sock_CMD.EndConnect(result);

                    m_sock_CMD.ReceiveBufferSize = RECV_BUF_SIZE;
                    m_sock_CMD.NoDelay = true;
                    m_stream_CMD = m_sock_CMD.GetStream();

                }
            }
            catch (ObjectDisposedException)
            { }
            catch (SocketException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            return m_sock_CMD != null;
        }


        
        /// <summary>
        /// Close the command port
        /// </summary>
        /// <returns></returns>
        public bool DisconnectCommandPort()
        {
            if (m_stream_CMD == null) return false;

            m_stream_CMD.Close();
            m_stream_CMD = null;
            if (m_sock_CMD != null)
                m_sock_CMD.Close();
            m_sock_CMD = null;
            return true;
        }

        public bool IsCommandPortConnected()
        {
            if (m_sock_CMD == null || m_sock_CMD.Client == null) return false;
            return m_sock_CMD.Connected;
        }


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

        /// <summary>
        /// Get the raw JPEG buffer received from the camera using UDP stream.
        /// Call GetImage if you want a decoded image instead of this method. Stream port must be opened before calling this.
        /// </summary>
        /// <param name="timeout">Timeout value to wait until an image is received</param>
        /// <param name="jpegBuffer">(out) JPEG buffer received</param>
        /// <param name="metaInfo">Acquired image meta info. For now, this is only available in UDP streaming.</param>
        /// <returns></returns>
        public IPCamError GetRawDataUDP(int timeout, out byte[] jpegBuffer, out MetaInfo metaInfo)
        {
            metaInfo = null;
            jpegBuffer = null;

            try
            {
                m_sock_SRM_UDP.Client.ReceiveTimeout = timeout;
                byte[] recv_data = null;

                // Search for the header packet.
                do
                {
                    recv_data = m_sock_SRM_UDP.Receive(ref m_ep);
                } while (recv_data.Length != UDP_STREAM_HEADER_LENGTH);

                metaInfo = MetaInfo.CreateFromByteArray(recv_data);
                int imageSize = metaInfo.Size;
                if (imageSize > MAX_IMAGE_SIZE)
                    return IPCamError.BrokenBuffer_IllegalSize;

                //Console.WriteLine(string.Format("{0}, {1}, {2}, {3:F3}", metaInfo.Size, metaInfo.Count, metaInfo.Exposure, metaInfo.Gain));

                // Receive the image buffer.
                jpegBuffer = new byte[imageSize];
                int receivedLen = 0;
                while (receivedLen < imageSize)
                {
                    recv_data = m_sock_SRM_UDP.Receive(ref m_ep);
                    if (recv_data.Length > imageSize - receivedLen)
                    {
                        return IPCamError.BrokenBuffer_IllegalSize;
                    }
                    Array.Copy(recv_data, 0, jpegBuffer, receivedLen, recv_data.Length);
                    receivedLen += recv_data.Length;
                }
                m_lastImageBufferSize = imageSize;
                m_recvBuf = jpegBuffer;
            }
            catch (Exception) 
            {
                // TODO: need to check other errors.
                return IPCamError.Timeout;
            }
            return IPCamError.OK;
        }

        /// <summary>
        /// Get the raw JPEG buffer received from the camera using TCP stream.
        /// Call GetImage if you want a decoded image instead of this method. Stream port must be opened before calling this.
        /// </summary>
        /// <param name="timeout">Timeout value to wait until an image is received</param>
        /// <param name="jpegBuffer">(out) JPEG buffer received</param>
        /// <returns></returns>
        public IPCamError GetRawDataTCP(int timeout, out byte[] jpegBuffer)
        {
            jpegBuffer = null;
            if (m_stream_SRM == null) return IPCamError.StreamNotOpened;

            m_stream_SRM.ReadTimeout = timeout;
            int bytesRead = 0;

            byte[] headerBytes = new byte[4];
            try
            {
                bytesRead = m_stream_SRM.Read(headerBytes, 0, headerBytes.Length);
            }
            catch (IOException)
            {
                //Console.WriteLine("socket exception " + ex.ToString());
                return IPCamError.Timeout;
            }
            if (bytesRead == 0) return IPCamError.Timeout;

            int bufLen = (headerBytes[0] << 24)
                        + (headerBytes[1] << 16)
                        + (headerBytes[2] << 8)
                        + headerBytes[3];
            if (bufLen <= 0 || bufLen > MAX_IMAGE_SIZE)
            {
                // dummy read to erase all broken data from the stream buffer.
                m_stream_SRM.ReadTimeout = 20;
                lock (m_imageBufLock)
                {
                    m_recvBuf = new byte[MAX_IMAGE_SIZE];
                    bytesRead = m_stream_SRM.Read(m_recvBuf, 0, m_recvBuf.Length);
                }
                return IPCamError.BrokenBuffer_IllegalSize;
            }
            //Console.WriteLine("header: " + bufLen);

            int totalBytesRead = 0;
            lock (m_imageBufLock)
            {
                m_recvBuf = new byte[bufLen];
                do
                {                   
                    //Thread.Sleep(3); // This seems to fix the issue with .NET 3.5 and trigger mode.
                    try
                    {
                        bytesRead = m_stream_SRM.Read(m_recvBuf,
                                                    totalBytesRead,
                                                    bufLen - totalBytesRead);
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("socket exception " + ex.ToString());
                        return IPCamError.SocketError;
                    }
                    totalBytesRead += bytesRead;
                } while (totalBytesRead < bufLen);
            }

            m_lastImageBufferSize = bufLen;
            jpegBuffer = m_recvBuf;
            return IPCamError.OK;
        }

        /// <summary>
        /// Get an image from camera. The image is decoded to a bitmap so it is ready for image processing.
        /// Need to call ConnectStreamPort() before calling this method.
        /// </summary>
        /// <param name="timeout">Timeout value in milliseconds.</param>
        /// <param name="bitmap">(out) Bitmap of the image</param>
        /// <returns>Error code</returns>
        public IPCamError GetImage(int timeout, out Bitmap bitmap, out MetaInfo metaInfo)
        {
            metaInfo = null;
            bitmap = null;
            BitmapSource source;
            IPCamError err = GetImage(timeout, out source, out metaInfo);
            if (err != IPCamError.OK) return err;

            bitmap = IPCameraUtils.BitmapFromSource(source);
            return IPCamError.OK;
        }

        // for compatibility with old SDK.
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
        /// <returns></returns>
        public IPCamError GetImage(int timeout, out BitmapSource bitmapSource, out MetaInfo metaInfo)
        {
            metaInfo = null;
            bitmapSource = null;
            byte[] jpeg = null;

            if (m_isUDPStreaming)
            {
                IPCamError err = GetRawDataUDP(timeout, out jpeg, out metaInfo);
                if (err != IPCamError.OK) return err;
            }
            else
            {
                IPCamError err = SendPing();
                if (err != IPCamError.OK)
                    return err;

                err = GetRawDataTCP(timeout, out jpeg);
                if (err != IPCamError.OK) return err;
            }

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

                bitmapSource = decoder.Frames[0];

                return IPCamError.OK;
            }
        }

        // for compatibility with old SDK.
        public IPCamError GetImage(int timeout, out BitmapSource bitmapSource)
        {
            MetaInfo metaInfo;
            return GetImage(timeout, out bitmapSource, out metaInfo);
        }

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
        /// <param name="mode">0: Free run, 1: One-shot trigger, 2: Multi-shot trigger</param>
        /// <param name="isActiveHi">Polarity of the trigger. True for rising edge, false for falling edge</param>
        /// <returns></returns>
        /// 
        public bool SetTriggerMode(int mode, bool isActiveHi)
        {
            string polarity = isActiveHi ? "H" : "L";

            // Old API - for compatibility with old firmware
            if (mode == 0)
                return SendCommand("DisableTrigger") != null;
            else
                return SendCommand(String.Format("EnableTrigger {0} {1}", mode, polarity)) != null;

            // New API
            //return SendCommand(String.Format("SetTriggerMode {0} {1}", mode, polarity)) != null;
        }

        /// <summary>
        /// Get the current trigger mode and the polarity of the trigger.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="isActiveHi"></param>
        /// <returns></returns>
        public bool GetTriggerMode(out int mode, out bool isActiveHi)
        {
            var resp = SendCommand("GetTriggerMode");
            if (resp != null)
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
                        return false;
                    }
                    try
                    {
                        if (mode != 0)
                            isActiveHi = words[2] == "H";
                        else
                            isActiveHi = false;
                    }
                    catch
                    {
                        isActiveHi = false;
                    }
                    return true;
                }
            }
            mode = 0;
            isActiveHi = false;
            return false;
        }


        /// <summary>
        /// Set Trigger Source : H/W or S/W
        /// </summary>
        /// <param name="isSWTrigger"> true: SW Trigger, false: HW Trigger</param>
        /// <returns></returns>
        public bool SetTriggerSource(bool isSWTrigger)
        {
            if (isSWTrigger)
                return SendCommand("SetTriggerSource 1") != null;
            else
                return SendCommand("SetTriggerSource 0") != null;
        }

        /// <summary>
        /// Get Trigger Source.
        /// </summary>
        /// <param name="isSWTrigger"> true: SW Trigger, false: HW Trigger</param>
        /// <returns></returns>
        public bool GetTriggerSource(out bool isSWTrigger)
        {
            var resp = SendCommand("GetTriggerSource");
            if (resp != null)
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
                        return false;
                    }
                    return true;
                }
            }
            isSWTrigger = false;
            return false;
        }

        /// <summary>
        /// Set the output of GPIO pin.
        /// </summary>
        /// <param name="output">true: high level, false: low level</param>
        /// <returns></returns>
        public bool SetGPIO(bool output)
        {
            if (output)
                return SendCommand("SetGPIO 11 H") != null;
            else
                return SendCommand("SetGPIO 11 L") != null;
        }

        /// <summary>
        /// Set the total gain of the camera.
        /// </summary>
        /// <param name="value">The multiplier value of the gain.</param>
        /// <returns></returns>
        public bool SetTotalGain(double value)
        {
            if (value < 1) return false;
            //if (value > 84.7) return false;

            return SendCommand("SetTotalGain " + value) != null;
        }
        
        /// <summary>
        /// Set the analog gain of the camera.
        /// </summary>
        /// <param name="value">The analog gain value. It has to be between 0 and 7.</param>
        /// <returns></returns>
        public bool SetAnalogGain(int value)
        {
            if (value < 0) return false;
            if (value > 7) return false;

            return SendCommand("SetAnalogGain " + value) != null;
        }

        /// <summary>
        /// Get the Analog gain of the camera.
        /// </summary>
        /// <param name="analog_gain">The Analog gain value. It has to be between 0 and 6.</param>
        /// <returns></returns>
        public bool GetAnalogGain( out int analog_gain)
        {
            var resp = SendCommand("GetAnalogGain");
            if (resp != null)
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
                        return false;
                    }
                    if (AGain > 6) analog_gain = 6;
                    else if (AGain < 0) analog_gain = 0;
                    else analog_gain = AGain;
                    return true;
                }
                else
                {
                    analog_gain = 0;
                    return false; // error
                }
            }
            else
            {
                analog_gain = 0;
                return false; // Error
            }
        }

        /// <summary>
        /// Get the Digital gain of the camera.
        /// </summary>
        /// <param name="digital_gain">The Digital gain value.</param>
        /// <returns></returns>
        public bool GetDigitalGain(out double digital_gain)
        {
            var resp = SendCommand("GetDigitalGain");
            if (resp != null)
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
                        return false;
                    }
                    return true;
                }
                else
                {
                    digital_gain = 1;
                    return false; // error
                }
            }
            else
            {
                digital_gain = 1;
                return false; // Error
            }
        }

        /// <summary>
        /// Get the Total gain of the camera.
        /// </summary>
        /// <param name="total_gain">The Digital gain value.</param>
        /// <returns></returns>
        public bool GetTotalGain(out double total_gain)
        {
            var resp = SendCommand("GetTotalGain");
            if (resp != null)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    try
                    {
                        total_gain = Convert.ToDouble(words[1]);
                        if (total_gain == 0.0) return false; // something is wrong.
                    }
                    catch (FormatException)
                    {
                        total_gain = 0.0;
                        return false;
                    }
                    return true;
                }
                else
                {
                    total_gain = 1;
                    return false; // error
                }
            }
            else
            {
                total_gain = 1;
                return false; // Error
            }
        }

        /// <summary>
        /// Set the digital gain of the camera.
        /// </summary>
        /// <param name="value">The digital gain value.</param>
        /// <returns></returns>
        public bool SetDigitalGain(double value)
        {
            return SendCommand("SetDigitalGain " + value) != null;
        }

        /// <summary>
        /// Set the black level.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool SetBlackLevel(int value)
        {
            if (value < -128) return false;
            if (value > 127) return false;

            return SendCommand("SetBlackLevel " + value) != null;
        }

        /// <summary>
        /// Set the trigger image count. This determines the number of frames to be sent when a trigger is fired.
        /// The default value is 1.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool SetTriggerImageCount(int num)
        {
            var resp = SendCommand("SetTrigImgNum " + num);
            if (resp == null) return false;
            if (resp.StartsWith("NG"))
                return false;
            else
                return true;
        }

        /// <summary>
        /// Get the current trigger image count.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool GetTriggerImageCount(out int num)
        {
            var resp = SendCommand("GetTrigImgNum");
            if (resp != null)
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
                        num = 0;
                        return false;
                    }
                    return true;
                }
            }
            num = 0;
            return false;
        }

        /// <summary>
        /// Fire a software trigger on camera. This should be used in trigger modes (one-shot, multi-shot).
        /// </summary>
        /// <returns></returns>
        public bool SetForcedTrigger()
        {
            return SendCommand("SetForcedTrigger ON") != null;
        }

        /// <summary>
        /// Set the bracket mode.
        /// </summary>
        /// <param name="isBrk">Enables/disables bracket mode.</param>
        /// <param name="bracketCount">The number of channels (0-4)</param>
        /// <returns></returns>
        public bool SetBracketMode(bool isBrk, int bracketCount)
        {
            if (isBrk == true)
                SendCommand("SetBracketMode ON " + bracketCount);
            else
                SendCommand("SetBracketMode OFF");
            
            return true;

        }

        /// <summary>
        /// Get the bracket mode.
        /// </summary>
        /// <param name="isbrkMode">(out)Tells if the bracket mode is enabled.</param>
        /// <param name="bracketCount">(out)The number of bracket channels.</param>
        /// <returns></returns>
        public bool GetBracketMode(out bool isbrkMode, out int bracketCount)
        {
            var resp = SendCommand("GetBracketMode");
            if (resp != null)
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
                        isbrkMode = false;
                        bracketCount = 0;
                        return false;
                    }
                    return true;
                }
            }
            isbrkMode = false;
            bracketCount = 1;
            return false;

        }

        /// <summary>
        /// Set camera's bracket settings.
        /// </summary>
        /// <param name="ch">Channel number (0-3)</param>
        /// <param name="exposure">Exposure value in micro seconds</param>
        /// <param name="Again">Analog gain value.</param>
        /// <param name="DGain">Digital gain value.</param>
        /// <returns></returns>
        public bool SetBracketInfo(int ch, int exposure, int Again, double DGain)
        {
            return SendCommand("SetBracketInfo " + ch + " " + exposure + " " + Again + " " + DGain) != null;
        }

        /// <summary>
        /// Get camera's bracket settings.
        /// </summary>
        /// <param name="ch">Channel number (0-3)</param>
        /// <param name="exposure">(out)Exposure value in micro seconds</param>
        /// <param name="Again">(out)Analog gain value</param>
        /// <param name="Dgain">(out)Digital gain value</param>
        /// <returns></returns>
        public bool GetBracketInfo(int ch, out int exposure, out int Again, out double Dgain)
        {
            Dgain = exposure = Again = 0;
            var resp = SendCommand("GetBracketInfo " + ch);
            if (resp != null)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 3)
                {
                    try
                    {
                        exposure = Convert.ToInt32(words[2]);
                        Again = Convert.ToInt32(words[3]);
                        //leess 20221024 여기서 인덱스 오버플로 에러 발생
                        if(words.Length > 4) {
                            Dgain = Convert.ToDouble(words[4]);
                        }
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the JPEG quality value manually. This is useful only when JPEG CBR (constant bit rate) is disabled.
        /// </summary>
        /// <param name="value">Quality value of JPEG between 1 and 63. Lower value gives better quality with larger image data.</param>
        /// <returns></returns>
        public bool SetJPEGQuality(int value)
        {
            if (value < 1 || value > 63) return false;

            return SendCommand("SetJPEGQuality " + value) != null;
        }

        /// <summary>
        /// Get the current JPEG quality value.
        /// </summary>
        /// <param name="quality">Output JPEG quality value</param>
        /// <returns></returns>
        public bool GetJPEGQuality(out int quality)
        {
            var resp = SendCommand("GetJPEGQuality");
            if (resp != null)
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
                        quality = 0;
                        return false;
                    }
                    return true;
                }
            }
            quality = 0;
            return false;
        }

        /// <summary>
        /// Set the camera to JPEG CBR (constant bit rate) mode.
        /// With this mode enabled, the camera decides the optimal JPEG quality value based on the
        /// specified bitrate setting.
        /// </summary>
        /// <param name="enable">Enable or disable</param>
        /// <param name="bitrate">Bitrate value to be applied. Typically this has to be less than 24.</param>
        /// <returns></returns>
        public bool SetJPEGCBR(bool enable, double bitrate)
        {
            if (enable)
                return SendCommand("SetJPEGCBR ON " + bitrate) != null;
            else
                return SendCommand("SetJPEGCBR OFF") != null;
        }

        /// <summary>
        /// Get the current JPEG CBR status and the bit rate.
        /// </summary>
        /// <param name="enable">True if JPEG CBR is enabled.</param>
        /// <param name="bitrate">When JPEG CBR is enabled, this represents the current bit rate.</param>
        /// <returns></returns>
        public bool GetJPEGCBR(out bool enable, out double bitrate)
        {
            var resp = SendCommand("GetJPEGCBR");
            if (resp != null)
            {
                var words = resp.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    enable = words[1] == "Enabled";
                    try
                    {
                        bitrate = enable ? Convert.ToDouble(words[2]) : 0;
                    }
                    catch (FormatException)
                    {
                        bitrate = 0;
                        return false;
                    }
                    return true;
                }
            }
            enable = false;
            bitrate = 0;
            return false;
        }

        /// <summary>
        /// Set H.264 quality value.
        /// Please note that H.264 streaming has to be received via RTSP.
        /// </summary>
        /// <param name="value">Quality value of H.264 between 1 and 51. Lower value gives better quality with larger image data.</param>
        /// <returns></returns>
        public bool SetH264Quality(int value)
        {
            if (value < 1 || value > 51) return false;

            var resp = SendCommand("SetH264Quality " + value);
            return resp != null;
        }

        /// <summary>
        /// Get the H.264 quality value.
        /// </summary>
        /// <param name="quality"></param>
        /// <returns></returns>
        public bool GetH264Quality(out int quality)
        {
            var resp = SendCommand("GetH264Quality");
            if (resp != null)
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
                        quality = 0;
                        return false;
                    }
                    return true;
                }
            }
            quality = 0;
            return false;
        }

        /// <summary>
        /// Set the white balance of the camera.
        /// </summary>
        /// <param name="blueGain">The gain of blue channel relative to green, in decibel. </param>
        /// <param name="redGain">The gain of red channel relative to green, in decibel.</param>
        /// <returns></returns>
        public bool SetWhiteBalance(double blueGain, double redGain)
        {
            if (blueGain < -20 || blueGain > 20) return false;
            if (redGain < -20 || redGain > 20) return false;

            double multiplier_R = Math.Pow(10, redGain/20);
            double multiplier_B = Math.Pow(10, blueGain/20);

            string cmd = string.Format("SetBlueRedGain {0:F4} {1:F4}", multiplier_B, multiplier_R);
            return SendCommand(cmd) != null;
        }

        public bool GetWhiteBalance(out double blueGain, out double redGain)
        {
            blueGain = redGain = 0;
            var resp = SendCommand("GetBlueRedGain");
            if (resp != null)
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
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the exposure (shutter speed) of the camera.
        /// </summary>
        /// <param name="microseconds">The exposure value in micro seconds.</param>
        /// <returns></returns>
        public bool SetExposure(int microseconds)
        {
            if (microseconds < 0 || microseconds > 10 * 1000 * 1000) return false;

            return SendCommand("SetExposure " + microseconds) != null;
        }

        /// <summary>
        /// Get the current exposure value in micro seconds.
        /// </summary>
        /// <param name="microseconds"></param>
        /// <returns></returns>
        public bool GetExposure(out int microseconds)
        {
            microseconds = 0;
            string resp = SendCommand("GetExposure");
            if (resp == null || resp.StartsWith("NG"))
            {
                return false;
            }

            try
            {
                microseconds = Convert.ToInt32(resp.Substring(3));
            }
            catch (FormatException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Set the frame rate of the camera.
        /// </summary>
        /// <param name="frameRate">The number of frames per second.</param>
        /// <returns></returns>
        public bool SetFrameRate(double frameRate)
        {
            if (frameRate > 30 || frameRate < 0.1) return false;

            return SendCommand("SetFrameRate " + frameRate) != null;
        }

        /// <summary>
        /// Get the current frame rate value.
        /// </summary>
        /// <param name="fps"></param>
        /// <returns></returns>
        public bool GetFrameRate(out double fps)
        {
            string resp = SendCommand("GetFrameRate");
            if (resp == null || resp.StartsWith("NG"))
            {
                fps = 0;
                return false;
            }

            try
            {
                fps = Convert.ToDouble(resp.Substring(3));
            }
            catch (FormatException)
            {
                fps = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Set Flash mode (Enable/Disable)
        /// </summary>
        /// <param name="mode"> Flash out mode. 0: Disable flash, 1: Exposure time, 2: Exposure + readout time.</param>
        /// <param name="isActiveHi">Polarity of flash output.</param>
        /// <returns></returns>
        public bool SetFlash(int mode, bool isActiveHi)
        {
            return SendCommand(string.Format("EnableFlash {0} {1}", mode, isActiveHi ? "H" : "L")) != null;
        }

        /// <summary>
        /// Get the current flash mode and the polarity of the flash.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="isActiveHi"></param>
        /// <returns></returns>
        public bool GetFlash(out int mode, out bool isActiveHi)
        {
            var resp = SendCommand("GetFlash");
            if (resp != null)
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
                        return false;
                    }
                    try
                    {
                        isActiveHi = words[2] == "H";
                    }
                    catch (Exception)
                    {
                        isActiveHi = false;
                        return false;
                    }
                    return true;
                }
            }
            mode = 0;
            isActiveHi = false;
            return false;
        }

        /// <summary>
        /// Set the delay amount when the flash is turned on.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool SetFlashOnDelay(int num)
        {
            return SendCommand("SetFlashOnDelay " + num) != null;
        }

        /// <summary>
        /// Set the delay amount when the flash is turned off.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool SetFlashOffDelay(int num)
        {
            return SendCommand("SetFlashOffDelay " + num) != null;
        }

        /// <summary>
        /// Set Auto Luminance Control parameters.
        /// </summary>
        /// <param name="alc">The ALC information to be applied. See ALC class for detail.</param>
        /// <returns></returns>
        public bool SetALC(ALC alc)
        {
            string command = string.Format("SetALC {0} {1} {2} {3} {4} {5} {6} {7}",
                alc.enableAEC ? "ON" : "OFF",
                alc.enableAGC ? "ON" : "OFF",
                alc.target,
                alc.minExposure,
                alc.maxExposure,
                alc.minGain,
                alc.maxGain,
                alc.p_factor);

            return SendCommand(command) != null;
        }

        /// <summary>
        /// Get Auto Luminance Control parameters.
        /// </summary>
        /// <param name="alc">The current ALC information in the camera. See ALC class for detail.</param>
        /// <returns></returns>
        public bool GetALC(out ALC alc)
        {
            alc = new ALC();
            
            var result = SendCommand("GetALC");
            if (result == null || !result.StartsWith("OK")) return false;
            string[] values = result.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                if (values.Length < 7) return false;
                alc.enableAEC = values[1] == "ON";
                alc.enableAGC = values[2] == "ON";
                alc.target = Convert.ToInt32(values[3]);
                alc.minExposure = Convert.ToInt32(values[4]);
                alc.maxExposure = Convert.ToInt32(values[5]);
                alc.minGain = Convert.ToDouble(values[6]);
                alc.maxGain = Convert.ToDouble(values[7]);
            }
            catch (FormatException) { return false; }
            return true;
        }

        /// <summary>
        /// Write a value to the register in camera's image sensor. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteSensorRegister(int addr, int value)
        {
            return SendCommand("WriteSensorRegister " + addr + " " + value) != null;
        }

        /// <summary>
        /// Write a value to the register in camera's ISP. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WriteISPRegister(int addr, int value)
        {
            return SendCommand("WriteISPRegister " + addr + " " + value) != null;
        }

        /// <summary>
        /// Read the register value in camera's image sensor. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ReadSensorRegister(int addr, out int value)
        {
            value = 0;
            string resp = SendCommand("ReadSensorRegister " + addr);
            
            if (resp == null) return false;
            try
            {
                value = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Read the register value in camera's ISP. This is only for internal use.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ReadISPRegister(int addr, out int value)
        {
            value = 0;
            string resp = SendCommand("ReadISPRegister " + addr);

            if (resp == null) return false;
            try
            {
                value = Convert.ToInt32(resp.Substring(3)); // skip "OK ".
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Save the last image which is already received.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool SaveLastImage(string path)
        {
            try
            {
                lock (m_imageBufLock)
                {
                    File.WriteAllBytes(path, m_recvBuf);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get the firmware version
        /// </summary>
        /// <returns></returns>
        public string GetFirmwareVersion()
        {
            var resp = SendCommand("GetFirmwareVersion");
            if (resp == null) return null;

            string version = resp.Substring(3); // skip "OK ".
            return version.Trim();
        }

        /// <summary>
        /// Get the serial number of the camera.
        /// </summary>
        /// <returns></returns>
        public string GetSerialNumber()
        {
            var resp = SendCommand("GetSerialNumber");
            if (resp == null || !resp.StartsWith("OK")) return string.Empty;

            string serial = resp.Substring(3); // skip "OK ".
            return serial.Trim();
        }

        /// <summary>
        /// Save the current camera settings to the camera so that the settings are retained after
        /// power-cycling the camera.
        /// Please note that only a part of the settings are saved.
        /// </summary>
        /// <returns></returns>
        public bool SaveSetting()
        {
            var resp = SendCommand("SaveSetting");
            if (resp != null && resp.StartsWith("OK"))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Reset the camera. After calling this method, reconnection is needed to communicate with the camera again.
        /// </summary>
        /// <returns></returns>
        public bool ResetCamera()
        {
            var resp = SendCommand("ResetCamera");
            // Camera would restart now so there would be no response.
            return true;
        }

        public bool RestoreDefaultSetting()
        {
            var resp = SendCommand("RestoreDefaultSetting");
            if (resp == null || resp.StartsWith("OK")) // Old firmware doesn't support this command so it will return an error code.
                return true;
            else
                return false;
            
            // Camera would restart now so there would be no response.
            //return true;
        }

        public byte[] GetLatestBuffer()
        {
            byte[] buf = null;
            lock (m_imageBufLock)
            {
                buf = (byte[])m_recvBuf.Clone();
            }
            return buf;
        }

        //
        // Private methods
        //
        private object m_sendLock = new object();

        private string SendCommand(string str)
        {
            if (m_stream_CMD == null)
            {
                return null;
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
                    //m_stream_CMD.ReadTimeout = 300; // wait longer for the first packet.
                    m_stream_CMD.ReadTimeout = 1000; // wait longer for the first packet.
                    bytesRead = m_stream_CMD.Read(recvBuf, 0, recvBuf.Length);
                }
                catch (NullReferenceException)
                { return null; }
                catch (ObjectDisposedException)
                { return null; }
                catch (IOException)
                {
                    return null;
                }
            }

            var response = System.Text.Encoding.Default.GetString(recvBuf, 0, bytesRead);
            return response;
        }

        private bool ConnectStreamPortUDP(string ipAddress)
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
                    try{
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
                            return false;
                        }
                    }
                    // no error - exit the loop
                    break;
                }

                //m_sock_SRM_UDP.Send(data, data.Length, m_ep);
                //m_ep.Address = IPAddress.Any;
                m_sock_SRM_UDP.Client.ReceiveBufferSize = MAX_IMAGE_SIZE;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                return false;
            }

            return true;
        }

        private bool ConnectStreamPortTCP(string ipAddress)
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
                    if (!success) return false;

                    m_sock_SRM_TCP.EndConnect(result);

                    m_sock_SRM_TCP.ReceiveBufferSize = RECV_BUF_SIZE;
                    m_sock_SRM_TCP.NoDelay = true;
                    m_stream_SRM = m_sock_SRM_TCP.GetStream();
                }
            }
            catch (SocketException)
            {
                return false;
            }

            return m_stream_SRM != null;
        }

        //
        // Private member variables
        //
        const int RECV_BUF_SIZE = 64 * 1024;
        const int MAX_IMAGE_SIZE = 512 * 1024;
        const int STREAM_PORT = 1334;
        const int COMMAND_PORT = 1335;
        const int UDP_STREAM_HEADER_LENGTH = 256;

        private TcpClient m_sock_SRM_TCP;
        private UdpClient m_sock_SRM_UDP;
        private IPEndPoint m_ep; // for UDP only
        private NetworkStream m_stream_SRM; // for TCP only
        private TcpClient m_sock_CMD;
        private NetworkStream m_stream_CMD;
        protected bool m_isUDPStreaming;
        private byte[] m_recvBuf = new byte[MAX_IMAGE_SIZE];
        private int m_lastImageBufferSize = 0;
        private object m_imageBufLock = new object();
    }

}
