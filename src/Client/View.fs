module View

open System
open Model
open Messages

open Fable.React
open Fable.React.Props

let view (state : Model) dispatch =
    div [ Style [ TextAlign TextAlignOptions.Center; Padding 40 ] ] [
        div [] [
            img [ Src "favicon.png" ]
            h1 [] [ str "File Player" ]
            div [ClassName "control"] [input [OnChange (fun evt -> dispatch (FilenameChanged evt.Value))]]
            div [] [
                span [] [ str "Playback delay:" ]
                span [ClassName "control"] [input [Value (sprintf "%d" state.PlaybackDelay); OnChange (fun evt -> dispatch (PlaybackDelayChanged evt.Value))]]
            ]
            if state.IsPlaying 
            then button [Disabled (String.IsNullOrEmpty state.Filename); OnClick (fun _ -> dispatch PausePlayback)] [str "Pause"]
            else button [Disabled (String.IsNullOrEmpty state.Filename); OnClick (fun _ -> dispatch StartPlayback)] [str "Play"]
            button [Disabled (String.IsNullOrEmpty state.Filename || not state.IsPlaying); OnClick (fun _ -> dispatch StopPlayback)] [str "Stop"]
            p [] [ str <| if Array.isEmpty state.Events || state.EventIndex < 0 then "" else state.Events.[state.EventIndex] ]
            p [] [ str state.Error ]
        ]
    ]
