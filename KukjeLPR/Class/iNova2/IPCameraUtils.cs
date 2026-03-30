using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Net.NetworkInformation;

namespace KyungsinLPR.iNova2 {
    class SSDPUtil
    {
        private System.Threading.Timer timer;
        private Dictionary<string, DateTime> ipList = new Dictionary<string, DateTime>();
        private object m_ipListLock = new object();

        public SSDPUtil()
        {
            PollInterface();
        }

        public void OnTimer(object state)
        {
            var now = DateTime.Now;
            lock (m_ipListLock)
            {
                bool removed = false;
                do
                {
                    removed = false;
                    foreach (KeyValuePair<string, DateTime> pair in ipList)
                    {
                        // if the ip in the list is too old, remove it.
                        if ((now - pair.Value).TotalSeconds > 3)
                        {
                            ipList.Remove(pair.Key);
                            IpUpdated(this, new IpUpdatedEventArgs(pair.Key, null, false, true));
                            removed = true;
                            break;
                        }
                    }
                } while (removed);
            }
        }

        private static ArrayList addressTable = new ArrayList();

        /// <summary>
        /// Retreve all IP addresses present on the local host.
        /// </summary>
        /// <returns>List of IP addresses present on the local host.</returns>
        public static IPAddress[] GetLocalAddresses()
        {
            return ((IPAddress[])addressTable.ToArray(typeof(IPAddress)));
        }

