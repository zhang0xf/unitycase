using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Google.Protobuf;
using MsgDefine;

public class UdpClient
{
    private static UdpClient m_Instance = null;

    private string m_ServerHost;
    private int m_ServerPort;

    private IPEndPoint m_SendPoint;     // 发送地址;
    private EndPoint m_RecvPoint;       // 接受地址;
    private Thread m_SendThreads;       // 发送线程;
    private Thread m_RecvThreads;       // 接受线程;
    private Socket m_UdpSocket;			// 收发终端;
    private bool IsRunThreads = false;
    private object m_LockMsg = new object();
    private object m_LockIndex = new object();
    private object m_LockErrID = new object();
    private byte[] m_SendBuffer = new byte[4096];
    private byte[] m_RecvBuffer = new byte[4096];
    private AddressFamily m_AddressFamily = AddressFamily.InterNetwork;
    private const int m_ControlFlagSize = sizeof(System.Byte);
    private const int m_ControlCmdSize = sizeof(System.Byte);
    private const int m_ACKContentSize = sizeof(System.Int32);
    private List<Message> m_MsgList = new List<Message>();
    private Queue<uint> m_AckQueue = new Queue<uint>();
    private Queue<SocketError> m_ErrorQueue = new Queue<SocketError>();

    public UdpClient()
    {

    }

