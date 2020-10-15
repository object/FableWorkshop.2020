module App

open Elmish
open Elmish.React
open Elmish.Bridge

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

open Update
open View

Program.mkProgram init update view
|> Program.withBridgeConfig (
    Bridge.endpoint "ws://localhost:8085/socket"     
    |> Bridge.withUrlMode Raw
    |> Bridge.withMapping (fun (x : Shared.Sockets.ClientMessage) -> x |> Messages.MediaSetEvent))
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
