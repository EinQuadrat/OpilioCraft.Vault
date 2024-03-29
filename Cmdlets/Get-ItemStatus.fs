﻿namespace OpilioCraft.Vault.Cmdlets

open System.Management.Automation

[<Cmdlet(VerbsCommon.Get, "ItemStatus")>]
[<OutputType(typeof<bool>)>]
type public GetItemStatusCommand () =
    inherit VaultItemCommand ()

    // cmdlet funtionality
    override x.ProcessPath path =
        path
        |> x.VaultHandler.Contains
        |> x.WriteObject
