// Server信息
public class ServerInfo
{
    public const string ServerHost = "127.0.0.1"; // 'localhost'不行
    public const string ServerPort = "9703";
}

// 回调
public delegate void MessageHandler(Message msg);
