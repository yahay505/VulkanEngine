//
//  InputEventStruct.h
//  a
//
//  Created by Yavuz Çelik on 27.04.2024.
//

#ifndef InputEventStruct_h
#define InputEventStruct_h
#define KEYBOARD_EVENT 1
struct keyboard_event{
    const void* translated_key;
    const void* translated_unmodified_key;
    uint16 qwerty_key;
    uint32_t flags;
    int32_t action;
};
#define MOUSE_EVENT 2
struct mouse_event{
    int32_t local_x;
    int32_t local_y;
    int32_t global_x;
    int32_t global_y;
    uint64_t button_state;
    int32_t button_action;
    int64_t button_refered;
    //int32_t state;
};
#define DOWN 0
#define HOLD 1
#define UP 2
#define MOVE 3
#define ENTER 4
#define EXIT 5

#define WINDOW_EVENT 4
struct window_event{
    int64_t windowID;
    int32_t event_type;
    void* data;
};
#define RESIZE 1
typedef struct{
    uint32_t w,h;
} resize_event_data_t;

#define SCROLL_EVENT 3
struct scroll_event{
    
};
#define UNKNOWN_EVENT -1
struct unknown_event{};

struct InputEventStruct {
    int32_t type;
    int32_t internal_type;
    union data{
        struct keyboard_event keyboard;
        struct mouse_event mouse;
        struct scroll_event scroll;
        struct window_event window;
        struct unknown_event unkown;
    } data;
};
;

#endif /* InputEventStruct_h */
