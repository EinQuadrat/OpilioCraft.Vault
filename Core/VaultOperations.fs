module OpilioCraft.Vault.Core.VaultOperations

open System.IO
open OpilioCraft.FSharp.Prelude

// common operations
let addToVault path (vaultHandler : VaultHandler) =
    try
        let fileInfo = FileInfo path
        let fingerprint = fileInfo.FullName |> Fingerprint.fingerprintAsString

        if not <| vaultHandler.Contains fingerprint
        then
            // create metadata
            let vaultItem =
                {
                    Id = fingerprint
                    AsOf = fileInfo.LastAccessTimeUtc
                    ContentType = { Category = ContentCategory.Unspecified; FileExtension = fileInfo.Extension.ToLower() }
                    Relations = List.empty<Relation>
                    Details = ItemDetails()
                }

            // store it
            vaultHandler.ImportFile fingerprint path
            vaultHandler.Store vaultItem

    with
        | exn -> failwith $"[OpilioCraft.Vault] cannot add file to vault: {exn.Message}"

let updateVaultItem itemId (updateFunc : VaultItem -> VaultItem) (vaultHandler : VaultHandler) =
    try
        vaultHandler.Fetch itemId
        |> updateFunc
        |> vaultHandler.Store

    with
        | exn -> failwith $"[OpilioCraft.Vault] cannot update item with id {itemId}: {exn.Message}"

let removeFromVault itemId (vaultHandler : VaultHandler) =
    try
        vaultHandler.Forget itemId

    with
        | exn -> failwith $"[OpilioCraft.Vault] cannot forget item with id {itemId}: {exn.Message}"

// relation related operations
module Relations =
    // predicates
    let hasRelations item = item.Relations.Length > 0

    let isRelatedTo targetId item = item.Relations |> List.exists (fun rel -> rel.Target = targetId)
    let isRelatedToBy targetId relType item = item.Relations |> List.exists (fun rel -> (rel.Target = targetId) && (rel.IsA = relType))
    
    // updates, is supporting chaining
    let add rel item =
        match item |> isRelatedToBy rel.Target rel.IsA with
        | false -> { item with Relations = rel :: item.Relations }
        | _ -> item

    let remove rel item =
        { item with Relations = item.Relations |> List.filter ( fun r -> r <> rel ) }

    let removeRelationsTo targetId item =
        { item with Relations = item.Relations |> List.filter (fun rel -> rel.Target <> targetId) }
    
    let removeAll item =
        { item with Relations = [] }

// details related operations
module Details =
    // predicates
    let hasDetail name item =
        item.Details.ContainsKey(name)

    // accessors
    let getDetail name defaultValue item =
        if item.Details.ContainsKey(name) then item.Details.[name] else defaultValue

    let getDetails item =
        item.Details

    // updates, is supporting chaining
    let setDetail name value item =
        item.Details.[name] <- value
        item

    let setDetails modifications item =
        modifications |> Map.iter ( fun key value -> item.Details.[key] <- value )
        item

    let unsetDetail name item =
        item.Details.Remove(name) |> ignore
        item

    let unsetDetails (names : string list) item =
        names |> List.iter (fun name -> item.Details.Remove(name) |> ignore)
        item

    let resetDetailsTo itemDetails item =
        { item with Details = itemDetails }
