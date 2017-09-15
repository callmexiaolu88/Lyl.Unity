//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Lyl.Unity.WcfExtensions.Channels.UdpScoket
//{
//    class ComSocket : IDisposable
//    {

//        #region 私有字段
        
//        /// <summary>
//        /// Socket
//        /// </summary>
//        private Socket socket = null;

//        /// <summary>
//        /// Socket列表
//        /// </summary>
//        private static Dictionary<IPEndPoint,ComSocket> comSocketList = new Dictionary<IPEndPoint,ComSocket>();

//        #endregion 私有字段

//        #region 构造函数

//        /// <summary>
//        /// 构造函数
//        /// </summary>
//        /// <param name="endPoint">IP端口对象</param>
//        /// <param name="threadName">线程名称</param>
//        private ComSocket(IPEndPoint endPoint, string threadName)
//        {
//            try
//            {
//                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

//                uint IOC_IN = 0x80000000;
//                uint IOC_VENDOR = 0x18000000;
//                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
//                socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
//                socket.Bind(endPoint);
//            }
//            catch (Exception ex)
//            {
//                throw ex;
//            }
//        }

//        #endregion 构造函数
        
//        #region 公有方法
        
//        /// <summary>
//        /// 注册Socket
//        /// </summary>
//        /// <param name="conn">通信对象</param>
//        /// <returns>返回通信Socket</returns>
//        public static ComSocket RegisterSocket(CConnection conn)
//        {
//            if (conn == null)
//            {
//                throw new ArgumentNullException("conn");
//            }
//            if(comSocketList.ContainsKey(conn.LocalEndPoint))
//            {
//                comSocketList[conn.LocalEndPoint].AddConnection(conn);
//                return comSocketList[conn.LocalEndPoint];
//            }
//            var result = new ComSocket(conn.LocalEndPoint, conn.Device.Config.Abbr);
//            comSocketList.Add(conn.LocalEndPoint, result);
//            result.AddConnection(conn);
//            return result;
//        }

//        /// <summary>
//        /// 发送数据
//        /// </summary>
//        /// <param name="conn">通信对象</param>
//        /// <param name="data">发送的数据</param>
//        public void SendData(byte[] data)
//        {
//            try
//            {
//                socket.SendTo(data, conn.RemoteEndPoint);
//            }
//            catch (Exception)
//            {                
//                throw;
//            }
//        }

//        #endregion 公有方法

//        #region 私有方法

//        /// <summary>
//        /// 接收数据
//        /// </summary>
//        /// <param name="obj">参数</param>
//        private void RecvData(object obj)
//        {
//            byte[] byteRecvdata = new byte[10000];
//            EndPoint RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
//            while (true)
//            {
//                try
//                {
//                    int irevDataLength = 0;
//                    //接收数据
//                    irevDataLength = socket.ReceiveFrom(byteRecvdata, ref RemoteEndPoint);
//#if PC
//                    var conn = connections.Find(i => i.RemoteEndPoint.ToString() == RemoteEndPoint.ToString());
//#else
//                    var conn = connections.Find(i => i.RemoteEndPoint.Address.ToString() == ((IPEndPoint)RemoteEndPoint).Address.ToString());
//#endif                    
//                    if (CHMI.HmiRunning &&
//                        conn != null)
//                    {
//                        UInt32 revCRC;
//                        if (irevDataLength > 0 && CAuxiliaryFun.CRCCompare(conn, byteRecvdata, irevDataLength, out revCRC))
//                        {
//#if PERFORMANCE
//                            var stime = DateTime.Now;
//#endif
//                            conn.ProcessRecvMsg(byteRecvdata, irevDataLength, revCRC);
//#if PERFORMANCE
//                            var etime = DateTime.Now;
//                            CLog.Log("HMI_PERFORMANCE_REV_DATA",
//                                string.Format("{0}#{1}#{2}#{3}#{4}",
//                                CDataManager.SelfSystem.Abbr,
//                                "REV",
//                                (etime - stime).Milliseconds,
//                                irevDataLength,
//                                thread.Name ?? thread.ManagedThreadId.ToString()));
//#endif
//                        }
//                    }
//                }
//                catch (TaskCanceledException)
//                {
//                }
//                catch (ThreadAbortException)
//                {
//                }
//                catch(Exception ex)
//                {
//                    CLog.Log(ex);
//                }
//            }
//        }

//        #endregion 私有方法

//        #region IDisposable 成员

//        /// <summary>
//        /// 释放资源
//        /// </summary>
//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        /// <summary>
//        /// 释放资源
//        /// </summary>
//        /// <param name="disposing">是否释放资源</param>
//        private void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                if (socket != null)
//                {
//                    socket.Dispose();
//                }
//            }
//        }

//        #endregion
//    }
//}
