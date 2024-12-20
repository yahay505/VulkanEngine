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
#import "eventParser.h"

void (*_consumer)(void*);

@interface CustomWindow : NSWindow
@end
@implementation CustomWindow
// Empty implementations here so that the window doesn't complain when the events aren't handled or passed on to its view.
// i.e. A traditional Cocoa app expects to pass events on to its view(s).
//- (void)keyDown:(NSEvent *)theEvent {}
//- (void)keyUp:(NSEvent *)theEvent {}

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
@interface AppDelegate:NSObject <NSApplicationDelegate,NSWindowDelegate>
@property NSWindow *window;
@end
@implementation AppDelegate

-(NSSize)windowWillResize:(NSWindow *)sender toSize:(NSSize)frameSize{
    // Update the content view and Metal layer's frame
      CGRect newFrame = CGRectMake(0, 0, frameSize.width, frameSize.height);
      [self.window.contentView setFrame:newFrame];

      CAMetalLayer *metalLayer = (CAMetalLayer *)self.window.contentView.layer;
      if (metalLayer) {
          metalLayer.frame = newFrame;
          metalLayer.drawableSize = CGSizeMake(frameSize.width * self.window.backingScaleFactor,
                                               frameSize.height * self.window.backingScaleFactor);
      }
    resize_event_data_t a ={frameSize.width* self.window.backingScaleFactor,frameSize.height* self.window.backingScaleFactor};
    struct window_event b={(size_t)(__bridge void*)self.window,RESIZE,&a};
    struct InputEventStruct ev = {WINDOW_EVENT,0};
    ev.data.window=b;
    _consumer(&ev);
    return frameSize;
}
-(void)windowWillClose:(NSNotification *)notification{
    
}

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
    AppDelegate *del = [[AppDelegate alloc] init];
    del.window=window;
    [window setDelegate:del];

    return window;
}
extern void* window_create_surface(NSWindow * window){
    // Create a Metal layer
    CAMetalLayer *metalLayer = [CAMetalLayer layer];
    metalLayer.device = MTLCreateSystemDefaultDevice();
    metalLayer.pixelFormat = MTLPixelFormatBGRA8Unorm;
    metalLayer.framebufferOnly = YES;
    metalLayer.opaque = NO;
    metalLayer.autoresizingMask = kCALayerWidthSizable | kCALayerHeightSizable;
    metalLayer.contentsScale = [NSScreen mainScreen].backingScaleFactor;

    // Attach the Metal layer to the window
    window.contentView.wantsLayer = YES;
    window.contentView.layer = metalLayer;

    return (__bridge void*)metalLayer;
    }


extern void window_makeKeyAndOrderFront(NSWindow* win){
    [win makeKeyAndOrderFront:nil];
}
extern void start_app(MyApp *application){
    [application run];
}
extern void set_transparent(NSWindow* win,int32_t state){
    [win setOpaque:!state];
    win.hasShadow = false;
    win.titlebarAppearsTransparent = true;
    win.backgroundColor = NSColor.clearColor;
    
    
}
extern MyApp* create_application(void){
    
        
    
    MyApp* a = [MyApp sharedApplication];
    [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
    
    [NSApp setPresentationOptions:NSApplicationPresentationDefault];
    [NSApp activateIgnoringOtherApps:YES];

    AppDelegate* appDelegate = [[AppDelegate alloc] init];
    [NSApp setDelegate:appDelegate];
    [NSApp run];
    [NSApp finishLaunching];


        

    return a;
}


void pump_messages(void (*consumer)(void*), bool wait_for_message) {
    @autoreleasepool {
        NSEvent* ev;
        do {
            ev = [NSApp nextEventMatchingMask: NSEventMaskAny
                                    untilDate: wait_for_message?NSDate.distantFuture:nil
                                       inMode: NSDefaultRunLoopMode
                                      dequeue: YES];
            wait_for_message=false;
            if (ev) {
                // handle events here
                //NSLog([ev debugDescription]);
                _consumer=consumer;
                [NSApp sendEvent:ev];
                [NSApp updateWindows];
                struct InputEventStruct data = parse_event(ev);
                consumer(&data);
                //NSLog(@"%i",(int)ev.type);
                switch (ev.type) {
                    case NSEventTypeLeftMouseDragged:
                        //NSLog(@"mouse moved");
                        break;
                        
                    default:
                        break;
                }
            }
        } while (ev);
        
    }
}
void fake_consumer(void* a){
    
    NSLog(@"%i",((struct InputEventStruct*)a)->type);

}
int main(int argc, const char * argv[]) {

    MyApp* app = create_application();
    NSWindow* win = open_window("asasa", 800, 800, 0,0,NSWindowStyleMaskTitled|NSWindowStyleMaskResizable|NSWindowStyleMaskClosable);
    window_create_surface(win);
    
    start_app(app);
    window_makeKeyAndOrderFront(win);
    while(1){
        pump_messages(&fake_consumer,true);
        //NSLog(@"events finished");

        //NSLog(@"new event received");
            
        
    }
    
    return 0;
}

