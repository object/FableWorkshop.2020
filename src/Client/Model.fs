module Model

open System
open Shared

[<RequireQualifiedAccess>]
type EventSet =
  | Small
  | Large

let [<Literal>] RemoveCountdownStart = 10
let [<Literal>] RemoveCountdownIntervalInMilliseconds = 500

module FileRef =
    let create partId qualityId : Dto.FileRef =
        { PartId = partId
          QualityId = qualityId }

module SubtitlesRef =
    let create partId : Dto.SubtitlesRef =
        { PartId = partId }

module ResourceRef =
    let getPartId resourceRef =
        match resourceRef with
        | Dto.FileRef fileRef -> fileRef.PartId
        | Dto.SubtitlesRef subRef -> subRef.PartId

type RecentMediaSet = {
    MediaSetId : string
    Status : Dto.MediaSetStatus
    PartNumbers : Map<string, int>
    AkamaiFiles : Map<Dto.FileRef, Dto.DistributionState>
    NepFiles : Map<Dto.FileRef, Dto.DistributionState>
    NepSubtitles : Map<Dto.SubtitlesRef, Dto.DistributionState>
    RemoveCountdown : int option
}

module ContentSet =

    let getPartNumbers (content : Dto.ContentSet) =
        match content with
        | Dto.WithParts parts ->
            parts
            |> Map.toList
            |> List.map (fun (partId,part) -> (partId, part.PartNumber))
            |> Map.ofList
        | _ -> Map.empty

    let getFiles (content : Dto.ContentSet) : Map<Dto.FileRef,Dto.ContentFile> =
        match content with
        | Dto.NoParts chunk ->
            match chunk.Files with
            | [] -> Map.empty
            | files ->
                files
                |> List.map (fun file -> FileRef.create None file.QualityId, file)
                |> Map.ofList
        | Dto.WithParts parts ->
            parts
            |> Map.toList
            |> List.collect (fun (partId,part) -> part.Content.Files |> List.map (fun file -> partId, file))
            |> List.map (fun (partId,file) -> FileRef.create (Some partId) file.QualityId, file)
            |> Map.ofList
        | _ -> Map.empty

    let getSubtitlesLinks (content : Dto.ContentSet) =
        match content with
        | Dto.NoParts chunk ->
            match chunk.SubtitlesLinks with
            | [] -> Map.empty
            | links -> [ SubtitlesRef.create None, links] |> Map.ofList
        | Dto.WithParts parts ->
            parts
            |> Map.toList
            |> List.map (fun (partId,part) -> SubtitlesRef.create (Some partId), part.Content.SubtitlesLinks)
            |> List.filter (fun (_,links) -> links.Length > 0)
            |> Map.ofList
        | _ -> Map.empty

module MediaSetStatus =
    let fromInt num =
        match num with
        | 0 -> Dto.MediaSetStatus.Pending
        | 1 -> Dto.MediaSetStatus.Completed
        | 2 -> Dto.MediaSetStatus.Rejected
        | 3 -> Dto.MediaSetStatus.Expired
        | _ -> invalidArg "num" <| sprintf "Invalid status %d" num

    let toInt status =
        match status with
        | Dto.MediaSetStatus.Pending -> 0
        | Dto.MediaSetStatus.Completed -> 1
        | Dto.MediaSetStatus.Rejected -> 2
        | Dto.MediaSetStatus.Expired -> 3

