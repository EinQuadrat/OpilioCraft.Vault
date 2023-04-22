namespace OpilioCraft.Vault.Cmdlets

open System
open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Unregister, "Item", DefaultParameterSetName="ItemId")>]
[<OutputType(typeof<Void>)>]
type public UnregisterItemCommand () =
    inherit VaultItemCommandBase ()

    // params
    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ParameterSetName="ItemId")>]
    member val ItemId : string array = [| |] with get,set

    // cmdlet functionality
    override x.ProcessNonPath () =
        x.ItemId
        |> Array.filter x.VaultHandler.Contains
        |> Array.iter x.VaultHandler.Forget
