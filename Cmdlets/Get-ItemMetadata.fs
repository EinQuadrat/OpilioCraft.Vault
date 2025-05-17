namespace OpilioCraft.Vault.Cmdlets

open System.Management.Automation

open OpilioCraft.FSharp.Prelude
open OpilioCraft.Vault

[<Cmdlet(VerbsCommon.Get, "ItemMetadata")>]
[<OutputType(typeof<VaultItem>)>]
type public GetItemDataCommand() =
    inherit VaultItemCommand()

    // cmdlet funtionality
    override x.ProcessPath(path) =
        path
        |> Fingerprint.fingerprintAsString
        |> x.ActiveVault.Fetch
        |> x.WriteObject
