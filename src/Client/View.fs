module View

open Feliz

open System
open Model
open Messages

let view (state : Model) dispatch =

    let showPlayOrPauseButton () =
        if state.IsPlaying 
        then Html.button [
            prop.disabled (String.IsNullOrEmpty state.Filename)
            prop.onClick (fun _ -> dispatch PausePlayback)
            prop.text "Pause" ]
        else Html.button [
            prop.disabled (String.IsNullOrEmpty state.Filename)
            prop.onClick (fun _ -> dispatch StartPlayback)
            prop.text "Play" ]

    let showStopButton () =
        Html.button [
            prop.disabled (String.IsNullOrEmpty state.Filename || not state.IsPlaying)
            prop.onClick (fun _ -> dispatch StopPlayback)
            prop.text "Stop" ]

    Html.div [ 
        prop.style [ style.alignContent.center ; style.padding 40 ]
        prop.children [
            Html.div [
                prop.children [
                    Html.img [prop.src "favicon.png"]
                    Html.h1 [prop.text "File Loader"]
                    Html.div [
                        prop.className "control"
                        prop.children [
                            Html.input [
                                prop.onChange (FilenameChanged >> dispatch)
                            ]
                        ]
                    ]
                    Html.div [
                        prop.children [
                            Html.span [ prop.text "Playback speed:" ]
                            Html.span [
                                prop.className "control"
                                prop.children [
                                    Html.input [
                                        prop.value (sprintf "%d" state.PlaybackDelay)
                                        prop.onChange (PlaybackDelayChanged >> dispatch)
                                    ]
                                ]
                            ]
                        ]
                    ]
                    showPlayOrPauseButton ()
                    showStopButton ()
                    Html.p [prop.text (if Array.isEmpty state.Events || state.EventIndex < 0 then "" else state.Events.[state.EventIndex])]
                    Html.p [prop.text state.Error]
                ]
            ]
        ]
    ]
