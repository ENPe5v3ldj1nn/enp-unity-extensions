#import <UIKit/UIKit.h>

// Native counterpart for ENP.UnityExtensions.Runtime.VibrationController's iOS haptic path.
// UIImpactFeedbackGenerator is the Taptic Engine API - unlike AudioServicesPlaySystemSound's
// fixed-strength system vibration, impactOccurredWithIntensity actually honors intensity01.
extern "C"
{
    void EnpHapticImpactOccurred(float intensity01)
    {
        if (@available(iOS 13.0, *))
        {
            UIImpactFeedbackGenerator *generator =
                [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            [generator prepare];
            [generator impactOccurredWithIntensity:(CGFloat)intensity01];
        }
        else
        {
            UIImpactFeedbackGenerator *generator =
                [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            [generator prepare];
            [generator impactOccurred];
        }
    }
}
