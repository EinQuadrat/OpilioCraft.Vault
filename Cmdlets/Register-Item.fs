namespace OpilioCraft.Vault.Cmdlets

open System.IO
open System.Management.Automation
open OpilioCraft.Vault

[<Cmdlet(VerbsLifecycle.Register, "Item")>]
[<OutputType(typeof<System.Void>)>]
type public RegisterItemCommand() =
    inherit VaultItemCommand()

    [<Parameter>]
    member val SetReadOnly = SwitchParameter(false) with get,set

    // cmdlet behaviour
    override x.ProcessPath(path) =
        x.ActiveVault |> VaultOperations.addToVault path

        if x.SetReadOnly.IsPresent
        then
            File.SetAttributes(path, FileAttributes.ReadOnly)
