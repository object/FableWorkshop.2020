module Update

open System
open Elmish
open Fable.SimpleHttp

open Model
open Messages

let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None

let loadEvents (state : Model) = 
    async {
        let! response = 
            Http.request (sprintf "http://localhost:8085/%s" state.Filename)
            |> Http.method GET
            |> Http.send
        if response.statusCode <> 200 then failwith (sprintf "Error %d" response.statusCode)
        return response.responseText
    }

let delayMessage msg delay state =
    let delayedMsg (dispatch : Msg -> unit) : unit =
      let delayedDispatch = async {
        do! Async.Sleep delay
        dispatch msg
      }
      Async.StartImmediate delayedDispatch
    state, Cmd.ofSub delayedMsg

let init () =
    Model.Empty, Cmd.none

let update msg state =
    match msg with
    | FilenameChanged filename ->
        { state with Filename = filename; Error = "" }, Cmd.none 
    | PlaybackDelayChanged delay ->
        { state with PlaybackDelay = tryParseWith System.Int32.TryParse delay |> Option.defaultValue state.PlaybackDelay }, Cmd.none 
    | StartPlayback ->
        { state with EventIndex = (if state.EventIndex < 0 then 0 else state.EventIndex); IsPlaying = true }, Cmd.OfAsync.either loadEvents state EventsLoaded EventsError
    | PausePlayback ->
        { state with IsPlaying = false }, Cmd.none
    | StopPlayback ->
        { state with EventIndex = -1; IsPlaying = false }, Cmd.none
    | NextEvent ->
        if state.IsPlaying then
            if state.EventIndex < state.Events.Length - 1 then
                { state with EventIndex = state.EventIndex + 1 }, Cmd.ofMsg (Delayed (NextEvent, state.PlaybackDelay * 1000))
            else
                { state with EventIndex = -1; IsPlaying = false }, Cmd.none
        else
            state, Cmd.none
    | EventsLoaded events ->
        { state with Events = events.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries) }, Cmd.ofMsg (Delayed (NextEvent, state.PlaybackDelay * 1000))
    | EventsError exn ->
        { state with Error = exn.ToString() }, Cmd.none
    | Delayed (msg, delay) -> delayMessage msg delay state
