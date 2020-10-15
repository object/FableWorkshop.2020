module Update

open System
open Elmish
open Elmish.Bridge

open Shared
open Model
open Messages

let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None

let getFilename eventSet =
    match eventSet with 
    | EventSet.Small -> "SingleProgram.txt" 
    | EventSet.Large -> "MultiplePrograms.txt"

let delayMessage msg delay state =
    let delayedMsg (dispatch : Msg -> unit) : unit =
      let delayedDispatch = async {
        do! Async.Sleep delay
        dispatch msg
      }
      Async.StartImmediate delayedDispatch
    state, Cmd.ofSub delayedMsg 

let initSocket state =
  try
    Bridge.Send Sockets.Connect
    let filename = getFilename state.EventSet
    Bridge.Send (Sockets.LoadMessages filename)
    Bridge.Send (Sockets.SetPlaybackDelay state.PlaybackDelay)
    { state with SocketConnected = true }, Cmd.none
  with _ ->
    let delay () = async {
        do! Async.Sleep 1000
    }
    let checkSocket = (fun _ -> ConnectSocket)
    { state with SocketConnected = false }, Cmd.OfAsync.either delay () checkSocket checkSocket

let init () =
    Model.Empty, Cmd.ofMsg ConnectSocket

let update msg state =
    match msg with
    | EventSetChanged eventSet ->
        let eventSet = if eventSet = EventSet.Small.ToString() then EventSet.Small else EventSet.Large
        let filename = getFilename eventSet
        Bridge.Send (Sockets.LoadMessages filename)
        { state with EventSet = eventSet; Error = "" }, Cmd.none 
    | PlaybackDelayChanged delay ->
        let delay = Int32.Parse delay
        Bridge.Send (Sockets.SetPlaybackDelay delay)
        { state with PlaybackDelay = delay }, Cmd.none
    | StartPlayback ->
        Bridge.Send Sockets.StartPlayback
        if state.IsPaused then 
            { state with IsPaused = false }, Cmd.none
        else 
            { state with IsPlaying = true; IsPaused = false; Events = [] }, Cmd.none
    | PausePlayback ->
        Bridge.Send (Sockets.PausePlayback)
        { state with IsPaused = true }, Cmd.none
    | StopPlayback ->
        Bridge.Send (Sockets.StopPlayback)
        { state with IsPlaying = false; IsPaused = false }, Cmd.none
    | MediaSetEvent msg -> 
        match msg with
        | Dto.Activity.MediaSetEvent evt -> 
            match evt with
            | Dto.RemoteFileUpdate _ | Dto.RemoteSubtitlesUpdate _ ->
                { state with Events = evt :: state.Events }, Cmd.none
            | _ ->
                state, Cmd.none
        | _ ->
            state, Cmd.none
    | ConnectSocket -> initSocket state
    | Delayed (msg, delay) -> delayMessage msg delay state
