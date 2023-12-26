namespace OpilioCraft.Vault.Cmdlets

open OpilioCraft.FSharp.Prelude

[<RequireQualifiedAccess>]
module Assert =
    let isValidFingerprintStrategy strategy =
        strategy            
        |> Fingerprint.tryParseStrategy
        |> Option.ifNone ( fun _ -> failwith $"not a valid fingerprint strategy: {strategy}" )
        |> Option.get
