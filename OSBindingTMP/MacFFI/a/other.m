static void createWindow() {
    NSUInteger windowStyle = NSTitledWindowMask  | NSClosableWindowMask | NSResizableWindowMask | NSMiniaturizableWindowMask;
    
    NSRect screenRect = [[NSScreen mainScreen] frame];
    NSRect viewRect = NSMakeRect(0, 0, 1024, 768);
    NSRect windowRect = NSMakeRect(NSMidX(screenRect) - NSMidX(viewRect),
                                   NSMidY(screenRect) - NSMidY(viewRect),
                                   viewRect.size.width,
                                   viewRect.size.height);
    
    window = [[NSWindow alloc] initWithContentRect:windowRect
                                                    styleMask:windowStyle
                                                      backing:NSBackingStoreBuffered
                                                        defer:NO];
    
    [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
    
    id menubar = [[NSMenu new] autorelease];
    id appMenuItem = [[NSMenuItem new] autorelease];
    [menubar addItem:appMenuItem];
    [NSApp setMainMenu:menubar];
    
    // Then we add the quit item to the menu. Fortunately the action is simple since terminate: is
    // already implemented in NSApplication and the NSApplication is always in the responder chain.
    id appMenu = [[NSMenu new] autorelease];
    id appName = [[NSProcessInfo processInfo] processName];
    id quitTitle = [@"Quit " stringByAppendingString:appName];
    id quitMenuItem = [[[NSMenuItem alloc] initWithTitle:quitTitle
                                                  action:@selector(terminate:) keyEquivalent:@"q"] autorelease];
    [appMenu addItem:quitMenuItem];
    [appMenuItem setSubmenu:appMenu];
    
    NSWindowController * windowController = [[NSWindowController alloc] initWithWindow:window];
    [windowController autorelease];
    
    //View
    view = [[[View alloc] initWithFrame:viewRect] autorelease];
    [window setContentView:view];

    //Window Delegate
    windowDelegate = [[WindowDelegate alloc] init];
    [window setDelegate:windowDelegate];
    
    [window setAcceptsMouseMovedEvents:YES];
    [window setDelegate:view];
    
    // Set app title
    [window setTitle:appName];
    
    // Add fullscreen button
    [window setCollectionBehavior: NSWindowCollectionBehaviorFullScreenPrimary];
    [window makeKeyAndOrderFront:nil];
}

void initApp() {
    [NSApplication sharedApplication];
    
    appDelegate = [[AppDelegate alloc] init];
    [NSApp setDelegate:appDelegate];
    
    running = true;
    
    [NSApp finishLaunching];
}

void frame() {
    @autoreleasepool {
        NSEvent* ev;
        do {
            ev = [NSApp nextEventMatchingMask: NSAnyEventMask
                                    untilDate: nil
                                       inMode: NSDefaultRunLoopMode
                                      dequeue: YES];
            if (ev) {
                // handle events here
                [NSApp sendEvent: ev];
            }
        } while (ev);
    }
}

int main(int argc, const char * argv[])  {
    initApp();
    createWindow();
    while (running) {
        frame();
        RenderWeirdGradient(XOffset, YOffset);
        [view setNeedsDisplay:YES];
        XOffset++;
        YOffset++;
    }
    
    return (0);