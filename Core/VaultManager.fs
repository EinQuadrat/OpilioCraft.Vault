namespace OpilioCraft.Vault

open System

open OpilioCraft.FSharp
open OpilioCraft.FSharp.Json

// ----------------------------------------------------------------------------

type VaultManagerError =
    | MissingVaultRegistryError
    | InvalidVaultRegistryError of UserSettings.UserSettingsError
    | UnknownVaultError of string
    | VaultAlreadyExistsError of string
    | DirectoryNotEmptyError of string
    | CannotInitializeVaultError of exn
    | VaultError of VaultError

// corresponding exceptions
exception VaultManagerException of VaultManagerError

// ----------------------------------------------------------------------------

[<RequireQualifiedAccess>]
module VaultManager =
    // mapping name to vault path
    type private VaultRegistry = Map<string, string>

    // vault caching
    let mutable private vaults : Map<string, Vault> = Map.empty

    // vault registry
    [<RequireQualifiedAccess>]
    module VaultRegistry =
        let tryGetRegistryPath () =
            [
                // file in user home overrules global file
                IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Defaults.VaultRegistryFilename.User)
                IO.Path.Combine(OpilioCraft.Settings.AppDataLocation, Defaults.VaultRegistryFilename.Global)
            ]
            |> List.tryFind IO.File.Exists
            |> Result.ofOption MissingVaultRegistryError
    
        let getRegistryPath = 
            tryGetRegistryPath ()
            |> Result.defaultWith (raise << VaultManagerException)

        let tryLoad () =
            tryGetRegistryPath ()
            |> Result.bind (UserSettings.load<VaultRegistry> >> (Result.mapError InvalidVaultRegistryError))

        let load () =
            tryLoad ()
            |> Result.defaultWith (raise << VaultManagerException)

        let containsVault name (r: VaultRegistry) = r.ContainsKey(name)

        // modify registry
        module Modify =
            let private modify f = f >> UserSettings.save<VaultRegistry> getRegistryPath
            let private modifyIf p f r = if p r then modify f r

            let registerVault name path =
                modifyIf (not << containsVault name) (Map.add name path)

            let unregisterVault name =
                modifyIf (containsVault name) (Map.remove name)

            let renameVault oldName newName (r: VaultRegistry) =
                if (r.ContainsKey(oldName) && not (r.ContainsKey(newName)))
                then
                    modify (fun r -> r |> Map.add newName r[oldName] |> Map.remove oldName) r

    // ------------------------------------------------------------------------
    // public API

    // vault connection handling
    let tryAttachVault name =
        VaultRegistry.tryLoad ()
        |> Result.test (VaultRegistry.containsVault name) (UnknownVaultError name)
        |> Result.map (fun r -> r[name])
        |> Result.bind (fun path -> Vault.Attach(path) |> Result.mapError VaultError)

    let attachVault name =
        tryAttachVault name
        |> Result.defaultWith (raise << VaultManagerException)

    let detachVault name = // no error on unknown vault
        Map.tryFind name vaults
        |> Option.iter
            (fun v ->
                (v :> System.IDisposable).Dispose()
                vaults <- vaults |> Map.remove name
            )

    let getVault name =
        // cache handling
        if not <| Map.containsKey name vaults
        then
            vaults <- vaults |> Map.add name (attachVault name)

        // return vault
        vaults[name]

    // vault registry handling
    let isRegistered name =
        VaultRegistry.load ()
        |> VaultRegistry.containsVault name

    let getRegisteredVaults () =
        VaultRegistry.load ()
        |> Map.keys

    let registerVault name path =
        VaultRegistry.load ()
        |> VaultRegistry.Modify.registerVault name path

    let unregisterVault name =
        detachVault name // detach vault first

        VaultRegistry.load ()
        |> VaultRegistry.Modify.unregisterVault name

    let renameVault oldName newName =
        detachVault oldName // detach vault first

        VaultRegistry.load ()
        |> VaultRegistry.Modify.renameVault oldName newName

    // vault initialization, using default layout
    let initVault name path =
        try
            Ok (name, path)
            |> Result.test
                (fun (n, _) -> not <| isRegistered n)
                (VaultAlreadyExistsError name)
            |> Result.test
                (fun (_, p) ->
                    if not <| IO.Directory.Exists(p)
                    then
                        IO.Directory.CreateDirectory(p) |> ignore
                        true
                    else
                        IO.Directory.EnumerateFileSystemEntries(p)
                        |> Seq.isEmpty
                )
                (DirectoryNotEmptyError path)
            |> Result.teeOk
                (fun (n, p) ->
                    // create vault
                    let layout = VaultLayout.Default in
                    IO.Directory.CreateDirectory(IO.Path.Combine(p, layout.Metadata)) |> ignore
                    IO.Directory.CreateDirectory(IO.Path.Combine(p, layout.Files)) |> ignore

                    // create vault config
                    let config = { Version = Defaults.ImplementationVersion; Layout = layout } in
                    UserSettings.saveWithOptions<VaultConfig>
                        (IO.Path.Combine(p, Defaults.ConfigFilename))
                        Defaults.DefaultJsonOptions
                        config

                    // register vault
                    registerVault n p
                )
            |> Result.map (fun (n, _) -> getVault n)

        with
            | exn -> Error <| CannotInitializeVaultError exn
