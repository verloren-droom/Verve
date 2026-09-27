import verve.acc.net.AccEndpoint;
import java.nio.file.*;
import java.util.Arrays;
import java.util.concurrent.*;

public class JavaGlueTest {
 public static void main(String[] args)throws Exception {
  byte[] cert=Files.readAllBytes(Paths.get(args[0])),key=Files.readAllBytes(Paths.get(args[1]));
  ExecutorService worker=Executors.newSingleThreadExecutor();
  try(AccEndpoint server=new AccEndpoint(true,"127.0.0.1:0",cert,key,null);AccEndpoint client=new AccEndpoint(false,"127.0.0.1:0",null,null,cert)){
   Future<AccEndpoint.Peer> accept=worker.submit(()->server.accept(10000));
   try(AccEndpoint.Peer a=client.connect("127.0.0.1:"+server.port(),"localhost");AccEndpoint.Peer b=accept.get(10,TimeUnit.SECONDS)){
    server.reload("frames_per_second = 100000");client.reload("frames_per_second = 100000");
    try{server.reload("frames_per_second = 1\nqueue_frames = 1");throw new AssertionError("fixed limit changed");}
    catch(Exception expected){if(!(expected instanceof java.io.IOException))throw expected;}
    byte[] packet=new byte[128],buffer=new byte[128];
    for(int i=0;i<1000;i++){
     prepare(packet,i+1);
     for(int j=0;j<4;j++)packet[48+j]=(byte)(i>>(j*8));
     if(!a.trySend(packet))throw new AssertionError("backpressure");read(b,buffer);if(!Arrays.equals(packet,buffer))throw new AssertionError("receive mismatch");
     if(!b.trySend(buffer))throw new AssertionError("backpressure");read(a,buffer);if(!Arrays.equals(packet,buffer))throw new AssertionError("echo mismatch");
    }
   }
  }finally{worker.shutdownNow();}
  System.out.println("Java JNI: 1000 ACC v2 echo frames, reload/rejection and scoped cleanup passed.");
 }
 static void read(AccEndpoint.Peer peer,byte[] buffer){long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(10);while(peer.receive(buffer)==0){if(System.nanoTime()>end)throw new AssertionError("timeout");Thread.yield();}}
 static void prepare(byte[] packet,long sequence){
  Arrays.fill(packet,(byte)0);packet[0]='V';packet[1]='A';packet[2]='C';packet[3]='C';packet[4]=2;packet[5]=(byte)(sequence==1?1:2);
  write64(packet,8,7);write64(packet,16,sequence);write64(packet,24,sequence-1);write64(packet,32,sequence);write32(packet,40,80);
 }
 static void write32(byte[] bytes,int offset,long value){for(int i=0;i<4;i++)bytes[offset+i]=(byte)(value >>> (8*i));}
 static void write64(byte[] bytes,int offset,long value){for(int i=0;i<8;i++)bytes[offset+i]=(byte)(value >>> (8*i));}
}
