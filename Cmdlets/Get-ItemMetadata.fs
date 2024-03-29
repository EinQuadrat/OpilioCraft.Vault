﻿namespace OpilioCraft.Vault.Cmdlets

open System.Management.Automation

open OpilioCraft.FSharp.Prelude
open OpilioCraft.Vault.Core

[<Cmdlet(VerbsCommon.Get, "ItemMetadata")>]
[<OutputType(typeof<VaultItem>)>]
type public GetItemDataCommand () =
    inherit VaultItemCommand ()

    // cmdlet funtionality
    override x.ProcessPath path =
        path
        |> Fingerprint.fingerprintAsString
        |> x.VaultHandler.Fetch
        |> x.WriteObject
