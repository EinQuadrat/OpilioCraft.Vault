namespace OpilioCraft.DigitalArchive.Tests

open Xunit

open OpilioCraft.FSharp.Prelude
open OpilioCraft.DigitalArchive.Core

// digital archive structure at filesystem
type DigitalArchiveInitTests () =
    [<Fact>]
    member _. ``Exception on non-existing archive path`` () =
        let testFunc () = ArchiveHandler.Init "X:/DOES_NOT_EXIST" |> ignore
        Assert.Throws<InvalidArchivePathException>(testFunc)

    [<Fact>]
    member _. ``Exception on folder without settings file`` () =
        let testFunc () = ArchiveHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.DigitalArchive/PlainFolder" |> ignore
        Assert.Throws<MissingArchiveSettingsFileException>(testFunc)

    [<Fact>]
    member _. ``Exception on folder without corrupt settings file`` () =
        let testFunc () = ArchiveHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.DigitalArchive/CorruptSettingsFile" |> ignore
        Assert.Throws<InvalidUserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Exception on digital archive with invalid settings file and wrong version`` () =
        let testFunc () = ArchiveHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.DigitalArchive/InvalidAndWrongVersion" |> ignore
        Assert.Throws<InvalidUserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Exception on digital archive with valid settings file and wrong version`` () =
        let testFunc () = ArchiveHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.DigitalArchive/ValidButWrongVersion" |> ignore
        Assert.Throws<IncompatibleVersionException>(testFunc)

    [<Fact>]
    member _. ``Exception on digital archive with invalid settings file but correct version`` () =
        let testFunc () = ArchiveHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.DigitalArchive/InvalidButCorrectVersion" |> ignore
        Assert.Throws<InvalidUserSettingsException>(testFunc)

    [<Fact>]
    member _. ``Silence on digital archive with correct settings file and correct version`` () =
        let testFunc = ArchiveHandler.Init "C:/opt/Testing/xUnit/OpilioCraft.DigitalArchive/ValidAndCorrectVersion" |> ignore; true
        Assert.True(testFunc)
