module SocketServer

open System
open System.IO
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Giraffe
open Elmish
open Elmish.Bridge
open Thoth.Json.Net

open Shared

let getFilesDirectory () =
    Path.Combine [|Directory.GetCurrentDirectory (); "public"|]

type ServerMsg =
  | ClientCommand of Sockets.ClientCommand
  | MessagesLoaded of Dto.Activity list
  | SendMessages
  | MediaSetMessage of Dto.Activity
  | Closed

type ConnectionState =
  | Connected
  | Disconnected

type SessionState = {
    ConnectionState : ConnectionState
    FileName : string
    PlaybackDelay : int
    IsPaused : bool
    Messages : Dto.Activity list
    RemainingMessages : Dto.Activity list
} with static member Empty = { 
        ConnectionState = Disconnected
        FileName = ""
        PlaybackDelay = 0
        IsPaused = false
        Messages = []
        RemainingMessages = [] }

let update clientDispatch (msg : ServerMsg) state =
    match msg with
    | ClientCommand msg ->
        match msg with
        | Sockets.Connect -> 
            { state with ConnectionState = Connected }, Cmd.none
        | Sockets.LoadMessages fileName -> 
            let filePath = Path.Combine [|getFilesDirectory (); fileName|]
            if File.Exists filePath then
                let cmd = task {
                    let! lines = File.ReadAllLinesAsync(filePath)
                    let msgs = 
                        lines
                        |> Seq.filter (not << String.IsNullOrEmpty)
                        |> Seq.map Decode.Auto.fromString<Dto.Activity>
                        |> Seq.choose (fun x -> match x with | Ok x -> Some x | Error _ -> None)
                        |> Seq.toList
                    return MessagesLoaded msgs
                }
                state, Cmd.OfTask.result cmd
            else
                state, Cmd.none
        | Sockets.SetPlaybackDelay delay -> 
                { state with PlaybackDelay = delay }, Cmd.none
        | Sockets.StartPlayback when state.IsPaused -> 
            { state with IsPaused = false }, Cmd.ofMsg SendMessages
        | Sockets.StartPlayback -> 
            { state with RemainingMessages = state.Messages }, Cmd.ofMsg SendMessages
        | Sockets.PausePlayback -> 
            { state with IsPaused = true }, Cmd.none
        | Sockets.StopPlayback -> 
            { state with IsPaused = false; RemainingMessages = [] }, Cmd.none
    | MessagesLoaded msgs -> 
        { state with Messages = msgs; RemainingMessages = [] }, Cmd.none
    | SendMessages when state.IsPaused -> 
        state, Cmd.none
    | SendMessages -> 
            let delay msg = task {
                do! Threading.Tasks.Task.Delay state.PlaybackDelay
                return msg
            }
            let state, cmd =
                match state.RemainingMessages with
                | [] -> state, Cmd.none
                | msg :: msgs -> 
                    { state with RemainingMessages = msgs },
                    Cmd.batch [
                        Cmd.ofMsg (MediaSetMessage msg)
                        Cmd.OfTask.result (delay SendMessages)
                    ]
            state, cmd
    | MediaSetMessage msg ->
        msg |> clientDispatch
        state, Cmd.none
    | Closed -> 
        { state with ConnectionState = Disconnected }, Cmd.none

let init _ () =
    SessionState.Empty, Cmd.none

let socketServer : HttpFunc -> HttpContext -> HttpFuncResult =
    Bridge.mkServer "" init update
    |> Bridge.register ClientCommand
    |> Bridge.register MediaSetMessage
    |> Bridge.whenDown Closed
    |> Bridge.run Giraffe.server
