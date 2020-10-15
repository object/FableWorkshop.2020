module Messages

open Shared

type Msg =
    | EventSetChanged of string
    | PlaybackDelayChanged of string
    | StartPlayback
    | PausePlayback
    | StopPlayback
    | ConnectSocket
    | RemoteEvent of Sockets.ClientMessage
    | RemoveCountdown of string
    | Delayed of Msg * delay:int
