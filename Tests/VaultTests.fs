namespace OpilioCraft.Vault.Tests

open System.IO
open Xunit

open OpilioCraft.FSharp.Json.UserSettings
open OpilioCraft.Vault

// digital archive structure at filesystem
type DigitalArchiveInitTests () =
    [<Fact>]
    let ``Error on non-existing archive path`` () =
        let testFunc () = Vault.Attach "X:/DOES_NOT_EXIST"
        Assert.True(testFunc() = Error(VaultNotFoundError("X:/DOES_NOT_EXIST")))

    [<Fact>]
    member _. ``Error on folder without settings file`` () =
        let testFunc () = Vault.Attach "C:/opt/Testing/xUnit/OpilioCraft.Vault/PlainFolder"
        Assert.True(testFunc() |> Result.isError)
        //Assert.True(testFunc() = Error(MissingVaultConfigError("C:/opt/Testing/xUnit/OpilioCraft.Vault/PlainFolder")))

    [<Fact>]
    member _. ``Error on folder with corrupt settings file`` () =
        let testFunc () = Vault.Attach "C:/opt/Testing/xUnit/OpilioCraft.Vault/CorruptSettingsFile"
        Assert.True(testFunc() |> Result.isError)

    [<Fact>]
    member _. ``Error on digital archive with invalid settings file and wrong version`` () =
        let testFunc () = Vault.Attach "C:/opt/Testing/xUnit/OpilioCraft.Vault/InvalidAndWrongVersion"
        Assert.True(testFunc() |> Result.isError)

    [<Fact>]
    member _. ``Error on digital archive with valid settings file and wrong version`` () =
        let testFunc () = Vault.Attach "C:/opt/Testing/xUnit/OpilioCraft.Vault/ValidButWrongVersion"
        Assert.True(testFunc() |> Result.isError)

    [<Fact>]
    member _. ``Error on digital archive with invalid settings file but correct version`` () =
        let testFunc () = Vault.Attach "C:/opt/Testing/xUnit/OpilioCraft.Vault/InvalidButCorrectVersion"
        Assert.True(testFunc() |> Result.isError)

    [<Fact>]
    member _. ``Silence on digital archive with correct settings file and correct version`` () =
        let testFunc = Vault.Attach "C:/opt/Testing/xUnit/OpilioCraft.Vault/ValidAndCorrectVersion" |> ignore; true
        Assert.True(testFunc)
