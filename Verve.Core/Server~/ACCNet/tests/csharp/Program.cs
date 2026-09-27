using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Verve.Acc.Net;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var cert=File.ReadAllBytes(args[0]);var key=File.ReadAllBytes(args[1]);
        using var server=new AccEndpoint(true,"127.0.0.1:0",cert,key,null);
        using var client=new AccEndpoint(false,"127.0.0.1:0",null,null,cert);
        var accepting=Task.Run(()=>server.Accept());
        using var peer=await Task.Run(()=>client.Connect($"127.0.0.1:{server.Port}","localhost"));
        using var remote=await accepting;
        server.Reload("frames_per_second = 100000");
        client.Reload("frames_per_second = 100000");
        try { server.Reload("frames_per_second = 1\nqueue_frames = 1"); throw new Exception("Fixed limit changed"); }
        catch (IOException) { }
        var packet=new byte[128];
        var buffer=new byte[65536];var response=new byte[65536];
        for(int i=0;i<2000;i++)
        {
            Prepare(packet, (ulong)i + 1);
            BitConverter.GetBytes(i).CopyTo(packet,48);
            if(!peer.TrySend(packet))throw new Exception("Unexpected send backpressure");
            Receive(remote,buffer);
            if(!remote.TrySend(packet))throw new Exception("Unexpected reply backpressure");
            if(Receive(peer,response)!=packet.Length)throw new Exception("Length mismatch");
            for(int j=0;j<packet.Length;j++)if(response[j]!=packet[j])throw new Exception("Payload mismatch");
        }
        server.Dispose();server.Dispose();
        try {remote.Receive(buffer);throw new Exception("Parent lifetime not enforced");}catch(ObjectDisposedException){}
        Console.WriteLine("C# P/Invoke: 2000 ACC v2 echo frames, reload/rejection and child invalidation passed.");
    }
    static void Prepare(byte[] packet, ulong sequence)
    {
        Array.Clear(packet, 0, packet.Length);
        packet[0]=(byte)'V'; packet[1]=(byte)'A'; packet[2]=(byte)'C'; packet[3]=(byte)'C';
        packet[4]=2; packet[5]=sequence == 1 ? (byte)1 : (byte)2;
        WriteUInt64(packet, 8, 7);
        WriteUInt64(packet, 16, sequence);
        WriteUInt64(packet, 24, sequence - 1);
        WriteUInt64(packet, 32, sequence);
        WriteUInt32(packet, 40, 80);
    }
    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        for (var i=0; i<4; i++) bytes[offset+i]=(byte)(value >> (8*i));
    }
    static void WriteUInt64(byte[] bytes, int offset, ulong value)
    {
        for (var i=0; i<8; i++) bytes[offset+i]=(byte)(value >> (8*i));
    }
    static int Receive(AccEndpoint.Peer peer,byte[] buffer)
    {
        var deadline=DateTime.UtcNow.AddSeconds(10);
        for(;;){int count=peer.Receive(buffer);if(count>0)return count;if(DateTime.UtcNow>deadline)throw new TimeoutException();Thread.Yield();}
    }
}
