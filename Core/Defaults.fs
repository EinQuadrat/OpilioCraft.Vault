namespace OpilioCraft.Vault

open System
open System.Text.Json

open OpilioCraft.FSharp.Prelude

// ----------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module Defaults =
    let ImplementationVersion = Version(2, 0)
    let ConfigFilename = "vault.json"
    let DefaultVault = "DEFAULT"

    let VaultRegistryFilename =
        {|
            User = ".vaults"
            Global = "vault-registry.json"
        |}

    let DefaultJsonOptions =
        new JsonSerializerOptions(JsonSerializerOptions.Default)
        |> tee (fun jso -> jso.WriteIndented <- true)