module RecentMediaSet =

    let fromMediaSetState (mediaSetState : Dto.MediaSetState) recentMediaSets =
        let mediaSet = 
            match recentMediaSets |> Map.tryFind mediaSetState.PersistenceId with
            | Some mediaSet -> mediaSet 
            | None ->
                {
                  MediaSetId = mediaSetState.PersistenceId
                  Status = Dto.MediaSetStatus.Pending
                  PartNumbers = ContentSet.getPartNumbers mediaSetState.State.Desired.Content
                  AkamaiFiles = ContentSet.getFiles mediaSetState.State.Desired.Content |> Map.map (fun _ _ -> Dto.DistributionState.None)
                  NepFiles = ContentSet.getFiles mediaSetState.State.Desired.Content |> Map.map (fun _ _ -> Dto.DistributionState.None)
                  NepSubtitles = ContentSet.getSubtitlesLinks mediaSetState.State.Desired.Content |> Map.map (fun _ _ -> Dto.DistributionState.None)
                  RemoveCountdown = None
                }
        let akamaiFiles = 
            mediaSetState.State.Current.AkamaiFiles
            |> List.map (fun x -> FileRef.create x.PartId x.QualityId, x.RemoteState.State) |> Map.ofList
        let nepFiles = 
            mediaSetState.State.Current.NepFiles
            |> List.map (fun x -> FileRef.create x.PartId x.QualityId, x.RemoteState.State) |> Map.ofList
        let nepSubtitles = 
            mediaSetState.State.Current.NepSubtitles
            |> List.map (fun x -> SubtitlesRef.create x.PartId, x.RemoteState.State) |> Map.ofList
        { mediaSet with
            AkamaiFiles = akamaiFiles |> Map.fold (fun acc k v -> acc |> Map.add k v) mediaSet.AkamaiFiles
            NepFiles = nepFiles |> Map.fold (fun acc k v -> acc |> Map.add k v) mediaSet.NepFiles
            NepSubtitles = nepSubtitles |> Map.fold (fun acc k v -> acc |> Map.add k v) mediaSet.NepSubtitles
        }

    let getAggregatedState (mediaSet : RecentMediaSet) =
        [
            mediaSet.AkamaiFiles |> Map.toList |> List.map snd
            mediaSet.NepFiles |> Map.toList |> List.map snd
            mediaSet.NepSubtitles |> Map.toList |> List.map snd 
        ] 
        |> List.concat
        |> List.fold (fun acc elt ->
            if acc = Dto.DistributionState.Rejected || elt = Dto.DistributionState.Rejected then Dto.DistributionState.Rejected 
            else if acc = Dto.DistributionState.Failed || elt = Dto.DistributionState.Failed then Dto.DistributionState.Failed
            else if acc = Dto.DistributionState.Requested || elt = Dto.DistributionState.Requested then Dto.DistributionState.Requested
            else if acc = Dto.DistributionState.Initiated || elt = Dto.DistributionState.Initiated then Dto.DistributionState.Initiated
            else if acc = Dto.DistributionState.Ingesting || elt = Dto.DistributionState.Ingesting then Dto.DistributionState.Ingesting
            else if acc = Dto.DistributionState.Segmenting || elt = Dto.DistributionState.Segmenting then Dto.DistributionState.Segmenting
            else if acc = Dto.DistributionState.Completed || elt = Dto.DistributionState.Completed then Dto.DistributionState.Completed
            else if acc = Dto.DistributionState.Deleted || elt = Dto.DistributionState.Deleted then Dto.DistributionState.Deleted
            else Dto.DistributionState.None) 
            Dto.DistributionState.None

    let updateFromRemoteEvent msg mediaSet =
        let startCountdown mediaSet =
            if mediaSet.RemoveCountdown.IsNone 
            then { mediaSet with RemoveCountdown = Some RemoveCountdownStart }
            else mediaSet
        let mediaSet =
            match msg with
            | Dto.RemoteFileUpdate e ->
                let akamaiFiles = 
                    if String.Compare(e.StorageProvider, "Akamai", true) = 0
                    then mediaSet.AkamaiFiles |> Map.add e.FileRef e.RemoteState.State
                    else mediaSet.AkamaiFiles
                let nepFiles = 
                    if String.Compare(e.StorageProvider, "Nep", true) = 0
                    then mediaSet.NepFiles |> Map.add e.FileRef e.RemoteState.State
                    else mediaSet.NepFiles
                { mediaSet with AkamaiFiles = akamaiFiles; NepFiles = nepFiles }
            | Dto.RemoteSubtitlesUpdate e ->
                let nepSubtitles = mediaSet.NepSubtitles |> Map.add e.SubtitlesRef e.RemoteState.State 
                { mediaSet with NepSubtitles = nepSubtitles }
            | Dto.StatusUpdate e -> 
                { mediaSet with Status = MediaSetStatus.fromInt e.Status }
        match mediaSet.Status with
        | Dto.MediaSetStatus.Completed | Dto.MediaSetStatus.Expired -> mediaSet |> startCountdown
        | _ -> mediaSet

type RemoteEvent = {
    MediaSetId : string
    StorageProvider : string
    ResourceRef : Dto.ResourceRef
    State : Dto.DistributionState
    Timestamp : DateTimeOffset
}

module RemoteEvents =
    let update msg remoteEvents =
        match msg with
        | Dto.RemoteFileUpdate e ->
          { MediaSetId = e.MediaSetId
            StorageProvider = e.StorageProvider
            ResourceRef = Dto.FileRef e.FileRef
            State = e.RemoteState.State
            Timestamp = e.RemoteState.Timestamp } :: remoteEvents
        | Dto.RemoteSubtitlesUpdate e ->
          { MediaSetId = e.MediaSetId
            StorageProvider = e.StorageProvider 
            ResourceRef = Dto.SubtitlesRef e.SubtitlesRef
            State = e.RemoteState.State
            Timestamp = e.RemoteState.Timestamp } :: remoteEvents
        | Dto.StatusUpdate _ -> 
          remoteEvents

type Model = 
    { EventSet: EventSet 
      PlaybackDelay : int
      IsPlaying : bool
      IsPaused : bool
      RecentMediaSets : Map<string, RecentMediaSet>
      RemoteEvents : RemoteEvent list
      SocketConnected : bool
      Error : string }
    static member Empty = 
        { EventSet = EventSet.Small
          PlaybackDelay = 2000
          IsPlaying = false
          IsPaused = false
          RecentMediaSets = Map.empty
          RemoteEvents = List.empty
          SocketConnected = false
          Error = "" }
