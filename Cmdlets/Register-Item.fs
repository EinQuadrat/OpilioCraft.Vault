namespace OpilioCraft.Vault.Cmdlets

open System.Management.Automation
open OpilioCraft.Vault.Core

[<Cmdlet(VerbsLifecycle.Register, "Item")>]
[<OutputType(typeof<System.Void>)>]
type public RegisterItemCommand () =
    inherit VaultItemCommand ()

    override x.ProcessPath path =
        x.VaultHandler |> VaultOperations.addToVault path
