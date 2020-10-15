module Model

open Shared

[<RequireQualifiedAccess>]
type EventSet =
  | Small
  | Large

type Model = 
    { EventSet: EventSet 
      PlaybackDelay : int
      IsPlaying : bool
      IsPaused : bool
      Events : Dto.MediaSetEvent list
      SocketConnected : bool
      Error : string }
    static member Empty = 
        { EventSet = EventSet.Small
          PlaybackDelay = 2000
          IsPlaying = false
          IsPaused = false
          Events = List.empty
          SocketConnected = false
          Error = "" }
