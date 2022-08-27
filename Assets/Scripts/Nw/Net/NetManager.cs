using Google.Protobuf;
using System;
using System.IO;
using MsgDefine;

public class NetManager
{
    private static NetManager m_Instance = null;

    public ulong m_ClientSession = 0;
    private uint m_Buffindex = 0;
    private byte[] m_ZeroBytes = new byte[0];
    private object m_LockIndex = new object();
    private NextSession m_NextSession = new NextSession();
    public MessagePool m_MessagePool = new MessagePool();

    public NetManager()
    {

    }

    public static NetManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = new NetManager();
            }
            return m_Instance;
        }
    }

    public bool AddCallBack(string name, MessageHandler handler)
    {
        return m_MessagePool.AddCallBack(name, handler);
    }

    public uint GetBufferIndex() { return m_Buffindex++; }

    public uint GetCheck() { return (uint)m_NextSession.next(); }

    public void SendMsgToServer(string key)
    {
        SendMsgToServer(key, m_ZeroBytes);
    }

    // 泛型接口
    // message "Person" inherit from IMessage<Person>, and any other message will inherit from IMessage<T> too.
    // we use this implement generic interface which can handle all message type.
    public void SendMsgToServer<T>(string key, T msg) where T : IMessage<T>
    {
        MemoryStream stream = new MemoryStream();
        msg.WriteTo(stream);
        byte[] bytes = stream.ToArray();

        SendMsgToServer(key, bytes);
    }

    private void SendMsgToServer(string key, byte[] buf)
    {
        uint index = 0;
        uint random = 0;

        // 创建一个Message对象,将消息的key(消息名字)写入buffer
        Message message = new Message(key);

        string serverHost = ServerInfo.ServerHost;
        string serverPort = ServerInfo.ServerPort;
        int port = Convert.ToInt32(serverPort);

        lock (m_LockIndex)
        {
            index = GetBufferIndex();
            random = GetCheck();
        }

        message.MsgIndex = index;

        // CRC校验和
        uint crc = NetCrC32.crc32(0, buf.Length);
        if (buf.Length > 0)
        {
            crc = NetCrC32.crc32(crc, buf, (uint)buf.Length);
        }

        uint rand = random + crc;

        MsgSession msgSession = new MsgSession();
        msgSession.Index = message.MsgIndex;
        msgSession.Check = rand;
        msgSession.Session = m_ClientSession;

        // 将Seesion信息写入buffer
        // Seesion相关信息由'Loginserver'提供(包括随机数种子,token等)
        // Seesion信息也会由'Loginserver'发往其他服务器,故每个消息需要加上MsgSession以用于服务器验证.
        MemoryStream stream = new MemoryStream();
        msgSession.WriteTo(stream);
        byte[] data = stream.ToArray();
        // 写入MsgSession(包括数据长度和数据)
        message.Write(data);

        // 写入已经序列化好的数据(包括数据长度和数据)
        message.Write(buf);

        UdpClient.Instance.SendToServer(serverHost, port, message, true);
    }
}
