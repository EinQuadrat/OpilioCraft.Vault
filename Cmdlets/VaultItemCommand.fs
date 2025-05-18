namespace OpilioCraft.Vault.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.FSharp
open OpilioCraft.FSharp.PowerShell
open OpilioCraft.FSharp.PowerShell.CmdletExtension
open OpilioCraft.Vault

type private VaultStatus =
    | NotInitialized
    | Initialized of Vault

[<AbstractClass>]
type public VaultItemCommand() =
    inherit PathSupportingCommand()

    // vault
    let mutable vault = NotInitialized

    member _.ActiveVault =
        match vault with
        | NotInitialized -> failwith "[FATAL] connection to vault is not initialized yet"
        | Initialized vault -> vault

    // vault item
    member x.GetIdOfManagedItem(path) =
        let itemId = Fingerprint.fingerprintAsString path

        if not <| x.ActiveVault.Contains(itemId)
        then
            failwith $"not known to vault: {path}"
        else
            itemId

    // params
    [<Parameter>]
    [<ValidateNotNullOrEmpty>]
    member val Vault = Defaults.DefaultVault with get,set

    // functionality
    override x.BeginProcessing() =
        base.BeginProcessing()

        try
            vault <- Initialized <| VaultManager.getVault x.Vault
        with
            | exn -> x.ThrowAsTerminatingError(ErrorCategory.ResourceUnavailable, exn)

    // default implementations
    override _.ProcessPath(_) = raise <| InvalidOperationException()
    override _.ProcessNonPath() = raise <| InvalidOperationException()