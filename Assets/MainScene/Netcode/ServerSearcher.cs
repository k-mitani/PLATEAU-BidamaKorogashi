using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public class ServerSearcher : IDisposable
{
    private readonly long clientId;
    private readonly UdpClient udp;
    private IPEndPoint local;
    private readonly IPEndPoint remote;
    private readonly Thread thread;

    public event EventHandler<Data> Receive;

    public ServerSearcher()
    {
        clientId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        var port = 7778;
        local = new IPEndPoint(IPAddress.Any, port);
        remote = new IPEndPoint(IPAddress.Parse("239.239.239.10"), port);
        udp = new UdpClient(local);
        udp.JoinMulticastGroup(remote.Address);
        thread = new Thread(DoLoopReceive);
        thread.Start();
    }

    private void DoLoopReceive()
    {
        while (true)
        {
            try
            {
                var bytes = udp.Receive(ref local);
                var data = Deserialize<Data>(bytes);
                if (data.clientId != clientId)
                {
                    Receive?.Invoke(this, data);
                }
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException) return;
                Debug.LogError(ex.ToString());
                Thread.Sleep(100);
            }
        }
    }

    private readonly byte[] buff = new byte[4096];
    private readonly int dataSize = Marshal.SizeOf<Data>();
    public void Send(Data d)
    {
        if (d.clientId == 0) d.clientId = clientId;
        Serialize(d, buff);
        udp.Send(buff, dataSize, remote);
    }

    public void Dispose()
    {
        thread.Abort();
        udp.Close();
    }


    public static IPAddress[] GetLocalIPAddressList()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();
    }


    public static unsafe T Deserialize<T>(byte[] bytes, int startIndex = 0) where T : struct
    {
        fixed (byte* ptr = &bytes[startIndex])
        {
            return Marshal.PtrToStructure<T>((IntPtr)ptr);
        }
    }

    public static byte[] Serialize<T>(T data, byte[] buffer = null, int startIndex = 0) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        if (buffer == null) buffer = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data, ptr, true);
        Marshal.Copy(ptr, buffer, startIndex, size);
        Marshal.FreeHGlobal(ptr);
        return buffer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Data
    {
        public long clientId;
        public MessageType type;
        public long serverIpv4Address;
        public ushort serverIpv4Port;
    }

    public enum MessageType : byte
    {
        SearchServer,
        SearchServerResponse,
    }
}
