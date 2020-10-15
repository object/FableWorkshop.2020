module Update

open System
open Elmish
open Fable.SimpleHttp
open Thoth.Json

open Shared
open Model
open Messages

let tryParseWith (tryParseFunc: string -> bool * _) = tryParseFunc >> function
        | true, v    -> Some v
        | false, _   -> None

let loadEvents (state : Model) = 
    async {
        let filename = match state.EventSet with | EventSet.Small -> "SingleProgram.txt" | EventSet.Large -> "MultiplePrograms.txt"
        let! response = 
            Http.request (sprintf "http://localhost:8085%s/%s" Shared.Route.files filename)
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

let selectFileAndSubtitlesEvents activities =
    activities
    |> Seq.map (Decode.Auto.fromString<Dto.Activity>)
    |> Seq.choose (fun x -> match x with Ok (Dto.Activity.MediaSetEvent x) -> Some x | _ -> None)
    |> Seq.choose (fun x -> match x with | Dto.StatusUpdate _ -> None | _ -> Some x)

let update msg state =
    match msg with
    | EventSetChanged eventSet ->
        let eventSet = if eventSet = EventSet.Small.ToString() then EventSet.Small else EventSet.Large
        { state with EventSet = eventSet; Error = "" }, Cmd.none 
    | PlaybackDelayChanged delay ->
        { state with PlaybackDelay = Int32.Parse delay }, Cmd.none
    | StartPlayback ->
        let eventIndex = 
            if state.EventIndex < 0 || state.EventIndex >= state.Events.Length then 0 
            else state.EventIndex
        { state with EventIndex = eventIndex; IsPlaying = true }, Cmd.OfAsync.either loadEvents state EventsLoaded EventsError
    | PausePlayback ->
        { state with IsPlaying = false }, Cmd.none
    | StopPlayback ->
        { state with EventIndex = -1; IsPlaying = false }, Cmd.none
    | NextEvent ->
        if state.IsPlaying then
            if state.EventIndex < state.Events.Length then
                { state with EventIndex = state.EventIndex + 1 }, Cmd.ofMsg (Delayed (NextEvent, state.PlaybackDelay))
            else
                { state with IsPlaying = false }, Cmd.none
        else
            state, Cmd.none
    | EventsLoaded events ->
        let events = 
            events.Split([|'\r'; '\n'|], StringSplitOptions.RemoveEmptyEntries)
            |> selectFileAndSubtitlesEvents
            |> Seq.toArray
        { state with Events = events }, Cmd.ofMsg (Delayed (NextEvent, state.PlaybackDelay))
    | EventsError exn ->
        { state with Error = exn.ToString() }, Cmd.none
    | Delayed (msg, delay) -> delayMessage msg delay state
