namespace OpilioCraft.Vault.Cmdlets

open System.Management.Automation
open OpilioCraft.FSharp.Prelude
open ExceptionExtension

// base class for all cmdlets, providing some helper functionality
[<AbstractClass>]
type public CommandBase () =
    inherit PSCmdlet ()

    // path helpers
    member x.ResolvePath (path : string) =
        fst <| x.GetResolvedProviderPathFromPSPath path
 
    // omit warnings
    member x.WarnIfNone warning =
        Option.ifNone ( fun _ -> x.WriteWarning warning )

    member x.WarnIfFalse warning input =
        if not input then x.WriteWarning warning
        input

    // simplify error handling
    member x.WriteAsError errorCategory (exn : #System.Exception) : unit =
        exn.ToError(errorCategory, x)
        |> x.WriteError

    member x.ThrowAsTerminatingError errorCategory (exn : #System.Exception) =
        exn.ToError(errorCategory, x)
        |> x.ThrowTerminatingError

    member x.ThrowValidationError errorMessage errorCategory =
        errorMessage
        |> ParameterBindingException
        |> x.ThrowAsTerminatingError errorCategory
