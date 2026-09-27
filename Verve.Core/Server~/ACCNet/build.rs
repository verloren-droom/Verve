use std::env;

fn main() {
    // Cargo otherwise embeds the build machine's absolute target/deps path
    // in a macOS cdylib. An rpath name keeps C/Go/Java bindings relocatable.
    if env::var_os("CARGO_CFG_TARGET_OS").as_deref() == Some(std::ffi::OsStr::new("macos")) {
        println!("cargo:rustc-cdylib-link-arg=-Wl,-install_name,@rpath/libverve_acc_net.dylib");
    }
}
