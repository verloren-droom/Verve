#include <jni.h>
#include "verve_acc_net.h"
#include <vector>
#include <string>
#include <cstring>
// JNI buffers are borrowed for one synchronous ABI call only.
struct Buffer {
 JNIEnv* env; jbyteArray array; jbyte* ptr; jsize size;
 Buffer(JNIEnv* e,jbyteArray a):env(e),array(a),ptr(a?e->GetByteArrayElements(a,nullptr):nullptr),size(a?e->GetArrayLength(a):0){}
 ~Buffer(){if(ptr)env->ReleaseByteArrayElements(array,ptr,JNI_ABORT);}
 AccBytes slice()const{return {reinterpret_cast<uint8_t*>(ptr),size_t(size)};}
};
struct Text {
 JNIEnv* env;jstring value;const char* ptr;
 Text(JNIEnv* e,jstring s):env(e),value(s),ptr(s?e->GetStringUTFChars(s,nullptr):nullptr){}
 ~Text(){if(ptr)env->ReleaseStringUTFChars(value,ptr);}
 AccBytes slice()const{return {reinterpret_cast<const uint8_t*>(ptr),ptr?std::strlen(ptr):0};}
};
static bool check(JNIEnv* env,int status){if(status==0)return true;size_t n=verve_acc_last_error(nullptr,0);std::vector<uint8_t> b(n);verve_acc_last_error(b.data(),n);std::string message(b.begin(),b.end());if(message.empty())message="ACC native error";env->ThrowNew(env->FindClass("java/io/IOException"),message.c_str());return false;}
extern "C" {
JNIEXPORT jlong JNICALL Java_verve_acc_net_AccEndpoint_create(JNIEnv* env,jclass,jboolean server,jstring bind,jbyteArray cert,jbyteArray key,jbyteArray ca){
 Text b(env,bind);Buffer c(env,cert),k(env,key),a(env,ca);if(env->ExceptionCheck())return 0;
 AccConfig config{};config.abi_version=2;config.server=server?1:0;config.bind_address=b.slice();config.certificate_der=c.slice();config.private_key_der=k.slice();config.ca_der=a.slice();config.max_state_bytes=8388608;config.max_frame=65536;config.queue_frames=32;config.max_connections=128;config.frames_per_second=2048;config.bytes_per_second=16777216;config.timeout_ms=10000;
 uint64_t handle=0;check(env,verve_acc_endpoint_create(&config,&handle));return jlong(handle);
}
JNIEXPORT void JNICALL Java_verve_acc_net_AccEndpoint_reload(JNIEnv* env,jclass,jlong endpoint,jstring toml){Text t(env,toml);if(env->ExceptionCheck())return;check(env,verve_acc_endpoint_reload(endpoint,t.slice()));}
JNIEXPORT jint JNICALL Java_verve_acc_net_AccEndpoint_port(JNIEnv* env,jclass,jlong endpoint){uint16_t port=0;check(env,verve_acc_endpoint_port(endpoint,&port));return port;}
JNIEXPORT jlong JNICALL Java_verve_acc_net_AccEndpoint_accept(JNIEnv* env,jclass,jlong endpoint,jint timeout){uint64_t peer=0;int status=verve_acc_endpoint_accept(endpoint,timeout,&peer);if(status==4)return 0;check(env,status);return jlong(peer);}
JNIEXPORT jlong JNICALL Java_verve_acc_net_AccEndpoint_connect(JNIEnv* env,jclass,jlong endpoint,jstring address,jstring name){Text a(env,address),n(env,name);if(env->ExceptionCheck())return 0;uint64_t peer=0;check(env,verve_acc_endpoint_connect(endpoint,a.slice(),n.slice(),&peer));return jlong(peer);}
JNIEXPORT jboolean JNICALL Java_verve_acc_net_AccEndpoint_send(JNIEnv* env,jclass,jlong endpoint,jlong peer,jbyteArray data){Buffer b(env,data);if(env->ExceptionCheck())return false;int status=verve_acc_peer_send(endpoint,peer,b.slice());if(status==2)return false;return check(env,status);}
JNIEXPORT jint JNICALL Java_verve_acc_net_AccEndpoint_receive(JNIEnv* env,jclass,jlong endpoint,jlong peer,jbyteArray output){
 if(!output){env->ThrowNew(env->FindClass("java/lang/IllegalArgumentException"),"null buffer");return 0;}jsize capacity=env->GetArrayLength(output);std::vector<uint8_t> bytes(capacity);size_t length=0;int status=verve_acc_peer_receive(endpoint,peer,bytes.data(),capacity,&length);if(status==1)return 0;if(status==3){env->ThrowNew(env->FindClass("java/lang/IllegalArgumentException"),"receive buffer too small; frame retained");return 0;}if(!check(env,status))return 0;env->SetByteArrayRegion(output,0,jsize(length),reinterpret_cast<jbyte*>(bytes.data()));return jint(length);
}
JNIEXPORT void JNICALL Java_verve_acc_net_AccEndpoint_closePeer(JNIEnv* env,jclass,jlong endpoint,jlong peer){check(env,verve_acc_peer_destroy(endpoint,peer));}
JNIEXPORT void JNICALL Java_verve_acc_net_AccEndpoint_destroy(JNIEnv* env,jclass,jlong endpoint){check(env,verve_acc_endpoint_destroy(endpoint));}
}
