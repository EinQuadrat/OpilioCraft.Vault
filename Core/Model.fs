﻿namespace OpilioCraft.Vault

open System
open System.Text.Json.Serialization

open OpilioCraft.FSharp.Prelude

// ----------------------------------------------------------------------------

type VaultConfig =
    {
        Version : Version
        Layout : VaultLayout
    }

and VaultLayout =
    {
        Metadata : string
        Files : string
    }

// errors
type VaultError =
    | UnknownVault of Name:string
    | VaultNotFound of Path:string
    | MissingVaultSettingsFile of Path:string
    | InvalidVaultSettingsFile of OpilioCraft.FSharp.Json.UserSettings.ErrorReason
    | IncompatibleVaultVersion of Type:Type * Expected:Version * Found:Version
    | IncompatibleVaultLayout
    | RuntimeError of exn

// corresponding exceptions
exception VaultException of VaultError
    
// ----------------------------------------------------------------------------

type ItemId = Fingerprint

type VaultItem = // data structure used by the vault
    {
        Id : ItemId // use fingerprint (SHA256) as id
        AsOf : DateTime // as UTC timestamp
        Relations : Relation list
    }

    [<JsonIgnore>]
    member x.AsOfLocal = x.AsOf.ToLocalTime()

and Relation = {
    Target : ItemId
    IsA : RelationType
}

and RelationType =
    | Related = 0
    | Sibling = 1
    | Derived = 2
