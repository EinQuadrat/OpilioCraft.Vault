namespace OpilioCraft.Vault.Tests

open System.IO
open Xunit

open OpilioCraft.FSharp.Json.UserSettings
open OpilioCraft.Vault

// digital archive structure at filesystem
type DigitalArchiveInitTests () =
    [<Fact>]
    member _. ``Exception on non-existing archive path`` () =
        let testFunc () = VaultHandler.Init "X:/DOES_NOT_EXIST" |> ignore
        Assert.Throws<DirectoryNotFoundException>(testFunc)

    [<Fact>]
    member _. ``Exception on folder without settings file`` () =
        let testFunc () = VaultHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.Vault/PlainFolder" |> ignore
        Assert.Throws<MissingVaultSettingsFileException>(testFunc)

    [<Fact>]
    member _. ``Exception on folder with corrupt settings file`` () =
        let testFunc () = VaultHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.Vault/CorruptSettingsFile" |> ignore
        Assert.Throws<UserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Exception on digital archive with invalid settings file and wrong version`` () =
        let testFunc () = VaultHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.Vault/InvalidAndWrongVersion" |> ignore
        Assert.Throws<UserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Exception on digital archive with valid settings file and wrong version`` () =
        let testFunc () = VaultHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.Vault/ValidButWrongVersion" |> ignore
        Assert.Throws<UserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Exception on digital archive with invalid settings file but correct version`` () =
        let testFunc () = VaultHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.Vault/InvalidButCorrectVersion" |> ignore
        Assert.Throws<UserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Silence on digital archive with correct settings file and correct version`` () =
        let testFunc = VaultHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.Vault/ValidAndCorrectVersion" |> ignore; true
        Assert.True(testFunc)
