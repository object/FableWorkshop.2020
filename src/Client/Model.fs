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
      Events : Dto.MediaSetEvent array
      EventIndex : int
      Error : string }
    static member Empty = 
        { EventSet = EventSet.Small
          PlaybackDelay = 2000
          IsPlaying = false
          Events = Array.empty
          EventIndex = -1
          Error = "" }
