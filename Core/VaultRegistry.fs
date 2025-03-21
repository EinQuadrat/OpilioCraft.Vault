namespace OpilioCraft.Vault

open System

open OpilioCraft.FSharp.Prelude
open OpilioCraft.FSharp.Json

// ----------------------------------------------------------------------------

type VaultRegistry = Map<string,string>

// errors
type VaultRegistryError =
    | MissingVaultRegistry
    | InvalidVaultRegistry of OpilioCraft.FSharp.Json.UserSettings.ErrorReason

// corresponding exceptions
exception VaultRegistryException of VaultRegistryError

// associated functions
module VaultRegistry =
    let vaultRegistryPath () =
        [
            // file in user home overrules default file
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vaultcfg")
            IO.Path.Combine(OpilioCraft.Settings.AppDataLocation, "vault-registry.json")
        ]
        |> List.tryFind IO.File.Exists
        |> Result.ofOption MissingVaultRegistry
    
    let VaultRegistryPath = 
        vaultRegistryPath ()
        |> Result.defaultWith (raise << VaultRegistryException)

    let loadVaultRegistry () =
        vaultRegistryPath ()
        |> Result.bind (UserSettings.load<VaultRegistry> >> (Result.mapError InvalidVaultRegistry))

    // load user settings on demand
    let private guardedLoad = lazy ( loadVaultRegistry () |> Result.defaultWith (raise << VaultRegistryException))
    let vaultRegistry () = guardedLoad.Value
