namespace OpilioCraft.Vault

open System
open System.Text.Json.Serialization

open OpilioCraft.FSharp.Prelude

// ----------------------------------------------------------------------------

type VaultRegistry = Map<string,string>

exception UnknownVaultException of Name : string
    with override x.Message = $"[OpilioCraft.Vault] no vault of name \"{x.Name}\" registered"

exception MissingVaultSettingsFileException of Path:string
    with override x.Message = $"[OpilioCraft.Vault] expected vault settings file at {x.Path}"

// ----------------------------------------------------------------------------

type VaultConfig =
    {
        Version     : Version
        Layout      : VaultLayout
    }
    
    interface IJsonOnDeserialized with
        member x.OnDeserialized () =
            isNotNull x.Layout -||- ArgumentNullException("VaultConfig.Layout")

and VaultLayout =
    {
        Metadata : string
        Files    : string
    }

    interface IJsonOnDeserialized with
        member x.OnDeserialized () =
            isNotNull x.Metadata -||- ArgumentNullException("VaultConfig.VaultLayout.Metdata")
            isNotNull x.Files -||- ArgumentNullException("VaultConfig.VaultLayout.Files")

// ----------------------------------------------------------------------------

type ItemId = Fingerprint

type VaultItem = // data structure used by the vault
    {
        Id          : ItemId // use fingerprint (SHA256) as id
        AsOf        : DateTime // as UTC timestamp
        Relations   : Relation list
    }

    [<JsonIgnore>]
    member x.AsOfLocal = x.AsOf.ToLocalTime()

and Relation = {
    Target : ItemId
    IsA    : RelationType
}

and RelationType =
    | Related = 0
    | Sibling = 1
    | Derived = 2
