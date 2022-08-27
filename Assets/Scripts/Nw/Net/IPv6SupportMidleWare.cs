using System;
using System.Net.Sockets;
using UnityEngine;

public class IPv6SupportMidleWare
{
    private static string GetIPv6(string host, string port)
    {
        // IPHONE needs "port" param.
        return host + "&&ipv4";
    }

    public static void GetIPType(string serverIp, string serverPorts, out string newServerIp, out AddressFamily ipType)
    {
        ipType = AddressFamily.InterNetwork;
        newServerIp = serverIp;
        try
        {
            string mIPv6 = GetIPv6(serverIp, serverPorts);
            if (!string.IsNullOrEmpty(mIPv6))
            {
                var strTemp = System.Text.RegularExpressions.Regex.Split(mIPv6, "&&");
                if (strTemp != null && strTemp.Length >= 2)
                {
                    var IPType = strTemp[1];
                    if (IPType == "ipv6")
                    {
                        newServerIp = strTemp[0];
                        ipType = AddressFamily.InterNetworkV6;
                    }
                }
            }
        }
        catch (Exception exp)
        {
            Debug.Log("GetIPv6 Error:" + exp.ToString());
        }
    }
}
