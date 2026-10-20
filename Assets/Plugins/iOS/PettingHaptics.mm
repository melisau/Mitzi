#import <UIKit/UIKit.h>

extern "C" void MitziPettingLightImpact(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if ([UIApplication sharedApplication].applicationState != UIApplicationStateActive)
            return;
        if (@available(iOS 13.0, *))
        {
            UIImpactFeedbackGenerator *feedback =
                [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleSoft];
            [feedback prepare];
            [feedback impactOccurredWithIntensity:0.25];
#if !__has_feature(objc_arc)
            [feedback release];
#endif
        }
        else if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackGenerator *feedback =
                [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [feedback prepare];
            [feedback impactOccurred];
#if !__has_feature(objc_arc)
            [feedback release];
#endif
        }
    });
}
