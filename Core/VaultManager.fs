namespace OpilioCraft.Vault.Core

[<RequireQualifiedAccess>]
module VaultManager =
    // defaults
    let DefaultVault = "DEFAULT"

    // vault caching
    let mutable private vaults : Map<string, Vault> = Map.empty

    // vault connection handling
    let private initVault name : Vault =
        try
            UserSettings.vaultRegistry ()
            |> Map.tryFind name
            |> Option.map Vault.Attach
            |> Option.defaultWith (fun _ -> raise <| UnknownVaultException name)
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
