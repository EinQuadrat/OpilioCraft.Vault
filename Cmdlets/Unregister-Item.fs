namespace OpilioCraft.Vault.Cmdlets

open System
open System.Management.Automation

[<Cmdlet(VerbsLifecycle.Unregister, "Item", DefaultParameterSetName="ItemId")>]
[<OutputType(typeof<Void>)>]
type public UnregisterItemCommand () =
    inherit VaultItemCommand ()

    // params
    [<Parameter(Position=0, Mandatory=true, ValueFromPipeline=true, ParameterSetName="ItemId")>]
    member val ItemId : string array = [| |] with get,set

    [<Parameter>]
    member val ReportUnknown = SwitchParameter(false) with get,set

    // cmdlet functionality
    override x.ProcessNonPath () =
        x.ItemId
        |> Array.partition x.ActiveVault.Contains
        |> fun (knownItems, unknownItems) ->
            knownItems |> Array.iter x.ActiveVault.Forget
            if x.ReportUnknown.IsPresent then unknownItems |> Array.iter ( fun item -> x.WriteWarning($"not in vault: {item}") )
