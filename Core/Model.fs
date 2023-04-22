namespace OpilioCraft.Vault.Core

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
        ContentType : ContentType
        Relations   : Relation list
        Details     : ItemDetails
    }

    [<JsonIgnore>]
    member x.AsOfLocal = x.AsOf.ToLocalTime()

and ContentType =
    {
        Category      : ContentCategory
        FileExtension : string
    }

and ContentCategory =
    | Unspecified = 0
    | Image = 1
    | Movie = 2
    | Digitalized = 3 // digitalized former analogue stuff

and Relation = {
    Target : ItemId
    IsA    : RelationType
}

and RelationType =
    | Related = 0
    | Sibling = 1
    | Derived = 2

and ItemDetails = Collections.Generic.Dictionary<string,ItemDetail>

and ItemDetail =
    | Boolean of bool
    | Float of float
    | Number of decimal
    | DateTime of DateTime
    | TimeSpan of TimeSpan
    | String of string

    static member Wrap value : ItemDetail =
        match box value with
        | :? bool as v      -> Boolean v
        | :? float as v     -> Float v
        | :? decimal as v   -> Number v
        | :? DateTime as v  -> DateTime v
        | :? TimeSpan as v  -> TimeSpan v
        | :? String as v    -> String v

        // default case
        | v                 -> String (v.ToString())

    member x.Unwrap : obj =
        match x with
        | Boolean plainValue  -> plainValue :> obj
        | Float plainValue    -> plainValue :> obj
        | Number plainValue   -> plainValue :> obj
        | DateTime plainValue -> plainValue :> obj
        | TimeSpan plainValue -> plainValue :> obj
        | String plainValue   -> plainValue :> obj

    member x.AsBoolean : bool =
        match x with
        | Boolean value -> value
        | _ -> failwith $"cannot cast {x.GetType().FullName} to System.Boolean"

    member x.AsDateTime : DateTime =
        match x with
        | DateTime value -> value
        | _ -> failwith $"cannot cast {x.GetType().FullName} to System.DateTime"

    member x.AsString : string =
        match x with
        | String value -> value
        | _ -> failwith $"cannot cast {x.GetType().FullName} to System.String"

    static member ofFlexibleValue fval =
        match fval with
        | FlexibleValue.Boolean x  -> Boolean x
        | FlexibleValue.Numeral x  -> decimal x |> Number
        | FlexibleValue.Float x    -> Float x
        | FlexibleValue.Decimal x  -> Number x
        | FlexibleValue.DateTime x -> DateTime x
        | FlexibleValue.TimeSpan x -> TimeSpan x
        | FlexibleValue.String x   -> String x

        // value changing operations
        | FlexibleValue.Date x     -> x.ToDateTime(System.TimeOnly.MinValue) |> DateTime

        // unsupported cases
        | _ -> failwith "cannot convert given FlexibleValue to ItemDetail"
