namespace Shared

open System

module Route =
    let hello = "/api/hello"
    let files = "/api/files"

module Dto =

  [<RequireQualifiedAccess>]
  type MediaSetStatus =
      | Pending
      | Completed
      | Rejected
      | Expired

  [<RequireQualifiedAccess>]
  type MediaType =
      | Video
      | Audio

  [<RequireQualifiedAccessAttribute>]
  type VideoMode = 
      | Default

  [<RequireQualifiedAccessAttribute>]
  type AudioMode = 
      | Default 
      | Audio51

  type AudioVideoMode = AudioMode * VideoMode

  [<RequireQualifiedAccessAttribute>]
  type MediaMode =
      | AudioVideo of AudioVideoMode
      | Audio of AudioMode
      
  [<RequireQualifiedAccess>]
  type DistributionState =
      | None
      | Requested
      | Initiated
      | Ingesting
      | Segmenting
      | Completed
      | Deleted
      | Cancelled
      | Failed
      | Rejected

  type AccessRestrictions = 
      { GeoRestriction : string
        MediaAccess : string }

  type ContentFile =
      { QualityId : int
        FileName : string
        SourcePath : string }
    
  type SubtitlesLink = 
      { Kind : string
        LanguageCode : string
        Format : string
        LinkUrl : string
        Duration : int
        Forced : bool }

  type ContentChunk = 
      { Files : ContentFile list
        SubtitlesLinks : SubtitlesLink list }
      
  type ContentPart = 
      { PartNumber : int
        Content : ContentChunk }

  type ContentSet = 
      | Empty
      | NoParts of ContentChunk
      | WithParts of Map<string, ContentPart>

  type DesiredMediaSetState = 
      { MediaMode : MediaMode option
        AccessRestrictions : AccessRestrictions
        Content : ContentSet }

  type RemoteState = 
      { State : DistributionState
        Timestamp : DateTimeOffset }

  type RemoteError = int * string
  type RemoteResult = Result<unit,RemoteError>

  type AkamaiStorageAssignment =
      { VolumeId : string
        EdgeChar : string
        Timestamp : DateTimeOffset }

  type AkamaiStorageState =
      | Unassigned
      | Assigned of AkamaiStorageAssignment

  type AkamaiFile =
      { SourcePath : string
        DirectoryPath : string option
        CdnPath : string }

  type AkamaiFileState =
      { PartId : string option
        QualityId : int
        File : AkamaiFile
        RemoteState : RemoteState
        LastResult : RemoteResult }

  type NepServiceAssignment = 
      { ServiceId : int
        UrlPrefix : string
        AssetIds : int list
        FormatIds : int list
        Timestamp : DateTimeOffset }

  type NepServiceState =
      | Unassigned
      | Assigned of NepServiceAssignment

  type NepAssetAssignment = 
      { AssetId : int
        Timestamp : DateTimeOffset }

  type NepAssetParts = Map<string,NepAssetAssignment>

  type NepStorageAssets =
      | Empty
      | NoParts of NepAssetAssignment
      | WithParts of NepAssetParts

  type NepStorageAssignment = 
      { Service : NepServiceState
        Assets : NepStorageAssets }

  type NepStorageState =
      | Unassigned
      | Assigned of NepStorageAssignment

  type NepFile =
      { SourcePath : string
        AssetId : int }

  type NepFileState =
      { PartId : string option
        QualityId : int
        File : NepFile
        RemoteState : RemoteState
        LastResult : RemoteResult }

  type NepSubtitles =
      { Links : SubtitlesLink list
        AssetId : int }

  type NepSubtitlesState =
      { PartId : string option
        Subtitles : NepSubtitles
        RemoteState : RemoteState
        LastResult : RemoteResult }

  type CurrentMediaSetState =
      { AkamaiStorage : AkamaiStorageState
        AkamaiFiles : AkamaiFileState list
        NepStorage : NepStorageState
        NepFiles : NepFileState list
        NepSubtitles : NepSubtitlesState list }

  type TotalState =
      { Desired : DesiredMediaSetState 
        Current : CurrentMediaSetState }

  type MediaSetState =
      { PersistenceId : string
        State : TotalState }

  type FileRef =
      { PartId : string option
        QualityId : int }

  type SubtitlesRef =
      { PartId : string option }

  type ResourceRef =
      | FileRef of FileRef
      | SubtitlesRef of SubtitlesRef

  [<RequireQualifiedAccess>]
  type AkamaiCommand =
      | UploadFile of FileRef
      | MoveFile of FileRef
      | DeleteFile of FileRef
      | RepairFileReference of FileRef
      | CreateStorage

  [<RequireQualifiedAccess>]
  type NepCommand =
      | UploadFile of FileRef
      | UploadSubtitles of SubtitlesRef
      | CreateStorage
      | RenewStorage
      | DeleteStorage
      | UpdateServiceOutputs
      | SetServiceUrlPrefix
      | RepairServiceReference

  type RemainingActions =
      { Akamai : AkamaiCommand list
        Nep : NepCommand list }

  type MediaSetStatusUpdate = {
    MediaSetId : string
    Status : int
    RemainingActions : RemainingActions
    Timestamp : DateTimeOffset
  }

  type MediaSetRemoteFileUpdate = {
    MediaSetId : string
    StorageProvider : string
    FileRef : FileRef
    RemoteState : RemoteState
    RemoteResult : RemoteResult
  }

  type MediaSetRemoteSubtitlesUpdate = {
    MediaSetId : string
    StorageProvider : string
    SubtitlesRef : SubtitlesRef
    RemoteState : RemoteState
    RemoteResult :RemoteResult
  }
    
  type MediaSetEvent =
  | StatusUpdate of MediaSetStatusUpdate
  | RemoteFileUpdate of MediaSetRemoteFileUpdate
  | RemoteSubtitlesUpdate of MediaSetRemoteSubtitlesUpdate

  [<RequireQualifiedAccess>]
  type Activity =
    | MediaSetState of MediaSetState
    | MediaSetEvent of MediaSetEvent 
