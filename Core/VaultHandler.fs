namespace OpilioCraft.Vault.Core

open System
open System.IO

open OpilioCraft.FSharp.Prelude

// ----------------------------------------------------------------------------
// features supported by VaultHandler

type private VaultCommand =
    | Contains of itemId:ItemId * AsyncReplyChannel<bool>

    | Fetch  of itemId:ItemId * AsyncReplyChannel<VaultItem>
    | Store  of item:VaultItem
    | Forget of itemId:ItemId // any request for non-existing item is gracefully ignored

    | ImportFile of itemId:ItemId * sourcePath:string
    | ExportFile of itemId:ItemId * targetPath:string * overwrite:bool

    | List of AsyncReplyChannel<ItemId list>

    | Quit

// ----------------------------------------------------------------------------
// vault handler itself

[<Sealed>]
type VaultHandler private (backend : VaultBackend) =
    static let ImplementationVersion = Version(1, 0)
    static let SettingsFilename = "settings.json"

    // supported events
    let errorEvent = Event<_>()
    
    // synchronized archive access
    let vaultAgent = lazy MailboxProcessor<VaultCommand>.Start(fun inbox ->
        let rec loop () =
            async {
                try
                    let! msg = inbox.Receive()

                    match msg with
                    | Contains (itemId, replyChannel) -> itemId |> backend.ContainsItem |> replyChannel.Reply
                    | Fetch (itemId, replyChannel) -> itemId |> backend.FetchItem |> replyChannel.Reply
                    | Store item -> item |> backend.StoreItem
                    | Forget itemId -> itemId |> backend.ForgetItem
                    | ImportFile (itemId, sourcePath) -> backend.ImportFile itemId sourcePath
                    | ExportFile (itemId, targetPath, overwrite) -> backend.ExportFile itemId targetPath overwrite
                    | List replyChannel -> backend.List() |> replyChannel.Reply

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

    member _.Contains itemId = vaultAgent.Force().PostAndReply(fun reply -> Contains(itemId, reply))
    member _.Fetch itemId = vaultAgent.Force().PostAndReply(fun reply -> Fetch(itemId, reply))
    member _.Store item = vaultAgent.Force().Post(Store(item))
    member _.Forget itemId = vaultAgent.Force().Post(Forget(itemId))
    member _.ImportFile itemId sourcePath = vaultAgent.Force().Post(ImportFile(itemId, sourcePath))
    member _.ExportFile itemId targetPath overwrite = vaultAgent.Force().Post(ExportFile(itemId, targetPath, overwrite))
    member _.List () = vaultAgent.Force().PostAndReply(fun reply -> List(reply))

    // instance creation
    static member VerifyVaultSetup pathToVault =
        // check given path
        if not <| Directory.Exists pathToVault
        then
            raise <| DirectoryNotFoundException pathToVault

        // check settings file exists
        let pathToSettingsFile = Path.Combine(pathToVault, SettingsFilename)

        if not <| File.Exists pathToSettingsFile
        then
            raise <| MissingVaultSettingsFileException pathToSettingsFile

        // load vault configuration
        let vaultConfig : VaultConfig =
            UserSettings.load<VaultConfig> pathToSettingsFile
            |> Verify.isVersion ImplementationVersion
        
        // returns parameters for VaultHandler constructor
        (pathToVault, vaultConfig.Layout)

    static member Init(pathToVault, ?enableCaching) =
        let vaultHandlerArgs = VaultHandler.VerifyVaultSetup pathToVault in

        let backend =
            match enableCaching with
            | Some true -> vaultHandlerArgs |> CachingVaultBackend :> VaultBackend
            | _ -> vaultHandlerArgs |> VaultBackend

        new VaultHandler(backend)

    // implements IDisposable
    member private x.Disposer = lazy ( vaultAgent.Value.Post(Quit) )

    member x.DisposeHandler disposing =
        if (disposing && (not x.Disposer.IsValueCreated))
        then
            x.Disposer.Force ()

    interface IDisposable with
        member x.Dispose() =
            x.DisposeHandler true
            GC.SuppressFinalize x
