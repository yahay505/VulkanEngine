//
//  EventParser.m
//  a
//
//  Created by Yavuz Çelik on 2.05.2024.
//

#import <Foundation/Foundation.h>
#import "eventParser.h"

static struct mouse_event make_mouse_event(NSEvent *ev,int action) {
    struct mouse_event me={};
    me.local_x = ev.locationInWindow.x;
    me.local_y = ev.locationInWindow.y;
    me.global_x = NSEvent.mouseLocation.x;
    me.global_y = NSEvent.mouseLocation.y;
    me.button_refered = ev.buttonNumber;
    me.button_action = action;
    me.button_state = NSEvent.pressedMouseButtons;
    return me;
}

static struct keyboard_event make_keyboard_event(NSEvent *ev, int action) {
    struct keyboard_event ke = {};
    ke.action = action;
    ke.qwerty_key = ev.keyCode;
    ke.flags = (uint32_t)ev.modifierFlags;
    ke.translated_key = ev.characters.UTF8String;
    ke.translated_unmodified_key = ev.charactersIgnoringModifiers.UTF8String;
    return ke;
}

struct InputEventStruct parse_event(NSEvent *ev){
    struct InputEventStruct a={};
    a.internal_type=(int32_t)ev.type;
    switch (ev.type){
            
        case NSEventTypeLeftMouseDown:
            {
                a.type = MOUSE_EVENT;
                struct mouse_event me = make_mouse_event(ev,DOWN);
                a.data.mouse = me;
            }
            break;
        case NSEventTypeLeftMouseUp:
            {
                a.type = MOUSE_EVENT;
                struct mouse_event me = make_mouse_event(ev,UP);
                a.data.mouse = me;
            }
            break;
        case NSEventTypeRightMouseDown:
            {
                a.type = MOUSE_EVENT;
                struct mouse_event me = make_mouse_event(ev,DOWN);
                a.data.mouse = me;
            }
            break;
        case NSEventTypeRightMouseUp:
            {
                a.type = MOUSE_EVENT;
                struct mouse_event me = make_mouse_event(ev,UP);
                a.data.mouse = me;
            }
            break;
        case NSEventTypeMouseMoved:
            {
                a.type = MOUSE_EVENT;
                struct mouse_event me = make_mouse_event(ev,MOVE);
                a.data.mouse = me;
            }
            break;
        case NSEventTypeLeftMouseDragged:
            {
                a.type = MOUSE_EVENT;
                struct mouse_event me = make_mouse_event(ev,HOLD);
                a.data.mouse = me;
            }
            break;
        case NSEventTypeRightMouseDragged:
        {
            a.type = MOUSE_EVENT;
            struct mouse_event me = make_mouse_event(ev,HOLD);
            a.data.mouse = me;
        }
            break;
        case NSEventTypeMouseEntered:
        {
            a.type = MOUSE_EVENT;
            struct mouse_event me = make_mouse_event(ev,ENTER);
            a.data.mouse = me;
        }
            break;
        case NSEventTypeMouseExited:
        {
            a.type = MOUSE_EVENT;
            struct mouse_event me = make_mouse_event(ev,EXIT);
            a.data.mouse = me;
        }
            break;
        case NSEventTypeKeyDown:
        {
            a.type = KEYBOARD_EVENT;
            struct keyboard_event ke = make_keyboard_event(ev,DOWN);
            a.data.keyboard = ke;
        }
            break;
        case NSEventTypeKeyUp:
            {
                a.type = KEYBOARD_EVENT;
                struct keyboard_event ke = make_keyboard_event(ev,UP);
                a.data.keyboard = ke;
            }
            break;
        case NSEventTypeFlagsChanged:
        {
            a.type = UNKNOWN_EVENT;
            struct unknown_event unk={};
            a.data.unkown=unk;
        }
            break;
        case NSEventTypeAppKitDefined:
            break;
        case NSEventTypeSystemDefined:
            break;
        case NSEventTypeApplicationDefined:

            break;
        case NSEventTypePeriodic:

            break;
        case NSEventTypeCursorUpdate:

            break;
        case NSEventTypeScrollWheel:

            break;
        case NSEventTypeTabletPoint:

            break;
        case NSEventTypeTabletProximity:

            break;
        case NSEventTypeOtherMouseDown:
        {
            a.type = MOUSE_EVENT;
            struct mouse_event me = make_mouse_event(ev,DOWN);
            a.data.mouse = me;
        }
            break;
        case NSEventTypeOtherMouseUp:
        {
            a.type = MOUSE_EVENT;
            struct mouse_event me = make_mouse_event(ev,UP);
            a.data.mouse = me;
        }
            break;
        case NSEventTypeOtherMouseDragged:
        {
            a.type = MOUSE_EVENT;
            struct mouse_event me = make_mouse_event(ev,HOLD);
            a.data.mouse = me;
        }
            break;
        case NSEventTypeGesture:

            break;
        case NSEventTypeMagnify:

            break;
        case NSEventTypeSwipe:

            break;
        case NSEventTypeRotate:

            break;
        case NSEventTypeBeginGesture:

            break;
        case NSEventTypeEndGesture:

            break;
        case NSEventTypeSmartMagnify:

            break;
        case NSEventTypeQuickLook:

            break;
        case NSEventTypePressure:

            break;
        case NSEventTypeDirectTouch:

            break;
        case NSEventTypeChangeMode:

            break;
    }
    return a;
}
