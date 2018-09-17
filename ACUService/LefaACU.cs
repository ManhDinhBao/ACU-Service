using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;

namespace ACUService
{
    public partial class LefaACU : ServiceBase
    {
        const int MAX_CLIENTS = 1000;
        const int PORT = 16707;

        public AsyncCallback pfnWorkerCallBack;
        private Socket m_mainSocket;
        private Socket[] m_workerSocket = new Socket[10];
        private int m_clientCount = 0;

        public LefaACU()
        {
            InitializeComponent();
        }
        
        protected override void OnStart(string[] args)
        {

            OpenSocket();
            Utilities.WriteLogError("WindowsService ACU start");
        }

        protected override void OnStop()
        {
            CloseSockets();
            Utilities.WriteLogError("WindowsService ACU stop");
        }

        protected override void OnPause()
        {
            base.OnPause();
            CloseSockets();
            Utilities.WriteLogError("WindowsService ACU pause");
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            OpenSocket();
            Utilities.WriteLogError("WindowsService ACU resume");
        }

        private void OpenSocket()
        {
            // Create the listening socket...
            m_mainSocket = new Socket(AddressFamily.InterNetwork,
                                      SocketType.Stream,
                                      ProtocolType.Tcp);
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, PORT);
            // Bind to local IP Address...
            m_mainSocket.Bind(ipLocal);
            // Start listening...
            m_mainSocket.Listen(100);
            // Create the call back for any client connections...
            m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }
        public void OnClientConnect(IAsyncResult asyn)
        {
            try
            {
                // Here we complete/end the BeginAccept() asynchronous call
                // by calling EndAccept() - which returns the reference to
                // a new Socket object
                m_workerSocket[m_clientCount] = m_mainSocket.EndAccept(asyn);
                // Let the worker Socket do the further processing for the 
                // just connected client
                WaitForData(m_workerSocket[m_clientCount]);
                //Get IP of client	
                IPEndPoint remoteIpEndPoint = m_workerSocket[m_clientCount].RemoteEndPoint as IPEndPoint;

                // Now increment the client count
                ++m_clientCount;
               
                //lblStatus.Text = str;                

                // Since the main Socket is now free, it can go back and wait for
                // other clients who are attempting to connect
                m_mainSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\n OnClientConnection: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
            }
           

        }
        // Start waiting for data from the client
        public void WaitForData(System.Net.Sockets.Socket soc)
        {
            try
            {
                if (pfnWorkerCallBack == null)
                {
                    // Specify the call back function which is to be 
                    // invoked when there is any write activity by the 
                    // connected client
                    pfnWorkerCallBack = new AsyncCallback(OnDataReceived);
                }
                SocketPacket theSocPkt = new SocketPacket();
                theSocPkt.m_currentSocket = soc;
                // Start receiving any data written by the connected client
                // asynchronously
                soc.BeginReceive(theSocPkt.dataBuffer, 0,
                                   theSocPkt.dataBuffer.Length,
                                   SocketFlags.None,
                                   pfnWorkerCallBack,
                                   theSocPkt);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
            }

        }


        public class SocketPacket
        {
            public System.Net.Sockets.Socket m_currentSocket;

            public byte[] dataBuffer = new byte[100];
        }

        public void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                SocketPacket socketData = (SocketPacket)asyn.AsyncState;

                int iRx = 0;
                // Complete the BeginReceive() asynchronous call by EndReceive() method
                // which will return the number of characters written to the stream 
                // by the client
                iRx = socketData.m_currentSocket.EndReceive(asyn);
                char[] chars = new char[iRx + 1];
                System.Text.Decoder d = System.Text.Encoding.UTF8.GetDecoder();
                int charLen = d.GetChars(socketData.dataBuffer,
                                         0, iRx, chars, 0);
                System.String szData = new System.String(chars);

                //Get cilent IP
                IPEndPoint remoteIpEndPoint = socketData.m_currentSocket.RemoteEndPoint as IPEndPoint;
                string IP = remoteIpEndPoint.Address.ToString();

                //Get string message 
                string message = BitConverter.ToString(socketData.dataBuffer);

                //Get function code
                string strCode = ArrayToStringNonInt(socketData, 0, 1);
                string code = string.Format("0x{0}", strCode);
                string functionName = Common.GetEnumName(code);

                //Get messlength
                string strLength = ArrayToStringNonInt(socketData, 2, 3);
                int intMesslength = int.Parse(strLength, System.Globalization.NumberStyles.HexNumber);

                //Get message data
                string strData = ArrayToStringNonInt(socketData, 4, intMesslength);                

