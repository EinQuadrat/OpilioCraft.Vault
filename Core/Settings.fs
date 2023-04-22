namespace OpilioCraft.Vault.Core

open System

[<RequireQualifiedAccess>]
module Settings =
    // location of app specific data
    let AppDataLocation = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpilioCraft")

    // configuration files
    let VaultRegistryPath =
        [
            // file in user home overrules default file
            IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vaultcfg")
            IO.Path.Combine(AppDataLocation, "vault.json")
        ]
        |> List.tryFind IO.File.Exists
        |> Option.defaultWith (fun _ -> failwith "[OpilioCraft.Vault] registry file not found")