    public static UdpClient Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new UdpClient();
            }
            return m_Instance;
        }
    }

    private bool IsMessageConfirm(UInt32 packetType)
    {
        return (packetType & (UInt32)TUdpControlFlag.ProtoCmd) > 0;
    }

    private bool IsMessageNeedAck(UInt32 packetType)
    {
        return (packetType & (UInt32)TUdpControlFlag.ProtoNeedAck) > 0;
    }

    public void SendToServer(string host, int port, Message message, bool needAck)
    {
        // 设置并记录服务器地址信息
        CreateServiceHost(host, port);
        // 创建收发线程
        CreateThreads();
        // 设置数据包需要回包
        message.SetUdpPacketType(TUdpControlFlag.ProtoNeedAck, needAck);
        // 将要发送的Message压入队列,等待发送线程发送数据包.
        PushMessage(message);

    }

    private void CreateServiceHost(string host, int port)
    {
        try
        {
            m_ServerHost = host;
            m_ServerPort = port;

            string newHost = "";
            IPv6SupportMidleWare.GetIPType(m_ServerHost, "", out newHost, out m_AddressFamily);
            if (string.IsNullOrEmpty(newHost))
            {
                newHost = m_ServerHost;
            }

            IPAddress ipAddress = Dns.GetHostAddresses(newHost)[0];

            if (null == m_SendPoint ||
                m_SendPoint.Address.ToString() != ipAddress.ToString() ||
                m_SendPoint.Port != m_ServerPort)
            {
                m_SendPoint = new IPEndPoint(ipAddress, m_ServerPort);
            }

            if (null == m_RecvPoint)
            {
                m_RecvPoint = new IPEndPoint(ipAddress, m_ServerPort);
            }

            // Debug.Log( " m_SendPoint " + m_SendPoint.ToString() );
            // Debug.Log( " m_RecvPoint " + m_RecvPoint.ToString() );
        }
        catch (Exception exp)
        {
            Debug.LogFormat(" UDP Client CreateServiceHost: {0}", exp.ToString());
        }
    }

    public void CreateThreads()
    {
        try
        {
            if (IsRunThreads) { return; }
            IsRunThreads = true;

            m_SendThreads = new Thread(SendRun);
            // m_SendThreads.IsBackground = true; // 标记后台线程;
            m_SendThreads.Name = "SendRun";
            m_SendThreads.Start();

            m_RecvThreads = new Thread(RecvRun);
            // m_RecvThreads.IsBackground = true; // 标记后台线程;
            m_RecvThreads.Name = "RecvRun";
            m_RecvThreads.Start();

            try
            {
                m_UdpSocket = new Socket(m_AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                // 设置接受缓冲区
                m_UdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 64 * 1024);
            }
            catch (Exception exp)
            {
                Debug.Log(" UDP Client CreateThreads: " + exp.ToString());
            }

            // 设置接受超时【在接受数据之前保持阻塞状态的时间量（毫秒）】
            m_UdpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
        }
        catch (Exception exp)
        {
            Debug.Log("Create Threads Failed " + exp.ToString());
        }
    }

    private void SendRun()
    {
        DateTime LastSendMsgTime = DateTime.UtcNow;

        while (IsRunThreads)
        {
            try
            {
                if (null != m_UdpSocket)
                {
                    // 发送数据包
                    if (SendWaitMsg())
                    {
                        LastSendMsgTime = DateTime.UtcNow;
                        continue;
                    }

                    TimeSpan timeSpan = DateTime.UtcNow - LastSendMsgTime;

                    if (timeSpan.TotalSeconds > 7 && m_SendPoint != null)
                    {
                        LastSendMsgTime = DateTime.UtcNow;
                        // 发送心跳包
                        SendHeartBeat();
                    }
                }

                Thread.Sleep(10);
            }
            catch (Exception exp)
            {
                Debug.Log("UDP Send Error 221 " + exp.ToString() + "m_SendPoint" + m_SendPoint.ToString());
            }
        }
    }

    private bool SendWaitMsg()
    {
        if (m_MsgList.Count <= 0)
        {
            return false;
        }

        // 加锁
        lock (m_LockMsg)
        {
            if (m_MsgList.Count > 0)
            {
                // 遍历当前队列中的所有数据包,并发送
                for (int i = m_MsgList.Count - 1; i >= 0; i--)
                {
                    Message CurMsg = m_MsgList[i];
                    // 消息多次没有回应,不再重发.
                    if (CurMsg.IsEmptyTimeOut())
                    {
                        m_MsgList.RemoveAt(i);
                        continue;
                    }

                    int diff = Environment.TickCount - CurMsg.MsgEnterQueueTime;
                    int timeout = CurMsg.GetMsgTimeout();

                    if (diff >= timeout)
                    {
                        CurMsg.RemoveMsgTimeOut();
                        // 用于发送消息的buffer,先加入udp数据包的类型
                        m_SendBuffer[0] = CurMsg.GetUdpPacketType();
                        // 再加入具体数据
                        Array.Copy(CurMsg.GetBuffer(), 0, m_SendBuffer, m_ControlFlagSize, CurMsg.GetSize());

                        int size =
                            m_UdpSocket.SendTo(m_SendBuffer, CurMsg.GetSize() + m_ControlFlagSize, SocketFlags.None, m_SendPoint);

                        Debug.Log("UDP Send: " + CurMsg.GetType() + "  Idx:" + CurMsg.MsgIndex + "  TimeOut:" + timeout);

                        // 不需要等待回包的数据包
                        if (!IsMessageNeedAck(CurMsg.GetUdpPacketType()))
                        {
                            m_MsgList.RemoveAt(i);
                        }

                        // m_SendBuffer = new byte[4096];
                    }
                }
            }
        }

        return true;
    }

    private void SendHeartBeat()
    {
        uint index = 0;
        uint random = 0;

        lock (m_LockIndex)
        {
            index = NetManager.Instance.GetBufferIndex();
            random = NetManager.Instance.GetCheck();
        }

        MsgSession msgSession = new MsgSession();
        msgSession.Index = index;
        msgSession.Check = random;
        msgSession.Session = NetManager.Instance.m_ClientSession;

        MemoryStream stream = new MemoryStream();
        msgSession.WriteTo(stream);
        byte[] data = stream.ToArray();

        // 构造心跳包
        byte[] HeartBeatBuff = new byte[m_ControlFlagSize + m_ControlCmdSize + sizeof(System.Int32) + data.Length];

        // 填充心跳包协议头部
        HeartBeatBuff[0] = (byte)TUdpControlFlag.ProtoCmd;
        HeartBeatBuff[1] = (byte)TUdpControlCmd.ProtoCmdHeartbeat;

        // 填充长度
        byte[] lengthBytes = BitConverter.GetBytes(data.Length);
        Array.Copy(lengthBytes, 0, HeartBeatBuff, m_ControlFlagSize + m_ControlCmdSize, lengthBytes.Length);

        // 填充数据
        Array.Copy(data, 0, HeartBeatBuff, m_ControlFlagSize + m_ControlCmdSize + sizeof(System.Int32), data.Length);

        int size =
            m_UdpSocket.SendTo(HeartBeatBuff, HeartBeatBuff.Length, SocketFlags.None, m_SendPoint);

        Debug.LogFormat(" send heart beat size " + size);
    }

    private void RecvRun()
    {
        const int MSG_LENGTH = sizeof(Int32);
        byte[] lengthBytes = new byte[MSG_LENGTH];
        DateTime LastRecvMsgTime = DateTime.UtcNow;

        while (IsRunThreads)
        {
            try
            {
                TimeSpan timeSpan = DateTime.UtcNow - LastRecvMsgTime;
                if (timeSpan.TotalSeconds > 30)
                {
                    lock (m_LockErrID)
                    {
                        LastRecvMsgTime = DateTime.UtcNow;
                        m_ErrorQueue.Enqueue(SocketError.TimedOut);
                    }
                }

                if (null != m_UdpSocket)
                {
                    // Gets the amount of data that has been received from the network and is available to be read.
                    if (m_UdpSocket.Available > 0)
                    {
                        // Receives a datagram into the data buffer and stores the endpoint.
                        int size =
                            m_UdpSocket.ReceiveFrom(m_RecvBuffer, ref m_RecvPoint);

                        if (size < 5) { continue; }

                        LastRecvMsgTime = DateTime.UtcNow;
                        int CurrReadSize = 0;

                        // 读取数据长度
                        Array.Copy(m_RecvBuffer, 0, lengthBytes, 0, MSG_LENGTH);
                        int MSG_DATA_LENGTH = BitConverter.ToInt32(lengthBytes, 0);
                        CurrReadSize += MSG_LENGTH;

                        // 读取数据
                        if ((size - CurrReadSize) < MSG_DATA_LENGTH) { continue; }
                        LastRecvMsgTime = DateTime.UtcNow;
                        byte[] msgDataHeadBytes = new byte[MSG_DATA_LENGTH];
                        Array.Copy(m_RecvBuffer, CurrReadSize, msgDataHeadBytes, 0, MSG_DATA_LENGTH);
                        CurrReadSize += MSG_DATA_LENGTH;

                        // 反序列化
                        MemoryStream stream = new MemoryStream(msgDataHeadBytes);
                        MsgDataHead msgDataHead = MsgDataHead.Parser.ParseFrom(stream);

                        if (IsMessageConfirm(msgDataHead.ControlFlag))
                        {
                            if (msgDataHead.ControlCmd == (UInt32)TUdpControlCmd.ProtoCmdAck)
                            {
                                RecvAcK(msgDataHead.AckIdx);
                            }
                        }
                        else
                        {
                            // 消息不是ACK或心跳包
                            if ((msgDataHead.ControlCmd & (UInt32)TUdpControlFlag.ProtoCmd) == 0)
                            {
                                if ((size - CurrReadSize) < MSG_DATA_LENGTH) { continue; }

                                if (IsMessageNeedAck(msgDataHead.ControlFlag))
                                {
                                    if (!SendAck(msgDataHead.AckIdx))
                                        continue;
                                }

                                MSG_DATA_LENGTH = size - CurrReadSize;
                                byte[] dataBytes = new byte[MSG_DATA_LENGTH];
                                Array.Copy(m_RecvBuffer, CurrReadSize, dataBytes, 0, MSG_DATA_LENGTH);
                                CurrReadSize += MSG_DATA_LENGTH;
                                NetManager.Instance.m_MessagePool.
                                    ParseBuffer(dataBytes, MSG_DATA_LENGTH, ProtocolType.Udp, msgDataHead.AckIdx, msgDataHead.MsgIdx);
                            }
                        }
                    }
                }
                Thread.Sleep(2);
            }
            catch (Exception exp)
            {
                Debug.Log("Receive failed and Thread exit" + exp.ToString());
            }
        }
    }

    private bool RecvAcK(uint ackIdx)
    {
        if (m_MsgList.Count <= 0) { return true; }

        lock (m_LockMsg)
        {
            for (int i = 0; i < m_MsgList.Count; i++)
            {
                Message CurMsg = m_MsgList[i];

                if (null == CurMsg) { continue; }

                if (CurMsg.MsgIndex == ackIdx)
                {
                    m_MsgList.Remove(CurMsg);
                    break;
                }
            }
        }

        return true;
    }

    private bool SendAck(uint ackIdx)
    {
        byte[] AckBuff = new byte[m_ControlFlagSize + m_ControlCmdSize + m_ACKContentSize];

        AckBuff[0] = (byte)TUdpControlFlag.ProtoCmd;
        AckBuff[1] = (byte)TUdpControlCmd.ProtoCmdAck;

        byte[] ackBytes = BitConverter.GetBytes(ackIdx);

        Array.Copy(ackBytes, 0, AckBuff, m_ControlFlagSize + m_ControlCmdSize, m_ACKContentSize);

        int size =
            m_UdpSocket.SendTo(AckBuff, AckBuff.Length, SocketFlags.None, m_SendPoint);

        return PushAckOverlap(ackIdx);
    }

    private bool PushAckOverlap(uint ackIdx)
    {
        try
        {
            if (m_AckQueue.Contains(ackIdx)) { return false; }

            if (m_AckQueue.Count >= 150)
            {
                m_AckQueue.Dequeue();
            }

            m_AckQueue.Enqueue(ackIdx);
        }
        catch (Exception exp)
        {
            Debug.Log(" push ACK exception " + exp.ToString());
        }

        return true;
    }

    private void PushMessage(Message message)
    {
        message.MsgEnterQueueTime = System.Environment.TickCount;

        lock (m_LockMsg)
        {
            m_MsgList.Add(message);
        }
    }
}
