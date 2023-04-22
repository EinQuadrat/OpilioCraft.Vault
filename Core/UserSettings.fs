namespace OpilioCraft.Vault.Core

open OpilioCraft.FSharp.Prelude.UserSettings
   
[<RequireQualifiedAccess>]
module internal UserSettings =
    // load user settings on demand
    let private loadVaultRegistry = lazyLoad<VaultRegistry> Settings.VaultRegistryPath
    let vaultRegistry () = loadVaultRegistry.Value
