// Test-only minimal server. Business hosts supply their own routing and authorization.
#include "verve_acc_net.h"
#include <algorithm>
#include <chrono>
#include <fstream>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>
static AccBytes bytes(const std::vector<uint8_t>& v){return {v.data(),v.size()};}
static AccBytes text(const std::string& s){return {reinterpret_cast<const uint8_t*>(s.data()),s.size()};}
static void write32(std::vector<uint8_t>& bytes,size_t offset,uint32_t value){for(size_t i=0;i<4;++i)bytes[offset+i]=uint8_t(value>>(8*i));}
static void write64(std::vector<uint8_t>& bytes,size_t offset,uint64_t value){for(size_t i=0;i<8;++i)bytes[offset+i]=uint8_t(value>>(8*i));}
static void prepare(std::vector<uint8_t>& packet,uint64_t sequence){
 std::fill(packet.begin(),packet.end(),0);packet[0]='V';packet[1]='A';packet[2]='C';packet[3]='C';packet[4]=2;packet[5]=sequence==1?1:2;
 write64(packet,8,7);write64(packet,16,sequence);write64(packet,24,sequence-1);write64(packet,32,sequence);write32(packet,40,80);
}
static void check(int status){if(status==0)return;std::vector<uint8_t> msg(verve_acc_last_error(nullptr,0));verve_acc_last_error(msg.data(),msg.size());throw std::runtime_error(std::string(msg.begin(),msg.end()));}
static std::vector<uint8_t> file(const char* name){std::ifstream in(name,std::ios::binary);if(!in)throw std::runtime_error("missing certificate");return {std::istreambuf_iterator<char>(in),{}};}
int main(int argc,char** argv){
 try {
  if(argc!=3)throw std::runtime_error("glue certificate.der private-key.der");
  auto cert=file(argv[1]),key=file(argv[2]);std::string bind="127.0.0.1:0",name="localhost";
  AccConfig config{};config.abi_version=2;config.server=1;config.bind_address=text(bind);config.certificate_der=bytes(cert);config.private_key_der=bytes(key);config.max_state_bytes=8388608;
  config.max_frame=65536;config.queue_frames=32;config.max_connections=16;config.frames_per_second=100000;config.bytes_per_second=1073741824;config.timeout_ms=10000;
  uint64_t server=0;check(verve_acc_endpoint_create(&config,&server));uint16_t port;check(verve_acc_endpoint_port(server,&port));
  check(verve_acc_endpoint_reload(server,text("max_connections = 16\nframes_per_second = 100000\nbytes_per_second = 1073741824")));
  if(verve_acc_endpoint_reload(server,text("max_connections = 16\nframes_per_second = 1\nqueue_frames = 1"))!=-1)throw std::runtime_error("fixed limit changed");
  std::exception_ptr failure;
  std::thread worker([&]{try{
   uint64_t peer;check(verve_acc_endpoint_accept(server,10000,&peer));std::vector<uint8_t> frame(65536);
   for(int n=0;n<10000;++n){size_t length=0;int status;auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(10);
    do{status=verve_acc_peer_receive(server,peer,frame.data(),frame.size(),&length);if(status==1){if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("echo timeout");std::this_thread::yield();}}while(status==1);check(status);
    do{status=verve_acc_peer_send(server,peer,{frame.data(),length});if(status==2)std::this_thread::yield();}while(status==2);check(status);
   }
   // Client closes first, ensuring queued final response is delivered.
   while(verve_acc_peer_status(server,peer)==0)std::this_thread::yield();
   check(verve_acc_peer_destroy(server,peer));
  }catch(...){failure=std::current_exception();}});
  config.server=0;config.certificate_der={};config.private_key_der={};config.ca_der=bytes(cert);
  uint64_t client,peer;check(verve_acc_endpoint_create(&config,&client));std::string address="127.0.0.1:"+std::to_string(port);
  check(verve_acc_endpoint_connect(client,text(address),text(name),&peer));
  std::vector<uint8_t> packet(128),received(128);
  auto started=std::chrono::steady_clock::now();
  for(uint32_t i=0;i<10000;++i){prepare(packet,uint64_t(i)+1);for(int j=0;j<4;++j)packet[48+j]=uint8_t(i>>(j*8));check(verve_acc_peer_send(client,peer,bytes(packet)));
   size_t length=0;int status;auto deadline=std::chrono::steady_clock::now()+std::chrono::seconds(10);
   // Probe a deliberately short buffer: frame must remain queued.
   do{status=verve_acc_peer_receive(client,peer,nullptr,0,&length);if(status==1){if(std::chrono::steady_clock::now()>deadline)throw std::runtime_error("client timeout");std::this_thread::yield();}}while(status==1);
   if(status!=3||length!=packet.size())throw std::runtime_error("small-buffer contract failed");
   check(verve_acc_peer_receive(client,peer,received.data(),received.size(),&length));if(received!=packet)throw std::runtime_error("echo mismatch");
  }
  check(verve_acc_endpoint_destroy(client));worker.join();if(failure)std::rethrow_exception(failure);
  if(verve_acc_peer_status(client,peer)!=-1||verve_acc_endpoint_destroy(client)!=-1)throw std::runtime_error("stale handle accepted");
  check(verve_acc_endpoint_destroy(server));
  std::cout<<"C++ DLL echo: 10000 frames, short-buffer retention, parent cleanup, stale handles passed in "<<std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now()-started).count()<<" ms\n";
  return 0;
 }catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}
}
