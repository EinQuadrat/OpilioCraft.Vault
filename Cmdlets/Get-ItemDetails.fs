namespace OpilioCraft.Vault.Cmdlets

open System.Collections
open System.Management.Automation

open OpilioCraft.Vault.Core

[<Cmdlet(VerbsCommon.Get, "ItemDetails")>]
[<OutputType(typeof<ItemDetails>, typeof<Hashtable>)>]
type public GetItemDetailCommand () =
    inherit VaultItemCommandBase ()

    [<Parameter>]
    member val AsHashtable = SwitchParameter(false) with get, set

    // cmdlet funtionality
    override x.ProcessPath path =
        x.GetIdOfManagedItem path
        |> x.VaultHandler.Fetch
        |> fun metadata -> metadata.Details
        |>
            if x.AsHashtable.IsPresent // transformation needed?
            then
                Seq.fold (fun (ht : Hashtable) item -> ht.Add(item.Key, item.Value.Unwrap); ht) (Hashtable())
                >> x.WriteObject // output is Hashtable
            else
                x.WriteObject // output is ItemDetails
