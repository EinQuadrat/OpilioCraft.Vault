namespace OpilioCraft.Vault.Cmdlets

open System
open System.Management.Automation

open OpilioCraft.Vault.Core

module private RelationHelper =
    let tryParseRelationType (input : string) : RelationType option =
        match System.Enum.TryParse<RelationType>(input, true) with
        | true, value -> Some value
        | _ -> None

    let parseRelationType input =
        tryParseRelationType input
        |> Option.defaultWith (fun _ -> failwith $"not a valid relation type: {input}")


// ------------------------------------------------------------------------------------------------

[<Cmdlet(VerbsCommon.Get, "Relation")>]
[<OutputType(typeof<Relation list>)>]
type public GetRelationCommand () =
    inherit VaultItemCommand ()

    // cmdlet behaviour
    override x.ProcessPath path =
        x.GetIdOfManagedItem path // throws exception on unmanaged item
        |> x.ActiveVault.Fetch
        |> fun metadata -> metadata.Relations
        |> x.WriteObject


// ------------------------------------------------------------------------------------------------

type private RelationContext =
    {
        Target : ItemId
        RelationType : RelationTypeParam
    }

    static member Init(x : VaultItemCommand, target : string, relType : string) =
        let targetId = x.GetIdOfManagedItem <| x.GetUnresolvedProviderPathFromPSPath target

        let relTypeValue : RelationTypeParam =
            match relType with
            | empty when empty = String.Empty -> NotSpecified
            | value -> RelationHelper.parseRelationType value |> Value

        { Target = targetId; RelationType = relTypeValue }

    static member Default = { Target = String.Empty; RelationType = NotSpecified }

and RelationTypeParam =
    | NotSpecified
    | Value of RelationType

// ------------------------------------------------------------------------------------------------

[<Cmdlet(VerbsCommon.Set, "Relation")>]
[<OutputType(typeof<Void>)>]
type public SetRelationCommand () =
    inherit VaultItemCommand ()

    // processed params
    let mutable context = RelationContext.Default

    // params
    [<Parameter(Mandatory=true)>]
    [<ValidateNotNullOrEmpty>]
    member val Target = String.Empty with get, set

    [<Parameter>]
    [<ValidateNotNullOrEmpty>]
    member val RelationType = String.Empty with get, set

    // cmdlet behaviour
    override x.BeginProcessing() =
        base.BeginProcessing()
        context <- RelationContext.Init(x, x.Target, x.RelationType)
    
    override x.ProcessPath path =
        let itemId = x.GetIdOfManagedItem path
        let relType = match context.RelationType with | Value relType -> relType | _ -> RelationType.Related

        x.ActiveVault |> VaultOperations.updateVaultItem
            itemId
            (VaultOperations.Relations.add { Target = context.Target; IsA = relType })

// ------------------------------------------------------------------------------------------------

[<Cmdlet(VerbsCommon.Remove, "Relation")>]
[<OutputType(typeof<Void>)>]
type public RemoveRelationCommand () =
    inherit VaultItemCommand ()

    // processed params
    let mutable context = RelationContext.Default

    // params
    [<Parameter(Mandatory=true, ParameterSetName="Path")>]
    [<Parameter(Mandatory=true, ParameterSetName="LiteralPath")>]
    [<ValidateNotNullOrEmpty>]
    member val Target = String.Empty with get, set

    [<Parameter(ParameterSetName="Path")>]
    [<Parameter(ParameterSetName="LiteralPath")>]
    [<ValidateNotNullOrEmpty>]
    member val RelationType = String.Empty with get, set

    [<Parameter(ParameterSetName="Path_1")>]
    [<Parameter(ParameterSetName="LiteralPath_1")>]
    member val All = SwitchParameter(false) with get, set

    // cmdlet behaviour
    override x.BeginProcessing() =
        base.BeginProcessing()

        if not <| x.All.IsPresent
        then
            context <- RelationContext.Init(x, x.Target, x.RelationType)

    override x.ProcessPath path =
        let itemId = x.GetIdOfManagedItem path

        if x.All.IsPresent
        then
            x.ActiveVault |> VaultOperations.updateVaultItem itemId VaultOperations.Relations.removeAll
        else
            let updateAction =
                match context.RelationType with
                | Value relType -> VaultOperations.Relations.remove { Target = context.Target; IsA = relType }
                | NotSpecified -> VaultOperations.Relations.removeRelationsTo context.Target

            x.ActiveVault |> VaultOperations.updateVaultItem itemId updateAction
