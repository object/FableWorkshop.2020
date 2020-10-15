module Model

type Model = 
    { Filename: string 
      PlaybackDelay : int
      IsPlaying : bool
      Events : string array
      EventIndex : int
      Error : string }
    static member Empty = 
        { Filename = ""
          PlaybackDelay = 1
          IsPlaying = false
          Events = Array.empty
          EventIndex = -1
          Error = "" }
