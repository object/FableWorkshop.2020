module View

open Feliz
open Feliz.Bulma

open System
open Model
open Messages

let view (state : Model) dispatch =

    let showTitle () =
        Html.h1 [
            prop.className [Bulma.IsSize2; Bulma.HasTextWeightBold]
            prop.text "File Player"
        ]

    let showEventSets () =
        Html.div [
            prop.className [Bulma.Field]
            prop.children [
                Html.div [
                    prop.className [Bulma.Control]
                    prop.children [
                        Html.div [
                            prop.className [Bulma.Select]
                            prop.children [
                                Html.select [
                                    prop.children [
                                        Html.option [
                                            prop.value (EventSet.Small.ToString())
                                            prop.text "Single program"
                                        ]
                                        Html.option [
                                            prop.value (EventSet.Large.ToString())
                                            prop.text "Multiple programs"
                                        ]
                                    ]
                                    prop.onChange (EventSetChanged >> dispatch)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let showPlaybackSpeeds () =
        Html.div [
            prop.className [Bulma.Field]
            prop.children [
                Html.div [
                    prop.className [Bulma.Control]
                    prop.children [
                        Html.div [
                            prop.className [Bulma.Select]
                            prop.children [
                                Html.select [
                                    prop.children [
                                        Html.option [
                                            prop.value "2000"
                                            prop.text "Slow playback"
                                        ]
                                        Html.option [
                                            prop.value "500"
                                            prop.text "Fast playback"
                                        ]
                                    ]
                                    prop.onChange (PlaybackDelayChanged >> dispatch)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let showPlayOrPauseButton () =
        if state.IsPlaying 
        then Html.button [
            prop.className [Bulma.Button; Bulma.IsPrimary]
            prop.onClick (fun _ -> dispatch PausePlayback)
            prop.children [
                Html.i [prop.className [FA.Fa; FA.FaPauseCircle] ]
            ] ]
        else Html.button [
            prop.className [Bulma.Button; Bulma.IsSuccess]
            prop.onClick (fun _ -> dispatch StartPlayback)
            prop.children [
                Html.i [prop.className [FA.Fa; FA.FaPlayCircle] ]
            ] ]

    let showStopButton () =
        Html.button [
            prop.className [Bulma.Button; Bulma.IsDanger]
            prop.disabled (not state.IsPlaying)
            prop.onClick (fun _ -> dispatch StopPlayback)
            prop.children [
                Html.i [prop.className [FA.Fa; FA.FaStopCircle] ]
            ]
        ]

    let showEvents () =
        Bulma.table [
            prop.children [
                Html.tbody (
                    state.Events
                    |> Array.truncate (Math.Max (state.EventIndex, 0))
                    |> Seq.map (fun e -> Html.tr [Html.td [Html.text e]])
                )
            ]
        ]

    let showError () =
        Html.p [prop.text state.Error]

    Bulma.column [
        prop.children [
            Bulma.column [
                Html.div [
                    prop.children [
                        showTitle ()
                        showEventSets ()
                        showPlaybackSpeeds ()
                        showPlayOrPauseButton ()
                        showStopButton ()
                    ]
                ]
            ]
            Bulma.column [
                Html.div [
                    prop.children [
                        showEvents ()
                        showError ()
                    ]
                ]
            ]
        ]
    ]