                //If event is SEND_EVENT_RSP, save to DB
                if (functionName == "SEND_EVENT_RSP")
                {
                    int length = 5;
                    AddEventToDB(socketData, IP, functionName);                    

                    byte[] a = BitConverter.GetBytes((UInt16)Common.FunctionCode.SEND_EVENT_ACK);
                    List<byte> list = new List<byte>();
                    list.Add(a[1]);
                    list.Add(a[0]);

                    byte[] mlength = BitConverter.GetBytes((UInt16)length);
                    byte messId = socketData.dataBuffer[4];
                    List<byte> data = new List<byte>();
                    data.AddRange(list.ToArray());
                    data.Add(mlength[1]);
                    data.Add(mlength[0]);
                    data.Add(messId);
                    List<byte[]> send = new List<byte[]>();
                    send.Add(data.ToArray());
                    SendMessageToDevice(send, IP);
                }

                // Continue the waiting for data on the Socket
                WaitForData(socketData.m_currentSocket);
            }
            catch (ObjectDisposedException)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nOnDataReceived: Socket has been closed\n");
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
            }
        }

        private string ArrayToStringNonInt(SocketPacket p, int startPos, int endPos)
        {
            string result = null;
            try
            {
                int byteCount = endPos - startPos + 1;
                List<byte> byteArr = new List<byte>();
                for (int i = startPos; i <= endPos; i++)
                {
                    byteArr.Add(p.dataBuffer[i]);
                }
                result = BitConverter.ToString(byteArr.ToArray());
                if (byteCount > 1)
                {
                    for (int i = 1; i < byteCount; i++)
                    {
                        result = result.Remove(i * 2, 1);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                return null;                
            }
        }
        private void AddEventToDB(SocketPacket p, string deviceIP, string functionName)
        {
            try
            {
                string eventId = ArrayToString(p, 4, 4);
                string cardNo = ArrayToString(p, 5, 8);

                string strHour = ArrayToString(p, 9, 9);
                string strMin = ArrayToString(p, 10, 10);
                string strSec = ArrayToString(p, 11, 11);
                string strDay = ArrayToString(p, 12, 12);
                string strMonth = ArrayToString(p, 13, 13);
                string strYear = (Convert.ToInt16(ArrayToString(p, 14, 14)) + 1900).ToString();
                string myDate = string.Format("{0}/{1}/{2} {3}:{4}:{5}", strYear, strMonth, strDay, strHour, strMin, strSec);
                //DateTime eventDate = DateTime.ParseExact(myDate, "dd/MM/yyyy HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture);
                DateTime eventDate = Convert.ToDateTime(myDate);
                string doorId = ArrayToString(p, 15, 15);

                string strStatus = ArrayToString(p, 16, 16);
                string eventStatus = "";
                if (strStatus == "0")
                {
                    eventStatus = "GRANTED";
                }
                else
                    if (strStatus == "1")
                {
                    eventStatus = "DENY";
                }
                else
                    if (strStatus == "2")
                {
                    eventStatus = "NOT_DEFINED";
                }
                Utilities.WriteLogError(string.Format("{0} {1} {2} {3} {4} {5}",eventDate,deviceIP,functionName,cardNo,doorId, eventStatus));
                Event e = new Event(eventId, eventDate, deviceIP, functionName, cardNo, doorId, eventStatus);
                e.AddEvent();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void SendMessageToDevice(List<byte[]> messages, string IP)
        {
            try
            {
                for (int i = 0; i < m_clientCount; i++)
                {
                    IPEndPoint remoteIpEndPoint = m_workerSocket[i].RemoteEndPoint as IPEndPoint;

                    if (IP == remoteIpEndPoint.Address.ToString())
                    {
                        if (m_workerSocket[i] != null)
                        {
                            if (m_workerSocket[i].Connected)
                            {
                                foreach (byte[] b in messages)
                                {
                                    try
                                    {
                                        m_workerSocket[i].Send(b);
                                    }
                                    catch (SocketException se)
                                    {
                                        Console.WriteLine(se.ToString());
                                    }
                                }
                            }                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


        }

        private void CloseSockets()
        {
            if (m_mainSocket != null)
            {
                m_mainSocket.Close();
            }
            for (int i = 0; i < m_clientCount; i++)
            {
                if (m_workerSocket[i] != null)
                {
                    m_workerSocket[i].Close();
                    m_workerSocket[i] = null;
                }
            }
        }
        private string ArrayToString(SocketPacket p, int startPos, int endPos)
        {
            string result = null;
            try
            {
                int byteCount = endPos - startPos + 1;
                List<byte> byteArr = new List<byte>();
                for (int i = startPos; i <= endPos; i++)
                {
                    byteArr.Add(p.dataBuffer[i]);
                }
                result = BitConverter.ToString(byteArr.ToArray());
                if (byteCount > 1)
                {
                    for (int i = 1; i < byteCount; i++)
                    {
                        result = result.Remove(i * 2, 1);
                    }
                }
                result = int.Parse(result, System.Globalization.NumberStyles.HexNumber).ToString();
                return result;
            }
            catch (Exception ex)
            {
                return null;
                Console.WriteLine(ex.ToString());
            }
        }

    }
}
