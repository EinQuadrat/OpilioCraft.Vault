namespace OpilioCraft.Vault.Cmdlets

open System
open OpilioCraft.FSharp.Prelude

[<RequireQualifiedAccess>]
module Assert =
    let fileExists errorMessage path =
        if not <| IO.File.Exists path then failwith $"{errorMessage}: path"
        path

    let isValidFingerprintStrategy strategy =
        strategy            
        |> Fingerprint.tryParseStrategy
        |> Option.ifNone ( fun _ -> failwith $"not a valid fingerprint strategy: {strategy}" )
        |> Option.get
