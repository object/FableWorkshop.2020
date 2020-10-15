module Index

open Elmish
open Fable.SimpleHttp

type Model = { 
    Filename: string 
    Events : string
    Error : string }

type Msg =
    | FilenameChanged of string
    | LoadEvents
    | EventsLoaded of string
    | EventsError of exn

let init() =
    { Filename = ""; Events = ""; Error = "" }, Cmd.none

let loadEvents (state : Model) = 
    async {
        let! response = 
            Http.request (sprintf "http://localhost:8085/%s" state.Filename)
            |> Http.method GET
            |> Http.send
        if response.statusCode <> 200 then failwith (sprintf "Error %d" response.statusCode)
        return response.responseText
    }

let update msg state =
    match msg with
    | FilenameChanged filename ->
        { state with Filename = filename; Error = "" }, Cmd.none 
    | LoadEvents ->
        { state with Events = ""; Error = "" }, Cmd.OfAsync.either loadEvents state EventsLoaded EventsError
    | EventsLoaded events ->
        { state with Events = events }, Cmd.none
    | EventsError exn ->
        { state with Error = exn.ToString() }, Cmd.none

open Fable.React
open Fable.React.Props

let view (state : Model) dispatch =
    div [ Style [ TextAlign TextAlignOptions.Center; Padding 40 ] ] [
        div [] [
            img [ Src "favicon.png" ]
            h1 [] [ str "File Loader" ]
            div [ClassName "control"] [input [OnChange (fun evt -> dispatch (FilenameChanged evt.Value))]]
            button [OnClick (fun _ -> dispatch LoadEvents)] [str "Load"]
            p [] [ str state.Events ]
            p [] [ str state.Error ]
        ]
    ]
