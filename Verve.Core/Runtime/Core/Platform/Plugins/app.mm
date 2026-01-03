#include <CoreFoundation/CoreFoundation.h>
#include <ApplicationServices/ApplicationServices.h>

extern "C" {
    int _ShowDialog(const char* title, const char* message, const char* defaultButton, const char* alternateButton) {
        const char* safeTitle = (title != NULL) ? title : "Dialog";
        const char* safeMessage = (message != NULL) ? message : "";
        const char* safeDefaultButton = (defaultButton != NULL) ? defaultButton : "OK";
        const char* safeAlternateButton = (alternateButton != NULL) ? alternateButton : NULL;
        
        CFStringRef titleStr = NULL;
        CFStringRef messageStr = NULL;
        CFStringRef defaultButtonStr = NULL;
        CFStringRef alternateButtonStr = NULL;
        
        titleStr = CFStringCreateWithCString(kCFAllocatorDefault, safeTitle, kCFStringEncodingUTF8);
        messageStr = CFStringCreateWithCString(kCFAllocatorDefault, safeMessage, kCFStringEncodingUTF8);
        defaultButtonStr = CFStringCreateWithCString(kCFAllocatorDefault, safeDefaultButton, kCFStringEncodingUTF8);
        
        if (safeAlternateButton != NULL) {
            alternateButtonStr = CFStringCreateWithCString(kCFAllocatorDefault, safeAlternateButton, kCFStringEncodingUTF8);
        }
        
        CFOptionFlags response = 0;
        CFOptionFlags result = CFUserNotificationDisplayAlert(
            0,
            kCFUserNotificationNoteAlertLevel,
            NULL,
            NULL,
            NULL,
            titleStr,
            messageStr,
            defaultButtonStr,
            alternateButtonStr,
            NULL,
            &response
        );
        
        if (titleStr) CFRelease(titleStr);
        if (messageStr) CFRelease(messageStr);
        if (defaultButtonStr) CFRelease(defaultButtonStr);
        if (alternateButtonStr) CFRelease(alternateButtonStr);
        
        if (result != 0) {
            return -1;
        }
        
        return (response == kCFUserNotificationDefaultResponse) ? 0 : 1;
    }
}