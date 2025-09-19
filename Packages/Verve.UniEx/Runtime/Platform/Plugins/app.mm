#pragma comment(linker, "/framework:AppKit")

#import <AppKit/AppKit.h>
#import <Foundation/Foundation.h>
#include <string>

extern "C" {
    void _ShowDialog(const char* title, const char* message, const char* okButton, const char* cancelButton, void (*callback)(bool)) {
        dispatch_async(dispatch_get_main_queue(), ^{
            @autoreleasepool {
                [NSApplication sharedApplication];
                
                NSAlert *alert = [[NSAlert alloc] init];
                
                if (title != nullptr) {
                    [alert setMessageText:[NSString stringWithUTF8String:title]];
                }
                
                if (message != nullptr) {
                    [alert setInformativeText:[NSString stringWithUTF8String:message]];
                }
                
                if (okButton != nullptr) {
                    [alert addButtonWithTitle:[NSString stringWithUTF8String:okButton]];
                } else {
                    [alert addButtonWithTitle:@"OK"];
                }
                
                if (cancelButton != nullptr && strlen(cancelButton) > 0) {
                    [alert addButtonWithTitle:[NSString stringWithUTF8String:cancelButton]];
                }
                
                [alert setAlertStyle:NSAlertStyleWarning];
                
                [alert beginSheetModalForWindow:nil completionHandler:^(NSModalResponse response) {
                    bool result = (response == NSAlertFirstButtonReturn);
                    if (callback != nullptr) {
                        callback(result);
                    }
                }];
            }
        });
    }
}