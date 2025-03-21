namespace OpilioCraft.Vault

open OpilioCraft.FSharp.Prelude

[<RequireQualifiedAccess>]
module VaultManager =
    // defaults
    let DefaultVault = "DEFAULT"

    // vault caching
    let mutable private vaults : Map<string, Vault> = Map.empty

    // vault connection handling
    let private initVault name : Vault =
        try
            VaultRegistry.vaultRegistry ()
            |> Map.tryFind name
            |> Result.ofOption (UnknownVault name)
            |> Result.bind Vault.Attach
            |> Result.defaultWith (raise << VaultException)
        with
            | exn -> failwith $"[VaultManager] cannot attach to vault: {exn.Message}"

    let getVault name =
        if not <| Map.containsKey name vaults
        then
            vaults <- vaults |> Map.add name (initVault name)

        vaults[name]

    let resetVault name =
        // withdraw an already existing vaulte handler first
        if vaults.ContainsKey(name)
        then
            (vaults[name] :> System.IDisposable).Dispose()
            vaults <- vaults |> Map.remove name // remove from cache to force reload

        // get a new vault
        getVault name
