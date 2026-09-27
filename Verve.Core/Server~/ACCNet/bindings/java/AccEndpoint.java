package verve.acc.net;

/** Native QUIC owner. Close this endpoint to release all child connections. */
public final class AccEndpoint implements AutoCloseable {
    static { System.loadLibrary("verve_acc_jni"); }
    private volatile long handle;
    public AccEndpoint(boolean server,String bind,byte[] cert,byte[] key,byte[] ca) {
        handle=create(server,bind,cert,key,ca);
    }
    /** Atomically reload receive rates; immutable changes fail. */
    public void reload(String toml){reload(handle,toml);}
    public int port(){return port(handle);}
    /** Blocking accept; returns null on timeout. */
    public Peer accept(int timeoutMs){long id=accept(handle,timeoutMs);return id==0?null:new Peer(id);}
    public Peer connect(String address,String name){return new Peer(connect(handle,address,name));}
    @Override public synchronized void close(){if(handle!=0){long id=handle;handle=0;destroy(id);}}
    public final class Peer implements AutoCloseable {
        private long id;
        private Peer(long id){this.id=id;}
        public boolean trySend(byte[] packet){return send(handle,id,packet);}
        public int receive(byte[] buffer){return AccEndpoint.receive(handle,id,buffer);}
        @Override public synchronized void close(){if(id!=0){long old=id;id=0;if(handle!=0)closePeer(handle,old);}}
    }
    private static native long create(boolean server,String bind,byte[] cert,byte[] key,byte[] ca);
    private static native void reload(long endpoint,String toml);
    private static native int port(long endpoint);
    private static native long accept(long endpoint,int timeout);
    private static native long connect(long endpoint,String address,String name);
    private static native boolean send(long endpoint,long peer,byte[] data);
    private static native int receive(long endpoint,long peer,byte[] buffer);
    private static native void closePeer(long endpoint,long peer);
    private static native void destroy(long endpoint);
}
