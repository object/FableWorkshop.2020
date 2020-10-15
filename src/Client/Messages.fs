module Messages

type Msg =
    | FilenameChanged of string
    | PlaybackDelayChanged of string
    | StartPlayback
    | PausePlayback
    | StopPlayback
    | NextEvent
    | EventsLoaded of string
    | EventsError of exn
    | Delayed of Msg * delay:int
