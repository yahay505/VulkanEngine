//
//  Window.m
//  MacFFI
//
//  Created by Yavuz Çelik on 20.04.2024.
//

#import <Foundation/Foundation.h>
#import <Cocoa/Cocoa.h>
#import <QuartzCore/CAMetalLayer.h>
#import <Metal/MTLDevice.h>
#import "InputEventStruct.h"


@interface CustomWindow : NSWindow
@end
@implementation CustomWindow
// Empty implementations here so that the window doesn't complain when the events aren't handled or passed on to its view.
// i.e. A traditional Cocoa app expects to pass events on to its view(s).
- (void)keyDown:(NSEvent *)theEvent {}
- (void)keyUp:(NSEvent *)theEvent {}

- (BOOL)acceptsFirstResponder { return YES; }
- (BOOL)canBecomeKeyWindow { return YES; }
- (BOOL)canBecomeMainWindow { return YES; }
@end

@interface MyApp:NSApplication
@end
@implementation MyApp
- (void)run
{
    
}
/*- (void)terminate:(id)sender
{
    
}*/
@end

extern void window_set_title(NSWindow * window, const char* title){
    [window setTitle:[NSString stringWithUTF8String:title]];
}
extern void window_set_size(NSWindow * window, int width, int height){
    [window setContentSize:NSMakeSize(width, height)];
}
extern void window_set_position(NSWindow * window, int x, int y){
    [window setFrameOrigin:NSMakePoint(x, y)];
}
extern void window_set_style(NSWindow * window, int style){
    [window setStyleMask:style];
}
extern NSWindow* open_window(const char* title, int width, int height, int x, int y, int style){
    
    NSWindow* window = [[NSWindow alloc] initWithContentRect:NSMakeRect(x, y, width, height)
                                         styleMask:style 
                                         backing:NSBackingStoreBuffered 
                                         defer:NO];
    [window setTitle:[NSString stringWithUTF8String:title]];
    [window setAcceptsMouseMovedEvents:YES];

    return window;
}
extern CAMetalLayer* window_create_surface(NSWindow * window){
    // Create a Metal layer
    CAMetalLayer *metalLayer = [CAMetalLayer layer];
    metalLayer.device = MTLCreateSystemDefaultDevice();
    metalLayer.pixelFormat = MTLPixelFormatBGRA8Unorm;
    metalLayer.framebufferOnly = YES;
    //get window size
    NSRect frame = [window frame];
    metalLayer.frame = frame;
    
    // Add the Metal layer to the window's content view
    [window.contentView setLayer:metalLayer];
    [window.contentView setWantsLayer:YES];
    return metalLayer;
    }


extern void window_makeKeyAndOrderFront(NSWindow* win){
    [win makeKeyAndOrderFront:nil];
}
extern void start_app(MyApp *application){
    [application run];
}
extern MyApp* create_application(void){
    
        
    
    MyApp* a = [MyApp sharedApplication];
    [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];

    
    [NSApp setPresentationOptions:NSApplicationPresentationDefault];
    [NSApp activateIgnoringOtherApps:YES];

    //AppDelegate* appDelegate = [[AppDelegate alloc] init];
    //[NSApp setDelegate:appDelegate];
    [NSApp run];
    [NSApp finishLaunching];


        

    return a;
}


void pump_messages(void consumer(void*), bool wait_for_message) {
    @autoreleasepool {
        NSEvent* ev;
        do {
            ev = [NSApp nextEventMatchingMask: NSAnyEventMask
                                    untilDate: wait_for_message?NSDate.distantFuture:nil
                                       inMode: NSDefaultRunLoopMode
                                      dequeue: YES];
            wait_for_message=false;
            if (ev) {
                // handle events here
                //NSLog([ev debugDescription]);
                [NSApp sendEvent:ev];
                [NSApp updateWindows];
                struct InputEventStruct data = {(int)ev.type};
                consumer(&data);
                NSLog(@"%i",(int)ev.type);
                switch (ev.type) {
                    case NSEventTypeLeftMouseDragged:
                        NSLog(@"mouse moved");
                        break;
                        
                    default:
                        break;
                }
            }
        } while (ev);
        
    }
}
void fake_consumer(void* a){}
int main(int argc, const char * argv[]) {

    NSApplication* app = create_application();
    NSWindow* win = open_window("asasa", 800, 600, 0, 0, 0);
    window_create_surface(win);
    
    start_app(app);
    [win makeKeyAndOrderFront:nil];

    while(1){
        pump_messages(&fake_consumer,true);
        //NSLog(@"events finished");

        //NSLog(@"new event received");
            
        
    }
    
    return 0;
}

