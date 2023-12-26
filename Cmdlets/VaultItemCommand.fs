namespace OpilioCraft.Vault.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.FSharp.Prelude
open OpilioCraft.FSharp.PowerShell
open OpilioCraft.FSharp.PowerShell.CmdletExtension
open OpilioCraft.Vault.Core

type private VaultHandlerStatus =
    | NotInitialized
    | Handler of VaultHandler

[<AbstractClass>]
type public VaultItemCommand () =
    inherit PathSupportingCommand ()

    // vault handler
    let mutable vaultHandler = NotInitialized

    member _.VaultHandler =
        match vaultHandler with
        | NotInitialized -> failwith "[FATAL] connection to vault is not initialized yet"
        | Handler vaultHandler -> vaultHandler

    // vault item
    member x.GetIdOfManagedItem path =
        let itemId = Fingerprint.fingerprintAsString path

        if not <| x.VaultHandler.Contains itemId
        then
            failwith $"not known to vault: {path}"
        else
            itemId

    // params
    [<Parameter>]
    [<ValidateNotNullOrEmpty>]
    member val Vault = VaultManager.DefaultVault with get,set

    // functionality
    override x.BeginProcessing () =
        base.BeginProcessing ()

        try
            vaultHandler <- Handler <| VaultManager.getHandler x.Vault
        with
            | exn -> exn |> x.ThrowAsTerminatingError ErrorCategory.ResourceUnavailable 

    // default implementations
    override _.ProcessPath _ = raise <| InvalidOperationException()
    override _.ProcessNonPath () = raise <| InvalidOperationException()