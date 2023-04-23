module OpilioCraft.Vault.Cmdlets.ExceptionExtension

type System.Exception with
    member x.ToError(errorCategory, targetObject) = System.Management.Automation.ErrorRecord(x, null, errorCategory, targetObject)
