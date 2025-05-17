namespace OpilioCraft.Vault

open System.Collections.Generic

type CachingVaultBackend(layout) =
    inherit VaultBackend(layout)

    let cache = new Dictionary<ItemId, VaultItem>()

    // public API
    override _.ContainsItem itemId = cache.ContainsKey itemId || base.ContainsItem itemId

    override _.FetchItem itemId =
        if not <| cache.ContainsKey itemId then cache.[itemId] <- base.FetchItem itemId // populate cache on demand
        cache.[itemId]

    override _.StoreItem item =
        let itemId = item.Id // very defensive style: be prepared for item corruption on saving it to disc

        try
            base.StoreItem item
            cache.[itemId] <- item // update cache
        with
            | _ -> itemId |> cache.Remove |> ignore; reraise() // in case of errors remove item from cache
    
    override _.ForgetItem itemId =
        try
            base.ForgetItem itemId
        finally
            if cache.ContainsKey itemId then cache.Remove itemId |> ignore