        /// <summary>
        /// Enumerates network interfaces.
        /// </summary>
        private void PollInterface()
        {
            try
            {
                ArrayList CurrentAddressTable = new ArrayList();
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface i in interfaces)
                {
                    if (i.IsReceiveOnly == false && i.OperationalStatus == OperationalStatus.Up && i.SupportsMulticast == true)
                    {
                        IPInterfaceProperties i2 = i.GetIPProperties();
                        foreach (UnicastIPAddressInformation i3 in i2.UnicastAddresses)
                        {
                            if (!CurrentAddressTable.Contains(i3.Address) && !i3.Address.Equals(IPAddress.IPv6Loopback)) { CurrentAddressTable.Add(i3.Address); }
                        }
                    }
                }

                ArrayList OldAddressTable = addressTable;
                addressTable = CurrentAddressTable;
                if (!addressTable.Contains(IPAddress.Loopback))
                {
                    addressTable.Add(IPAddress.Loopback);
                }

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString(),"NetworkInfo");
            }
        }

        public static IPAddress UpnpMulticastV4Addr = IPAddress.Parse("239.255.255.250");
        public event EventHandler<IpUpdatedEventArgs> IpUpdated;

        public void SetupSSDPSessions()
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            byte[] inValue = new byte[] { 0, 0, 0, 0 };     // == false
            byte[] outValue = new byte[] { 0, 0, 0, 0 };    // initialize to 0

            IPAddress[] ips = GetLocalAddresses();
            
            if ( timer == null)
                timer = new System.Threading.Timer(OnTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));

            foreach (IPAddress addr in ips)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork || addr.ScopeId != 0)
                {
                    try
                    {
                        if (addr.AddressFamily == AddressFamily.InterNetwork)
                        {
                            UdpClient session = new UdpClient(AddressFamily.InterNetwork);
                            try { session.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); }
                            catch (SocketException )
                            {
                                Console.WriteLine("SetupSSDPSessions: SocketException");
                            }
                            try { session.ExclusiveAddressUse = false; }
                            catch (SocketException )
                            {
                                Console.WriteLine("SetupSSDPSessions: SocketException");
                            }
                            session.Client.Bind(new IPEndPoint(addr, 1900));
                            session.EnableBroadcast = true;
                            session.JoinMulticastGroup(UpnpMulticastV4Addr, addr);
                            try { session.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue); }
                            catch (SocketException )
                            {
                                Console.WriteLine("SetupSSDPSessions: SocketException");
                            }
                            session.BeginReceive(new AsyncCallback(OnReceiveSink), new object[2] { session, new IPEndPoint(addr, ((IPEndPoint)session.Client.LocalEndPoint).Port) });

                            UdpClient usession = new UdpClient(AddressFamily.InterNetwork);
                            usession.Client.Bind(new IPEndPoint(addr, 0));
                            try { usession.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue); }
                            catch (SocketException)
                            {
                                Console.WriteLine("SetupSSDPSessions: SocketException");
                            }
                            usession.BeginReceive(new AsyncCallback(OnReceiveSink), new object[2] { usession, new IPEndPoint(addr, ((IPEndPoint)session.Client.LocalEndPoint).Port) });
                        }
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(ex.ToString());
                    } // Sometimes the bind will throw an exception. In this case, we want to skip that interface and move on.
                }
            }
        }
        
        private void OnReceiveSink(IAsyncResult result)
        {
            IPEndPoint ep = null;
            object[] args = (object[])result.AsyncState;
            UdpClient session = (UdpClient)args[0];
            IPEndPoint local = (IPEndPoint)args[1];

            try
            {
                byte []buffer = session.EndReceive(result, ref ep);
                var recvstr = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
                var lines = recvstr.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool isNovitecCamera = false;
                string serial = null;
                string locationIp = null;
                bool pwset = true;
                foreach (var line_org in lines)
                {
                    string line = line_org.ToUpper();
                    if (line.StartsWith("LOCATION:"))
                    {
                        // Get the source IP address from an End point - this is more reliable.
                        locationIp = ep.Address.ToString();
                    }
                    else if (line.StartsWith("USN: UUID:NOVITEC_"))
                    {
                        int pos = line.IndexOf("::UPNP");
                        if (pos < 22)
                            serial = null; // old firmware with no serial num in the line
                        else
                        {
                            serial = line.Substring(18, pos - 18);
                        }

                        isNovitecCamera = true;
                    }
                    else if (line.StartsWith("PWSET:"))
                    {
                        string pwset_str = line.Substring(7, line.Length - 7);
                        if (pwset_str == "TRUE") pwset = true;
                        else pwset = false;
                    }
                }
                if (isNovitecCamera && locationIp != null)
                {
                    // update the dictionary to remember the time received.
                    lock (m_ipListLock)
                    {
                        if (ipList.ContainsKey(locationIp))
                            ipList.Remove(locationIp);
                        ipList.Add(locationIp, DateTime.Now);
                    }
                    IpUpdated(this, new IpUpdatedEventArgs(locationIp, serial, true, pwset));
                }
                session.BeginReceive(new AsyncCallback(OnReceiveSink), args);
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

    }

    class IpUpdatedEventArgs : System.EventArgs
    {
        public readonly string ipAddress;
        public readonly string serialNumber;
        public readonly bool added;
        public readonly bool pwset;

        public IpUpdatedEventArgs(string ip, string serial, bool _added, bool _pwset)
        {
            ipAddress = ip;
            serialNumber = serial;
            added = _added;
            pwset = _pwset;
        }
    }

    class IPCameraUtils
    {
        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            int width = (int)bitmapsource.Width;
            int height = (int)bitmapsource.Height;
            Bitmap bitmap = new Bitmap(width,
                                        height,
                                        System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.WriteOnly,
                                                PixelFormat.Format32bppRgb);
            bitmapsource.CopyPixels(System.Windows.Int32Rect.Empty, bits.Scan0, width * height * 4, width * 4);
            bitmap.UnlockBits(bits);

            return bitmap;
        }

        public static BitmapSource CreateBitmapSourceFromByteArray(byte []buffer, int width, int height)
        {
            BitmapSource bitmapSource = null;
            var dpi = 96d;
            var pixelFormat = System.Windows.Media.PixelFormats.Gray8;
            var bytesPerPixel = 1;
            var stride = bytesPerPixel * width;
            try
            {
                bitmapSource = BitmapSource.Create(width, height, dpi, dpi, pixelFormat, null, buffer, stride);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("buf size=" + buffer.Length);
            }
            return bitmapSource;
        }
    }

    public static class NetworkUtils
    {
        // http://www.codeproject.com/KB/IP/host_info_within_network.aspx
        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", ExactSpelling = true)]
        static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref int PhyAddrLen);

        /// <summary>
        /// Gets the MAC address (<see cref="PhysicalAddress"/>) associated with the specified IP.
        /// </summary>
        /// <param name="ipAddress">The remote IP address.</param>
        /// <returns>The remote machine's MAC address.</returns>
        public static PhysicalAddress GetMacAddress(IPAddress ipAddress)
        {
            const int MacAddressLength = 6;
            int length = MacAddressLength;
            var macBytes = new byte[MacAddressLength];
            SendARP(BitConverter.ToInt32(ipAddress.GetAddressBytes(), 0), 0, macBytes, ref length);
            return new PhysicalAddress(macBytes);
        }
    }

    public class YUVtoRGB
    {
        // These member variables are for incremental conversion only.
        private byte[] m_yuv;
        private byte[] m_rgb;
        private byte[] m_rgb_final;
        private Bitmap m_bitmap;
        private int m_processedLines;
        private int m_width;
        private int m_height;
        private object m_bitmapLock = new object();
        private bool m_needUpdate = false;

        public void InitYUVtoRGB_incremental(int width, int height, byte[] buffer)
        {
            m_width = width;
            m_height = height;
            m_yuv = buffer;
            m_processedLines = 0;

            if (m_rgb == null || m_rgb.Length != width * height * 4)
                m_rgb = new byte[width * height * 4];

            lock (m_bitmapLock)
            {
                if (m_rgb_final == null || m_rgb_final.Length != width * height * 4)
                    m_rgb_final = new byte[width * height * 4];
            }
        }

        public void DoYUVtoRGB_incremental(int validLength)
        {
            int uv_size = m_width * m_height / 2;
            const int bpp = 4;

            // Still receiving UV pixels?
            if (validLength < uv_size)
                return;

            //
            // Now we have Y pixels to process.
            //

            // have new lines?
            int lines = (validLength - uv_size) / m_width;
            lines = lines - lines % 2; // two lines are processed at the same time.
            if (lines <= m_processedLines)
                return;

            // Process YUV->RGB for the new lines.
            for (int y = m_processedLines; y < lines; y += 2)
            {
                for (int x = 0; x < m_width; x += 2)
                {
                    int bases = y * m_width + x + uv_size;

                    int y1 = (int)m_yuv[bases] << 8;
                    int y2 = (int)m_yuv[bases + 1] << 8;
                    int y3 = (int)m_yuv[bases + m_width] << 8;
                    int y4 = (int)m_yuv[bases + m_width + 1] << 8;

                    int x2 = x + 0; // TODO: Test UV shifting phenomenon.
                    int v1 = (int)(((int)m_yuv[y * m_width / 2 + x2] - 128) * 292);
                    int v2 = (int)(((int)m_yuv[y * m_width / 2 + x2] - 128) * 149);
                    int u1 = (int)(((int)m_yuv[y * m_width / 2 + x2 + 1] - 128) * 100);
                    int u2 = (int)(((int)m_yuv[y * m_width / 2 + x2 + 1] - 128) * 520);

                    int basep = (y * m_width + x) * bpp;

                    m_rgb[basep + 0] = (byte)(Math.Max(Math.Min(y1 + v1, 255 << 8), 0) >> 8);       // R
                    m_rgb[basep + 1] = (byte)(Math.Max(Math.Min(y1 - u1 - v2, 255 << 8), 0) >> 8);  // G
                    m_rgb[basep + 2] = (byte)(Math.Max(Math.Min(y1 + u2, 255 << 8), 0) >> 8);       // B
                  
                    m_rgb[(basep + bpp + 0)] = (byte)(Math.Max(Math.Min(y2 + v1, 255 << 8), 0) >> 8);       // R
                    m_rgb[(basep + bpp + 1)] = (byte)(Math.Max(Math.Min(y2 - u1 - v2, 255 << 8), 0) >> 8);  // G
                    m_rgb[(basep + bpp + 2)] = (byte)(Math.Max(Math.Min(y2 + u2, 255 << 8), 0) >> 8);       // B

                    m_rgb[(basep + m_width * bpp) + 0] = (byte)(Math.Max(Math.Min(y3 + v1, 255 << 8), 0) >> 8);      // R
                    m_rgb[(basep + m_width * bpp) + 1] = (byte)(Math.Max(Math.Min(y3 - u1 - v2, 255 << 8), 0) >> 8); // G
                    m_rgb[(basep + m_width * bpp) + 2] = (byte)(Math.Max(Math.Min(y3 + u2, 255 << 8), 0) >> 8);      // B

                    m_rgb[basep + (m_width + 1) * bpp + 0] = (byte)(Math.Max(Math.Min(y4 + v1, 255 << 8), 0) >> 8);      // R
                    m_rgb[basep + (m_width + 1) * bpp + 1] = (byte)(Math.Max(Math.Min(y4 - u1 - v2, 255 << 8), 0) >> 8); // G
                    m_rgb[basep + (m_width + 1) * bpp + 2] = (byte)(Math.Max(Math.Min(y4 + u2, 255 << 8), 0) >> 8);      // B
                }
            }
                
            m_processedLines = lines;

            if (m_processedLines == m_height)
            {
                lock (m_bitmapLock)
                {
                    m_needUpdate = true;
                    Array.Copy(m_rgb, m_rgb_final, m_rgb.Length);
                }
            }
        }

        // The coeffients are from https://www.fourcc.org/fccyvrgb.php
        public void DoYUVtoRGB_incremental_TEST(int validLength)
        {
            int uv_size = m_width * m_height / 2;
            const int bpp = 4;

            // Still receiving UV pixels?
            if (validLength < uv_size)
                return;

            //
            // Now we have Y pixels to process.
            //

            // have new lines?
            int lines = (validLength - uv_size) / m_width;
            lines = lines - lines % 2; // two lines are processed at the same time.
            if (lines <= m_processedLines)
                return;

            // Process YUV->RGB for the new lines.
            for (int y = m_processedLines; y < lines; y += 2)
            {
                for (int x = 0; x < m_width; x += 2)
                {
                    int bases = y * m_width + x + uv_size;
                    int y1 = (int)((m_yuv[bases] - 16) * 1.164);
                    int y2 = (int)((m_yuv[bases + 1] - 16) * 1.164);
                    int y3 = (int)((m_yuv[bases + m_width] - 16) * 1.164);
                    int y4 = (int)((m_yuv[bases + m_width + 1] - 16) * 1.164);

                    int v1 = (int)(((int)m_yuv[y * m_width / 2 + x] - 128) * 1.596);
                    int v2 = (int)(((int)m_yuv[y * m_width / 2 + x] - 128) * 0.813);
                    int u1 = (int)(((int)m_yuv[y * m_width / 2 + x + 1] - 128) * 0.391);
                    int u2 = (int)(((int)m_yuv[y * m_width / 2 + x + 1] - 128) * 2.018);

                    int basep = (y * m_width + x) * bpp;
                    m_rgb[basep + 0] = (byte)Math.Max(Math.Min(y1 + v1, 255), 0); // R
                    m_rgb[basep + 1] = (byte)Math.Max(Math.Min(y1 - u1 - v2, 255), 0); // G
                    m_rgb[basep + 2] = (byte)Math.Max(Math.Min(y1 + u2, 255), 0); // B

                    m_rgb[(basep + bpp + 0)] = (byte)Math.Max(Math.Min(y2 + v1, 255), 0); // R
                    m_rgb[(basep + bpp + 1)] = (byte)Math.Max(Math.Min(y2 - u1 - v2, 255), 0); // G
                    m_rgb[(basep + bpp + 2)] = (byte)Math.Max(Math.Min(y2 + u2, 255), 0); // B

                    m_rgb[(basep + m_width * bpp) + 0] = (byte)Math.Max(Math.Min(y3 + v1, 255), 0); // R
                    m_rgb[(basep + m_width * bpp) + 1] = (byte)Math.Max(Math.Min(y3 - u1 - v2, 255), 0); // G
                    m_rgb[(basep + m_width * bpp) + 2] = (byte)Math.Max(Math.Min(y3 + u2, 255), 0); // B

                    m_rgb[basep + (m_width + 1) * bpp + 0] = (byte)Math.Max(Math.Min(y4 + v1, 255), 0); // R
                    m_rgb[basep + (m_width + 1) * bpp + 1] = (byte)Math.Max(Math.Min(y4 - u1 - v2, 255), 0); // G
                    m_rgb[basep + (m_width + 1) * bpp + 2] = (byte)Math.Max(Math.Min(y4 + u2, 255), 0); // B         
                }
            }

            m_processedLines = lines;
        }

        public Bitmap GetBitmap(bool update = true)
        {
            lock (m_bitmapLock)
            {
                if (update || m_needUpdate)
                {
                    m_needUpdate = false;
                    var pixfmt = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
                    m_bitmap = new Bitmap(m_width, m_height, pixfmt);
                    BitmapData bdata = m_bitmap.LockBits(new Rectangle(0, 0, m_width, m_height),
                                                    ImageLockMode.ReadOnly,
                                                    PixelFormat.Format32bppRgb);
                    int size = bdata.Width * bdata.Height * 4;
                    System.Runtime.InteropServices.Marshal.Copy(m_rgb_final, 0, bdata.Scan0, size);
                    m_bitmap.UnlockBits(bdata);
                }
                return m_bitmap;
            }

        }

        public BitmapSource GetBitmapSource_YUVtoRGB_incremental()
        {
            BitmapSource source = null;
            
            try{
                //lock (m_bitmapLock)
                {
                    source = BitmapSource.Create(m_width,
                                        m_height,
                                        96d,
                                        96d,
                                        System.Windows.Media.PixelFormats.Bgr32,
                                        null,
                                        m_rgb,
                                        m_width * 4);
                    source.Freeze();
                }
             }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("buf size=" + m_rgb.Length);
            }
            return source;
        }

        // 32-bit RGB
        public Bitmap CreateBitmapFromYUV(int width, int height, byte[] buffer)
        {
            var pixfmt = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            //var pixfmt = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            Bitmap bitmap = new Bitmap(width, height, pixfmt); ;

            int[] rgb = new int[width * height];
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    int bases = y * width + x;
                    int y1 = (int)(buffer[bases] - 16) * 298;
                    int y2 = (int)(buffer[bases + 1] - 16) * 298;
                    int y3 = (int)(buffer[bases + width] - 16) * 298;
                    int y4 = (int)(buffer[bases + width + 1] - 16) * 298;

                    int v1 = (int)(buffer[height * width + y * width / 2 + x] - 128) * 409;
                    int v2 = (int)(buffer[height * width + y * width / 2 + x] - 128) * 208;
                    int u = (int)buffer[height * width + y * width / 2 + x + 1] - 128;

                    int basep = y * width + x;
                    rgb[basep + 0] = Math.Max(Math.Min(((y1 + v1 + 128) >> 8), 255), 0); // R
                    rgb[basep + 0] += Math.Max(Math.Min(((y1 - 100 * u - v2 + 128) >> 8), 255), 0) << 8; // G
                    rgb[basep + 0] += Math.Max(Math.Min(((y1 + 516 * u + 128) >> 8), 255), 0) << 16; // B

                    rgb[(basep + 1)] = Math.Max(Math.Min(((y2 + v1 + 128) >> 8), 255), 0); // R
                    rgb[(basep + 1)] += Math.Max(Math.Min(((y2 - 100 * u - v2 + 128) >> 8), 255), 0) << 8; // G
                    rgb[(basep + 1)] += Math.Max(Math.Min(((y2 + 516 * u + 128) >> 8), 255), 0) << 16; // B

                    rgb[(basep + width)] = Math.Max(Math.Min(((y3 + v1 + 128) >> 8), 255), 0); // R
                    rgb[(basep + width)] += Math.Max(Math.Min(((y3 - 100 * u - v2 + 128) >> 8), 255), 0) << 8; // G
                    rgb[(basep + width)] += Math.Max(Math.Min(((y3 + 516 * u + 128) >> 8), 255), 0) << 16; // B

                    rgb[(basep + width + 1)] = Math.Max(Math.Min(((y4 + v1 + 128) >> 8), 255), 0); // R
                    rgb[(basep + width + 1)] += Math.Max(Math.Min(((y4 - 100 * u - v2 + 128) >> 8), 255), 0) << 8; // G
                    rgb[(basep + width + 1)] += Math.Max(Math.Min(((y4 + 516 * u + 128) >> 8), 255), 0) << 16; // B         

                }
            }

            BitmapData bdata = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                                ImageLockMode.ReadOnly,
                                                PixelFormat.Format32bppRgb);
            int size = bdata.Width * bdata.Height;
            byte[] pixels = new byte[size];
            System.Runtime.InteropServices.Marshal.Copy(rgb, 0, bdata.Scan0, size);
            bitmap.UnlockBits(bdata);

            return bitmap;
        }

        // TODO: remove "fixed" usage so that this compiles without "Allow unsafe code" option.
        /*
        // For monochrome or faster frame rate.
        public Bitmap CreateBitmapFromY(int width, int height, byte[] buffer)
        {
            System.Drawing.Imaging.PixelFormat pixfmt = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            Bitmap bitmap;

            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    IntPtr pAddr = (IntPtr)ptr;
                    bitmap = new Bitmap(width, height, width * 1, pixfmt, pAddr);
                }
            }

            {
                ColorPalette ncp = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                    ncp.Entries[i] = Color.FromArgb(255, i, i, i);
                bitmap.Palette = ncp;
            }

            return bitmap;
        }*/
    }

}
