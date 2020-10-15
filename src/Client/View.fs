module View

open Feliz
open Feliz.Bulma

open System
open Shared
open Model
open Messages

let [<Literal>] TimeFormatString = "HH:mm:ss"

let getShortId (mediaSetId : string) =
    mediaSetId.ToUpper().Split('~') |> Seq.last

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

    let showEvent (e : RemoteEvent) =

        let getResourceRef (e : RemoteEvent) =
            let partSegment =
                ResourceRef.getPartId e.ResourceRef
                |> Option.map (fun partId ->
                    match state.RecentMediaSets |> Map.tryFind e.MediaSetId with
                    | Some mediaSet -> 
                        match mediaSet.PartNumbers |> Map.tryFind partId with
                        | Some partNumber -> sprintf "%d-" partNumber
                        | None -> ""
                    | None -> "")
                |> Option.defaultValue ""
            match e.ResourceRef with
            | Dto.FileRef fileRef -> sprintf "%s%d" partSegment fileRef.QualityId
            | Dto.SubtitlesRef _ -> sprintf "%sTEXT" partSegment

        let color =
            match e.State with
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
            match e.State with
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
            Html.td [prop.className color; prop.text (getShortId e.MediaSetId)]
            Html.td [prop.className color; prop.text (e.StorageProvider.Substring(0,1))]
            Html.td [prop.className color; prop.text (getResourceRef e)]
            Html.td [prop.className color; prop.text (e.Timestamp.ToString(TimeFormatString))]
        ]

    let showRemoteEvents () =
        Bulma.table [
            prop.children [
                Html.tbody (
                    state.RemoteEvents
                    |> Seq.map showEvent
                )
            ]
        ]

    let showError () =
        Html.p [prop.text state.Error]

    let getMediaSetBoxColor (mediaSet : RecentMediaSet) =
        match RecentMediaSet.getAggregatedState mediaSet with
        | Dto.DistributionState.Requested -> Bulma.HasBackgroundPrimaryLight
        | Dto.DistributionState.Initiated -> Bulma.HasBackgroundLinkLight
        | Dto.DistributionState.Ingesting -> Bulma.HasBackgroundInfoLight
        | Dto.DistributionState.Segmenting -> Bulma.HasBackgroundInfoLight
        | Dto.DistributionState.Completed -> Bulma.HasBackgroundSuccessLight
        | Dto.DistributionState.Deleted -> Bulma.HasBackgroundSuccessLight
        | Dto.DistributionState.Cancelled -> Bulma.HasBackgroundDangerLight
        | Dto.DistributionState.Failed -> Bulma.HasBackgroundWarningLight
        | Dto.DistributionState.Rejected -> Bulma.HasBackgroundDangerLight
        | _ -> Bulma.HasBackgroundPrimaryLight

    let getMediaSetResourceIcon resourceState mediaSet =
        let icon, color =
            match resourceState with
            | Dto.DistributionState.None -> FA.FaMinus, Bulma.HasBackgroundGrey
            | Dto.DistributionState.Requested -> FA.FaEllipsisH, Bulma.HasBackgroundPrimary
            | Dto.DistributionState.Initiated -> FA.FaUpload, Bulma.HasBackgroundLink
            | Dto.DistributionState.Ingesting -> FA.FaCloudUpload, Bulma.HasBackgroundInfo
            | Dto.DistributionState.Segmenting -> FA.FaCloudUpload, Bulma.HasBackgroundInfo
            | Dto.DistributionState.Completed -> FA.FaCheckCircle, Bulma.HasBackgroundSuccess
            | Dto.DistributionState.Deleted -> FA.FaTrash, Bulma.HasBackgroundSuccess
            | Dto.DistributionState.Cancelled -> FA.FaExclamationCircle, Bulma.HasBackgroundDanger
            | Dto.DistributionState.Failed -> FA.FaExclamationTriangle, Bulma.HasBackgroundWarning
            | Dto.DistributionState.Rejected -> FA.FaExclamationCircle, Bulma.HasBackgroundDanger
        let color = 
            mediaSet.RemoveCountdown 
            |> Option.map (fun x -> if x % 2 = 1 then color + "-light" else color) 
            |> Option.defaultWith (fun () ->
            match mediaSet.Status with
            | Dto.MediaSetStatus.Expired | Dto.MediaSetStatus.Rejected -> color + "-dark"
            | _ -> color)
        [FA.Fa; icon; color; Bulma.HasTextWhite]

    let showMediaSetResource resourceState mediaSet =
        Html.i [
            prop.style [style.padding 5; style.margin(3,3,0,0); style.borderRadius 5; style.width 27]
            prop.className (getMediaSetResourceIcon resourceState mediaSet)]

    let showPendingMediaSet mediaSet =
        let defaultTitleClassName = [Bulma.Title; Bulma.Is4]
        let lighterTitleClassName = [Bulma.Title; Bulma.Is4; Bulma.HasTextGreyLighter]
        let lightTitleClassName = [Bulma.Title; Bulma.Is4; Bulma.HasTextGreyLight]
        let titleClassName = 
            mediaSet.RemoveCountdown 
            |> Option.map (fun x -> if x % 2 = 1 then lighterTitleClassName else defaultTitleClassName) 
            |> Option.defaultWith (fun () ->
            match mediaSet.Status with
            | Dto.MediaSetStatus.Expired | Dto.MediaSetStatus.Rejected -> lightTitleClassName
            | _ -> defaultTitleClassName)
        Bulma.box [
            prop.className [getMediaSetBoxColor mediaSet]
            prop.style [style.margin(5,5,5,5)]
            prop.children [
                Html.div [
                    prop.className titleClassName
                    prop.style [style.margin(0,0,0,0)]
                    prop.text (getShortId mediaSet.MediaSetId)
                ]
                Html.div (mediaSet.AkamaiFiles
                    |> Map.toList
                    |> List.map (fun (_,resourceState) -> showMediaSetResource resourceState mediaSet))
                Html.div (mediaSet.NepFiles
                    |> Map.toList
                    |> List.map (fun (_,resourceState) -> showMediaSetResource resourceState mediaSet))
            ]
        ]

    let showPendingMediaSets () =
        state.RecentMediaSets
        |> Map.toList
        |> List.map (fun (_, mediaSet) -> showPendingMediaSet mediaSet)

    Bulma.column [
        prop.children [
            Bulma.columns [
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
            ]
            Bulma.columns [
                Bulma.column [
                    column.is3
                    prop.children [
                        Html.div [
                            prop.children [
                                showRemoteEvents ()
                                showError ()
                            ]
                        ]
                    ]
                ]
                Bulma.column [
                    prop.children [
                        Bulma.columns [
                            prop.style [style.flexWrap.wrap; style.marginRight 10]
                            prop.children (showPendingMediaSets ())
                        ]
                    ]
                ]
            ]
        ]
    ]
