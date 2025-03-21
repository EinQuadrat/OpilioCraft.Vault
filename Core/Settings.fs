namespace OpilioCraft.Vault

open System

open OpilioCraft.FSharp.Json

// ------------------------------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module Settings =
    // configuration files
    let VaultRegistryPath =
        [
            // file in user home overrules default file
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vaultcfg")
            IO.Path.Combine(OpilioCraft.Settings.AppDataLocation, "vault-registry.json")
        ]
        |> List.tryFind IO.File.Exists
        |> Option.defaultWith (fun _ -> failwith "[OpilioCraft.Vault] registry file not found")


// ------------------------------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module UserSettings =
    let load () = UserSettings.load<VaultRegistry> Settings.VaultRegistryPath

    // load user settings on demand
    let private guardedLoad = lazy ( load () |> Result.defaultWith UserSettings.throwExceptionOnError )
    let vaultRegistry () = guardedLoad.Value
