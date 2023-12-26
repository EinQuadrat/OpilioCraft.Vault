namespace OpilioCraft.Vault.Core

open OpilioCraft.FSharp.Prelude

// ----------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module VaultManager =
    // defaults
    let DefaultVault  = "DEFAULT"

    // active vaults
    let mutable private activeVaults : Map<string, VaultHandler> = Map.empty

    // archive access
    let private initVaultHandler name : VaultHandler =
        try
            let registeredVaults = UserSettings.vaultRegistry () in

            let pathToVault =
                if registeredVaults.ContainsKey name
                then
                    registeredVaults.[name]
                else
                    raise <| UnknownVaultException name

            VaultHandler.Init pathToVault
        with
            | exn -> failwith $"[VaultManager] cannot initialize vault handler: {exn.Message}"

    let getHandler name =
        if (not <| Map.containsKey name activeVaults)
        then
            activeVaults <- activeVaults |> Map.add name (initVaultHandler name)

        activeVaults.[name]

    let resetHandler name =
        // withdraw an already existing vaulte handler first
        if activeVaults.ContainsKey name
        then
            (activeVaults[name] :> System.IDisposable).Dispose()
            activeVaults <- activeVaults |> Map.remove name // remove from cache to force reload

        // get a new handler
        getHandler name
