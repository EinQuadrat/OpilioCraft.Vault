namespace OpilioCraft.Vault.Cmdlets

open System.Management.Automation

open OpilioCraft.FSharp.Prelude

[<Cmdlet(VerbsDiagnostic.Test, "VaultItem")>]
[<OutputType(typeof<bool>)>]
type public TestVaultItemCommand () =
    inherit VaultItemCommand ()

    // cmdlet funtionality
    override x.ProcessPath path =
        path
        |> Fingerprint.fingerprintAsString
        |> x.ActiveVault.Contains
        |> x.WriteObject
