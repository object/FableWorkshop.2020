module Messages

open Shared

type Msg =
    | EventSetChanged of string
    | PlaybackDelayChanged of string
    | StartPlayback
    | PausePlayback
    | StopPlayback
    | ConnectSocket
    | MediaSetEvent of Sockets.ClientMessage
    | Delayed of Msg * delay:int
