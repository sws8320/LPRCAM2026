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

namespace KyungsinLPR
{
    class SSDPUtil
    {
        private System.Threading.Timer timer;
        private Dictionary<string, DateTime> ipList = new Dictionary<string, DateTime>();
        private object m_ipListLock = new object();

        public SSDPUtil()
        {
            PollInterface();
        }

        private volatile bool _stopped = false;

        public void OnTimer(object state)
        {
            if (_stopped) return;
            try
            {
                var now = DateTime.Now;
                string removeKey = null;
                lock (m_ipListLock)
                {
                    foreach (KeyValuePair<string, DateTime> pair in ipList)
                    {
                        if ((now - pair.Value).TotalSeconds > 3)
                        {
                            removeKey = pair.Key;
                            ipList.Remove(pair.Key);
                            break;
                        }
                    }
                }
                if (removeKey != null)
                {
                    var handler = IpUpdated;
                    if (handler != null && !_stopped)
                    {
                        try { handler(this, new IpUpdatedEventArgs(removeKey, false)); }
                        catch (Exception ex) { try { Util.Logger.Log("[SSDPUtil.OnTimer handler] " + ex.Message); } catch { } }
                    }
                }
            }
            catch (Exception ex)
            {
                try { Util.Logger.Log("[SSDPUtil.OnTimer] " + ex.Message); } catch { }
            }
        }

        /// <summary>타이머 + 이벤트 정리 — frmEnv 종료 시 호출.</summary>
        public void Stop()
        {
            _stopped = true;
            try { timer?.Dispose(); timer = null; } catch { }
            IpUpdated = null;
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
                timer = new System.Threading.Timer(OnTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

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
                            }
                            try { session.ExclusiveAddressUse = false; }
                            catch (SocketException )
                            {
                            }
                            session.Client.Bind(new IPEndPoint(addr, 1900));
                            session.EnableBroadcast = true;
                            session.JoinMulticastGroup(UpnpMulticastV4Addr, addr);
                            try { session.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue); }
                            catch (SocketException )
                            {
                            }
                            session.BeginReceive(new AsyncCallback(OnReceiveSink), new object[2] { session, new IPEndPoint(addr, ((IPEndPoint)session.Client.LocalEndPoint).Port) });

                            UdpClient usession = new UdpClient(AddressFamily.InterNetwork);
                            usession.Client.Bind(new IPEndPoint(addr, 0));
                            try { usession.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue); }
                            catch (SocketException)
                            {
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
                string locationIp = null;
                foreach (var line_org in lines)
                {
                    string line = line_org.ToUpper();
                    if (line.StartsWith("LOCATION:"))
                    {
                        // Get the source IP address from an End point - this is more reliable.
                        locationIp = ep.Address.ToString();
                    }
                    else if (line.StartsWith("USN: UUID:NOVITEC_")) // TODO: use other field - user can change the camera name.
                    {
                        isNovitecCamera = true;
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
                    IpUpdated(this, new IpUpdatedEventArgs(locationIp, true));
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
        public readonly String ipAddress;
        public readonly bool added;

        public IpUpdatedEventArgs(string ip, bool _added)
        {
            ipAddress = ip;
            added = _added;
        }
    }

    class IPCameraUtils
    {
        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
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

}
