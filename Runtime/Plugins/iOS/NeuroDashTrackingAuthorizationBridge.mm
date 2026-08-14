#import <Foundation/Foundation.h>

#if __has_include(<AppTrackingTransparency/ATTrackingManager.h>)
#import <AppTrackingTransparency/ATTrackingManager.h>
#define NEURODASH_ATT_AVAILABLE 1
#else
#define NEURODASH_ATT_AVAILABLE 0
#endif

extern "C"
{
    int NeuroDashTrackingAuthorizationGetStatus()
    {
#if NEURODASH_ATT_AVAILABLE
        if (@available(iOS 14.0, *))
        {
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        }
#endif
        return 3;
    }

    void NeuroDashTrackingAuthorizationRequest()
    {
#if NEURODASH_ATT_AVAILABLE
        if (@available(iOS 14.0, *))
        {
            if ([ATTrackingManager trackingAuthorizationStatus] != ATTrackingManagerAuthorizationStatusNotDetermined)
            {
                return;
            }

            dispatch_async(dispatch_get_main_queue(), ^{
                [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(__unused ATTrackingManagerAuthorizationStatus status) {
                }];
            });
        }
#endif
    }
}
