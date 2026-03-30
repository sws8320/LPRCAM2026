using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Threading;
using System.IO;

namespace KyungsinLPR
{
    /// <summary>
    /// The IPCamera class with the callback functionality.
    /// </summary>
    public class IPCameraAsync : IPCamera
    {
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ImageGrabbedEventArgs> ImageGrabbed;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<ImageGrabbedEventArgs> ImageGrabFailed;

        private Thread m_grabThread;
        private bool m_keepGrab;

        private Thread m_decodeThread;
        private bool m_keepDecode;

        private AutoResetEvent m_startDecode = new AutoResetEvent(false);

        /// <summary>
        /// To receive callback, user needs to call this method.
        /// </summary>
        public void StartGrab()
        {
            if (m_keepGrab) return;

            m_grabThread = new Thread(GrabLoop);
            m_grabThread.IsBackground = true;
            m_keepGrab = true;
            m_grabThread.Start();

            m_decodeThread = new Thread(DecodeLoop);
            m_decodeThread.IsBackground = true;
            m_keepDecode = true;
            m_decodeThread.Start();
        }

        /// <summary>
        /// To stop receiving callback, user needs to call this method.
        /// </summary>
        public void StopGrab()
        {
            if (!m_keepGrab) return;

            m_keepGrab = false;
            m_grabThread.Join(1000);

            m_keepDecode = false;
            m_decodeThread.Join(1000);
        }

        byte[] m_jpegBuffer;

        private void DecodeLoop(object threadParam)
        {
            while (m_keepDecode)
            {
                if (m_startDecode.WaitOne(100))
                {
                    using (MemoryStream jpegStrm = new MemoryStream(m_jpegBuffer))
                    {
                        JpegBitmapDecoder decoder;
                        try
                        {
                            decoder = new JpegBitmapDecoder(jpegStrm,
                                                            BitmapCreateOptions.None,
                                                            BitmapCacheOption.OnLoad);
                            BitmapSource bitmap = decoder.Frames[0];
                            if (ImageGrabbed != null)
                                ImageGrabbed(this, new ImageGrabbedEventArgs(bitmap, IPCamError.OK));
                        }
                        catch (Exception)
                        {
                            if (ImageGrabbed != null)
                                ImageGrabbed(this, new ImageGrabbedEventArgs(null, IPCamError.DecodeFailure));
                        }

                    }
                }
            }
        }

        private void GrabLoop(object threadParam)
        {
            while (m_keepGrab)
            {
                byte[] tmpBuffer;
                IPCamError err;
                if (m_isUDPStreaming)
                {
                    MetaInfo metainfo;
                    err = GetRawDataUDP(1000, out tmpBuffer, out metainfo);
                }
                else
                {
                    err = SendPing();
                    err = GetRawDataTCP(1000, out tmpBuffer);
                }

                if (err == IPCamError.OK)
                {
                    // Check if the EOI marker exists at the end of the buffer
                    if (tmpBuffer[tmpBuffer.Length - 1] != 0xd9
                        || tmpBuffer[tmpBuffer.Length - 2] != 0xff)
                    {
                        if (ImageGrabFailed != null)
                            ImageGrabFailed(this, new ImageGrabbedEventArgs(null, IPCamError.BrokenBuffer_MissingEOI));
                    }

                    // Buffer seems fine. Let the decode thread do the job.
                    m_jpegBuffer = tmpBuffer;
                    m_startDecode.Set();
                }
                else // error or timeout.
                {
                    if (ImageGrabFailed != null)
                        ImageGrabFailed(this, new ImageGrabbedEventArgs(null, err));
                    Thread.Sleep(10);
                }
            }
        }
    }

    public class ImageGrabbedEventArgs : System.EventArgs
    {
        public readonly BitmapSource bitmap;
        public readonly IPCamError error;

        public ImageGrabbedEventArgs(BitmapSource _bitmap, IPCamError _error)
        {
            bitmap = _bitmap;
            error = _error;
        }
    }
}
