using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using StrLen = System.Int32;
using MsgDefine;

public class Message
{
    public uint m_AckIdx = 0;
    public uint m_MsgIdx = 0;
    public long m_RecvTime = 0;
    public uint m_RecvIdx = 0;

    private int m_Position = 0;
    private int m_Size = 0;
    private string m_Name;
    private byte[] m_Buffer;
    private uint m_MsgIndex;
    private int m_MsgEnterQueueTime;

    private Queue<int> m_TimeoutQueue;
    private byte m_UdpPacketType = (byte)TUdpControlFlag.ProtoNeedAck;
    private ProtocolType m_ProtocolType;

    public Message()
    {
        m_Buffer = new byte[1024];
        m_Position = 0;
        m_Size = 0;
    }

    public Message(int size)
    {
        m_Buffer = new byte[size];
        m_Position = 0;
        m_Size = 0;
    }

    public Message(string msgName)
    {
        m_TimeoutQueue = new Queue<int>();

        m_TimeoutQueue.Enqueue(0);
        m_TimeoutQueue.Enqueue(100);
        m_TimeoutQueue.Enqueue(300);
        m_TimeoutQueue.Enqueue(600);

        m_Buffer = new byte[1024];
        m_Position = 0;
        m_Size = 0;
        m_Name = msgName;
        Write((Int32)0);    // 服务器代码(ryzomcore)要求一个HeaderSize = 4
        Write(msgName);
    }

    public int GetSize() { return m_Size; }

    public string GetMsgType() { return m_Name; }

    public byte[] GetBuffer() { return m_Buffer; }

    public uint MsgIndex
    {
        set { m_MsgIndex = value; }
        get { return m_MsgIndex; }
    }

    public int MsgEnterQueueTime
    {
        set { m_MsgEnterQueueTime = value; }
        get { return m_MsgEnterQueueTime; }
    }

    public void ReStore(byte[] bytes, int size)
    {
        try
        {
            int pos = 1 + 4;    // ?
            const int SIZE = sizeof(StrLen);
            byte[] lengthField = new byte[SIZE];
            Array.Copy(bytes, pos, lengthField, 0, SIZE);
            StrLen length = BitConverter.ToInt32(lengthField, 0);
            pos += SIZE;

            byte[] stringField = new byte[length];
            Array.Copy(bytes, pos, stringField, 0, length);
            m_Name = Encoding.Default.GetString(stringField);
            pos += length;

            m_Size = size - pos;

            if (m_Size > 0)
            {
                Array.Copy(bytes, pos, m_Buffer, 0, m_Size);
            }
            m_Position = 0;
        }
        catch (Exception exp)
        {
            Debug.Log(" Exception: " + exp.ToString() + "name:" + m_Name);
        }
    }

    public void Write(byte val)
    {
        byte[] bytes = BitConverter.GetBytes(val);
        m_Buffer[m_Position] = bytes[0];
        ++m_Position;
        ++m_Size;
    }

    public void Write(Int16 val)
    {
        byte[] bytes = BitConverter.GetBytes(val);
        for (int i = 0; i < sizeof(short); ++i)
        {
            m_Buffer[m_Position] = bytes[i];
            ++m_Position;
            ++m_Size;
        }
    }

    public void Write(Int32 val)
    {
        byte[] bytes = BitConverter.GetBytes(val);
        for (int i = 0; i < sizeof(int); ++i)
        {
            m_Buffer[m_Position] = bytes[i];
            ++m_Position;
            ++m_Size;
        }
    }

    public void Write(Int64 val)
    {
        byte[] bytes = BitConverter.GetBytes(val);
        for (int i = 0; i < sizeof(long); ++i)
        {
            m_Buffer[m_Position] = bytes[i];
            ++m_Position;
            ++m_Size;
        }
    }

    public void Write(string val)
    {
        Write((StrLen)val.Length);
        AddToBuffer(Encoding.Default.GetBytes(val), 0, val.Length);
    }

    public void WriteUnicode(string val)
    {
        Write((StrLen)val.Length);
        AddToBuffer(Encoding.Unicode.GetBytes(val), 0, val.Length);
    }

    public void Write(byte[] bytes)
    {
        // 写入长度
        Write((StrLen)bytes.Length);
        // 写入内容
        AddToBuffer(bytes, 0, bytes.Length);
    }

    public void AddToBuffer(byte[] bytes, int index, int size)
    {
        Array.Copy(bytes, index, m_Buffer, m_Position, size);
        m_Position += size;
        m_Size += size;
    }

    public bool IsEmptyTimeOut()
    {
        if (m_TimeoutQueue.Count > 0) { return false; }
        return true;
    }

    public int GetMsgTimeout()
    {
        if (m_TimeoutQueue.Count <= 0) { return 0; }
        return m_TimeoutQueue.Peek();
    }

    public void RemoveMsgTimeOut()
    {
        if (m_TimeoutQueue.Count <= 0) { return; }
        m_TimeoutQueue.Dequeue();
    }

    public byte GetUdpPacketType() { return m_UdpPacketType; }

    public void SetProtocol(ProtocolType type) { m_ProtocolType = type; }

    public byte SetUdpPacketType(TUdpControlFlag type, bool needAck)
    {
        if (needAck)
        {
            m_UdpPacketType |= (byte)type;
        }
        else
        {
            m_UdpPacketType &= (byte)~(byte)type;
        }
        return m_UdpPacketType;
    }
}
