namespace OpilioCraft.Vault

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

open OpilioCraft.FSharp
open OpilioCraft.FSharp.IO

type VaultBackend (layout: VaultLayout) =
    // content store filesystem layout
    let constructMetadataPath itemId = Path.Combine(layout.Metadata, $"{itemId}.json")
    let constructFilePath itemId ext = Path.Combine(layout.Files, $"{itemId}{ext}")

    // item id guessing
    let itemIdFromFilename (filename: string) = filename.Substring(0, filename.Length - ".json".Length)

    // serialization settings
    let jsonOptions =
        JsonSerializerOptions()
        |> tee
            (fun jsonOpts ->
                jsonOpts.WriteIndented <- true
                jsonOpts.Converters.Add(JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
                jsonOpts.Converters.Add(RelationListConverter())
            )
    
    // item access API
    abstract member ContainsItem : string -> bool
    abstract member FetchItem : string -> VaultItem
    abstract member StoreItem : VaultItem -> unit
    abstract member ForgetItem : string -> unit

    // item access implementation
    default _.ContainsItem(itemId) =
        itemId |> constructMetadataPath |> File.Exists

    default _.FetchItem(itemId) =
        try
            let json = itemId |> constructMetadataPath |> File.ReadAllText in
            JsonSerializer.Deserialize<VaultItem>(json, jsonOptions)
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot read metadata for id {itemId}: {exn.Message}"

    default _.StoreItem(item) =
        try
            saveGuard (item.Id |> constructMetadataPath)
            <| fun uri -> let itemAsJson = JsonSerializer.Serialize(item, jsonOptions) in File.WriteAllText(uri, itemAsJson)
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot write metadata for id {item.Id}: {exn.Message}"

    default _.ForgetItem(itemId) =
        try
            // be relaxed on non-existing itemId
            let pathToMedatataFile = itemId |> constructMetadataPath in
            if File.Exists pathToMedatataFile then File.Delete pathToMedatataFile
            Directory.GetFiles(layout.Files, $"{itemId}.*") |> Array.iter File.Delete
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot cleanup resources related to id {itemId}: {exn.Message}"

    // content API
    member _.ImportFile(itemId, sourcePath) =
        let fileInfo = FileInfo(sourcePath)
        let dataFile = (itemId, fileInfo.Extension.ToLower()) ||> constructFilePath

        try
            File.Copy(fileInfo.FullName, dataFile, true) // overwrite any existing file to ensure consistent data
                        
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot import file for id {id}: {exn.Message}"

    member _.ExportFile(itemId, targetPath, ?overwrite) =
        try
            // in order to optimize metadata access, we lookup the file by id only
            match Directory.GetFiles(layout.Files, $"{itemId}.*") with
            | [| file |] -> File.Copy(file, targetPath, overwrite |> Option.defaultValue false)
            | [| |] -> failwith "no file found"
            | _ -> failwith "found more than one file, vault is possibly damaged"
                        
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot export file for id {id}: {exn.Message}"

    member _.List() =
        layout.Metadata
        |> Directory.EnumerateFiles
        |> Seq.map (fun filename -> filename |> FileInfo |> _.Name |> itemIdFromFilename)
        |> Seq.toList

// ------------------------------------------------------------------------------------------------
// serialization handling
        
and RelationListConverter() =
    inherit JsonConverter<Relation list>()

    override _.Read(reader: byref<Utf8JsonReader>, _: Type, options: JsonSerializerOptions) =
        JsonSerializer.Deserialize<Relation seq>(&reader, options)
        |> List.ofSeq
    
    override _.Write(writer: Utf8JsonWriter, value: Relation list, options: JsonSerializerOptions) =
        JsonSerializer.Serialize(writer, List.toSeq value, options)
