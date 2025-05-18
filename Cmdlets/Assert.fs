namespace OpilioCraft.Vault.Cmdlets

open OpilioCraft.FSharp

[<RequireQualifiedAccess>]
module Assert =
    let isValidFingerprintStrategy strategy =
        strategy            
        |> Fingerprint.tryParseStrategy
        |> Option.ifNone (fun _ -> failwith $"not a valid fingerprint strategy: {strategy}")
        |> Option.get
