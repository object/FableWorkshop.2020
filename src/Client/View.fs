module View

open Feliz
open Feliz.Bulma

open Shared
open Model
open Messages

let [<Literal>] TimeFormatString = "HH:mm:ss"

let view (state : Model) dispatch =

    let showTitle () =
        Html.h1 [
            prop.className [Bulma.IsSize2; Bulma.HasTextWeightBold]
            prop.text "Events Player"
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
        if not state.IsPlaying || state.IsPaused 
        then Html.button [
            prop.className [Bulma.Button; Bulma.IsSuccess]
            prop.onClick (fun _ -> dispatch StartPlayback)
            prop.children [
                Html.i [prop.className [FA.Fa; FA.FaPlayCircle] ]
            ] ]
        else Html.button [
            prop.className [Bulma.Button; Bulma.IsPrimary]
            prop.onClick (fun _ -> dispatch PausePlayback)
            prop.children [
                Html.i [prop.className [FA.Fa; FA.FaPauseCircle] ]
            ] ]

    let showStopButton () =
        Html.button [
            prop.className [Bulma.Button; Bulma.IsDanger]
            prop.onClick (fun _ -> dispatch StopPlayback)
            prop.children [
                Html.i [prop.className [FA.Fa; FA.FaStopCircle] ]
            ]
        ]

    let showEvent e =

        let getId (mediaSetId : string) =
            mediaSetId.Split('~') |> Seq.last

        let mediaSetId,state,provider,quality,timestamp =
            match e with
            | Dto.RemoteFileUpdate e -> getId e.MediaSetId, e.RemoteState.State,e.StorageProvider,e.FileRef.QualityId.ToString(),e.RemoteState.Timestamp
            | Dto.RemoteSubtitlesUpdate e -> getId e.MediaSetId, e.RemoteState.State,e.StorageProvider,"TEXT",e.RemoteState.Timestamp
            | _ -> failwithf "Unsupported event type %A" e

        let color =
            match state with
            | Dto.DistributionState.None -> Bulma.IsDark
            | Dto.DistributionState.Requested -> Bulma.IsPrimary
            | Dto.DistributionState.Initiated -> Bulma.IsLink
            | Dto.DistributionState.Ingesting -> Bulma.IsInfo
            | Dto.DistributionState.Segmenting -> Bulma.IsInfo
            | Dto.DistributionState.Completed -> Bulma.IsSuccess
            | Dto.DistributionState.Deleted -> Bulma.IsSuccess
            | Dto.DistributionState.Cancelled -> Bulma.IsDanger
            | Dto.DistributionState.Failed -> Bulma.IsWarning
            | Dto.DistributionState.Rejected -> Bulma.IsDanger

        let icon =
            match state with
            | Dto.DistributionState.None -> FA.FaQuestionCircle
            | Dto.DistributionState.Requested -> FA.FaEllipsisH
            | Dto.DistributionState.Initiated -> FA.FaUpload
            | Dto.DistributionState.Ingesting -> FA.FaCloudUpload
            | Dto.DistributionState.Segmenting -> FA.FaCloudUpload
            | Dto.DistributionState.Completed -> FA.FaCheckCircle
            | Dto.DistributionState.Deleted -> FA.FaTrash
            | Dto.DistributionState.Cancelled -> FA.FaExclamationCircle
            | Dto.DistributionState.Failed -> FA.FaExclamationTriangle
            | Dto.DistributionState.Rejected -> FA.FaExclamationCircle
            |> fun icon ->
                Html.i [
                    prop.className [
                        FA.Fa
                        icon
                    ]
                ]

        Html.tr [
            Html.td [prop.className color; prop.children [icon]]
            Html.td [prop.className color; prop.text (mediaSetId.ToUpper())]
            Html.td [prop.className color; prop.text (provider.Substring(0,1))]
            Html.td [prop.className color; prop.text (quality)]
            Html.td [prop.className color; prop.text (timestamp.ToString(TimeFormatString))]
        ]

    let showEvents () =
        Bulma.table [
            prop.children [
                Html.tbody (
                    state.Events
                    |> Seq.map showEvent
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
