namespace OpilioCraft.Vault

open System
open System.Text.Json

open OpilioCraft.FSharp.Prelude

// ----------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module Defaults =
    let ImplementationVersion = Version(1, 0)
    let ConfigFilename = "settings.json"
    let DefaultVault = "DEFAULT"

    let VaultRegistryFilename = {|
        User = ".vaultcfg"
        Global = "vault-registry.json"
        |}

    let DefaultJsonOptions =
        new JsonSerializerOptions(JsonSerializerOptions.Default)
        |> tee (fun jso -> jso.WriteIndented <- true)

