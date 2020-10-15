module Update

open System
open Elmish
open Elmish.Bridge

open Shared
open Model
open Messages

let [<Literal>] MaxRemoteEvents = 25

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

let handleMediaSetEvent msg state =
  let remoteEvents = RemoteEvents.update msg state.RemoteEvents |> List.truncate MaxRemoteEvents
  let mediaSetId, status =
    match msg with
    | Dto.RemoteFileUpdate e -> e.MediaSetId, Dto.MediaSetStatus.Pending
    | Dto.RemoteSubtitlesUpdate e -> e.MediaSetId, Dto.MediaSetStatus.Pending
    | Dto.StatusUpdate e -> e.MediaSetId, MediaSetStatus.fromInt e.Status
  match state.RecentMediaSets |> Map.tryFind mediaSetId with
  | Some mediaSet -> 
    let mediaSet = RecentMediaSet.updateFromRemoteEvent msg mediaSet
    let pendingMediaSets = state.RecentMediaSets |> Map.add mediaSetId mediaSet
    let countdownCmd =
      match msg with
      | Dto.StatusUpdate _ when mediaSet.RemoveCountdown.IsSome -> Cmd.ofMsg (RemoveCountdown mediaSetId)
      | _ -> Cmd.none
    { state with RecentMediaSets = pendingMediaSets; RemoteEvents = remoteEvents }, countdownCmd
  | None ->
    { state with RemoteEvents = remoteEvents }, Cmd.none

let handleRemoveCountdown mediaSetId state =
  match state.RecentMediaSets |> Map.tryFind mediaSetId with
  | Some mediaSet ->
    match mediaSet.RemoveCountdown with
    | Some count ->
      if count = 0 then
        { state with RecentMediaSets = state.RecentMediaSets |> Map.remove mediaSetId }, Cmd.none
      else
        let mediaSet = { mediaSet with RemoveCountdown = Some (count-1) }
        let cmd = Cmd.ofMsg (Delayed (RemoveCountdown mediaSetId, RemoveCountdownIntervalInMilliseconds))
        { state with RecentMediaSets = state.RecentMediaSets |> Map.add mediaSetId mediaSet }, cmd
    | None -> state, Cmd.none
  | None -> state, Cmd.none

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
            { state with IsPlaying = true; IsPaused = false; RemoteEvents = []; RecentMediaSets = Map.empty }, Cmd.none

    | PausePlayback ->
        Bridge.Send (Sockets.PausePlayback)
        { state with IsPaused = true }, Cmd.none

    | StopPlayback ->
        Bridge.Send (Sockets.StopPlayback)
        { state with IsPlaying = false; IsPaused = false }, Cmd.none

    | RemoteEvent msg -> 
        match msg with
        | Dto.Activity.MediaSetEvent evt -> handleMediaSetEvent evt state
        | Dto.Activity.MediaSetState mediaSet ->
            let recentMediaSet = RecentMediaSet.fromMediaSetState mediaSet state.RecentMediaSets
            match RecentMediaSet.getAggregatedState recentMediaSet with
            | Dto.DistributionState.None | Dto.DistributionState.Deleted -> state, Cmd.none
            | _ -> { state with RecentMediaSets = state.RecentMediaSets |> Map.add mediaSet.PersistenceId recentMediaSet }, Cmd.none

    | RemoveCountdown mediaSetId -> handleRemoveCountdown mediaSetId state

    | ConnectSocket -> initSocket state

    | Delayed (msg, delay) -> delayMessage msg delay state
