using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class MessagePool
{
    private byte[] m_Buffer;
    private int m_Position;
    private System.Int32 m_MessageSize;
    private static object m_Lock = new object();
    private List<Message> m_MessagePool;
    private Dictionary<string, MessageHandler> m_CallBackDict;

    private const int MSG_LEN = 4;

    public MessagePool()
    {

    }

    public bool AddCallBack(string name, MessageHandler handler)
    {
        if (!m_CallBackDict.ContainsKey(name))
        {
            m_CallBackDict[name] = handler;
            return true;
        }
        return false;
    }

    public void ParseBuffer(byte[] buffer, int size, ProtocolType protocol)
    {
        ParseBuffer(buffer, size, protocol, 0, 0);
    }

    public void ParseBuffer(byte[] buffer, int size, ProtocolType protocol, uint ackIdx, uint msgIdx)
    {
        int position = 0;
        int NEED_LEN = MSG_LEN - m_Position;

        if (size - position >= NEED_LEN)
        {
            byte[] lengthBytes = new byte[MSG_LEN];
            Array.Copy(buffer, position, m_Buffer, m_Position, NEED_LEN);
            Array.Copy(m_Buffer, lengthBytes, MSG_LEN);
            // Converts a number from network byte order to host byte order.
            m_MessageSize = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));
            position += NEED_LEN;
            if (size - position == 0)
            {
                return;
            }
        }

        Array.Copy(buffer, position, m_Buffer, 0, m_MessageSize);
        position += m_MessageSize;

        Message message = new Message();
        uint recvIdx = 0;
        message.ReStore(m_Buffer, m_MessageSize);
        message.SetProtocol(protocol);
        message.m_AckIdx = ackIdx;
        message.m_MsgIdx = msgIdx;
        message.m_RecvIdx = ++recvIdx;
        message.m_RecvTime = DateTime.Now.Ticks / 10000;

        lock (m_Lock)
        {
            m_MessagePool.Add(message);
        }

        m_Position = 0;
        m_MessageSize = 0;
    }
}
