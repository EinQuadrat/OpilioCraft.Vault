namespace OpilioCraft.Vault

open System
open System.IO

open OpilioCraft.FSharp
open OpilioCraft.FSharp.Json.UserSettings

// ----------------------------------------------------------------------------
// features supported by Vault

type private VaultCommand =
    | Contains      of ItemId: string * AsyncReplyChannel<bool>

    | Fetch         of ItemId: string * AsyncReplyChannel<VaultItem>
    | Store         of Item: VaultItem
    | Forget        of ItemId: string // any request for non-existing item is gracefully ignored

    | ImportFile    of ItemId: string * SourcePath: string
    | ExportFile    of ItemId: string * TargetPath: string * Overwrite: bool

    | List of AsyncReplyChannel<string list>

    | Quit

// ----------------------------------------------------------------------------
// vault itself
    
[<Sealed>]
type Vault private (backend: VaultBackend) =
    // supported events
    let errorEvent = Event<_>()
    
    // synchronized archive access
    let vaultAgent = lazy MailboxProcessor<VaultCommand>.Start(fun inbox ->
        let rec loop () =
            async {
                try
                    let! msg = inbox.Receive()

                    match msg with
                    | Contains(itemId, replyChannel) -> itemId |> backend.ContainsItem |> replyChannel.Reply
                    | Fetch(itemId, replyChannel) -> itemId |> backend.FetchItem |> replyChannel.Reply
                    | Store(item) -> item |> backend.StoreItem
                    | Forget(itemId) -> itemId |> backend.ForgetItem
                    | ImportFile(itemId, sourcePath) -> backend.ImportFile(itemId, sourcePath)
                    | ExportFile(itemId, targetPath, overwrite) -> backend.ExportFile(itemId, targetPath, overwrite)
                    | List(replyChannel) -> backend.List() |> replyChannel.Reply

                    | Quit -> return ()

                with
                    | exn -> errorEvent.Trigger(exn)

                return! loop ()
            }

        // engage! :-)
        loop ()
    )

    // public API
    member _.OnError = errorEvent.Publish

    member _.Contains(itemId) = vaultAgent.Force().PostAndReply(fun reply -> Contains(itemId, reply))
    member _.Fetch(itemId) = vaultAgent.Force().PostAndReply(fun reply -> Fetch(itemId, reply))
    member _.Store(item) = vaultAgent.Force().Post(Store(item))
    member _.Forget(itemId) = vaultAgent.Force().Post(Forget(itemId))
    member _.ImportFile(itemId, sourcePath) = vaultAgent.Force().Post(ImportFile(itemId, sourcePath))
    member _.ExportFile(itemId, targetPath, ?overwrite) = vaultAgent.Force().Post(ExportFile(itemId, targetPath, overwrite |> Option.defaultValue false))
    member _.List() = vaultAgent.Force().PostAndReply(fun reply -> List(reply))

    // instance creation
    static member Attach(pathToVault, ?enableCaching) =
        let resolvePath root (path: string) =
            if Path.IsPathRooted(path)
            then
                path 
            else
                Path.Combine(root, path)

        // check vault path
        Ok pathToVault
        |> Result.testWith Directory.Exists VaultNotFoundError

        // check settings file
        |> Result.map (fun p -> Path.Combine(p, Defaults.ConfigFilename))
        |> Result.testWith File.Exists MissingVaultConfigError

        // check settings
        |> Result.bind (load<VaultConfig> >> (Result.mapError InvalidVaultConfigError))
        |> Result.bind (Version.isValidVersion Defaults.ImplementationVersion >> (Result.mapError InvalidVaultConfigError))

        // check vault layout
        |> Result.bind
            (fun settings ->
                Ok settings.Layout
                |> Result.test (tryGetProperty "Items" >> Option.isSome) (MissingPropertyError("Items"))
                |> Result.test (tryGetProperty "Metadata" >> Option.isSome) (MissingPropertyError("Metadata"))
                |> Result.map (fun l -> { l with Metadata = resolvePath pathToVault (l.Metadata) })
                |> Result.testWith (fun s -> Directory.Exists(s.Metadata)) (fun s -> MissingFolderError(s.Metadata))
                |> Result.test (tryGetProperty "Files" >> Option.isSome ) (MissingPropertyError("Files"))
                |> Result.map (fun l -> { l with Files = resolvePath pathToVault (l.Files) })
                |> Result.testWith (fun s -> Directory.Exists(s.Files)) (fun s -> MissingFolderError(s.Files))
                |> Result.mapError InvalidVaultConfigError
            )
        
        // create backend
        |> Result.map
            (fun layout ->
                match enableCaching with
                | Some true -> layout |> CachingVaultBackend :> VaultBackend
                | _ -> layout |> VaultBackend
            )

        // initialize vault access
        |> Result.map (fun backend -> new Vault(backend))

    // implements IDisposable
    member private _.Disposer = lazy (vaultAgent.Value.Post(Quit))

    member x.DisposeHandler(disposing) =
        if disposing && (not x.Disposer.IsValueCreated)
        then
            x.Disposer.Force()

    interface IDisposable with
        member x.Dispose() =
            x.DisposeHandler(true)
            GC.SuppressFinalize(x)
