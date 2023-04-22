namespace OpilioCraft.Vault.Core

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

open OpilioCraft.FSharp.Prelude

type VaultBackend (root : string, layout : VaultLayout) =
    // content store filesystem layout
    let (>+>) path childPath = Path.Combine(path, childPath)

    let processPath (path : string) = if Path.IsPathRooted(path) then path else root >+> path

    let metadataSection = processPath layout.Metadata
    let constructMetadataPath (id : ItemId) = metadataSection >+> $"{id}.json"
    
    let filesSection = processPath layout.Files
    let constructFilePath itemId ext = filesSection >+> $"{itemId}{ext}"
    let constructFilePathFromItem (item : VaultItem) = (item.Id, item.ContentType.FileExtension) ||> constructFilePath
    
    let itemIdFromFilename (filename : string) = filename.Substring(0, filename.Length - ".json".Length)

    // serialization settings
    let jsonOptions =
        JsonSerializerOptions() |> fun jsonOpts ->
            jsonOpts.WriteIndented <- true
            jsonOpts.Converters.Add(JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
            jsonOpts.Converters.Add(RelationListConverter())
            jsonOpts.Converters.Add(DetailsConverter())
            jsonOpts
    
    // archive access
    member _.ContainsItem itemId =
        itemId |> constructMetadataPath |> File.Exists

    member _.FetchItem itemId =
        try
            let json = itemId |> constructMetadataPath |> File.ReadAllText in
            JsonSerializer.Deserialize<VaultItem>(json, jsonOptions)
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot read metadata for id {itemId}: {exn.Message}"

    member _.StoreItem item =
        try
            IO.saveGuard (item.Id |> constructMetadataPath)
            <| fun uri -> let itemAsJson = JsonSerializer.Serialize(item, jsonOptions) in File.WriteAllText(uri, itemAsJson)
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot write metadata for id {item.Id}: {exn.Message}"

    member _.ForgetItem itemId =
        try
            // be relaxed on non-existing itemId
            let pathToMedatataFile = itemId |> constructMetadataPath in
            if File.Exists pathToMedatataFile then File.Delete pathToMedatataFile
            Directory.GetFiles(filesSection, $"{itemId}.*") |> Array.iter File.Delete
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot cleanup resources related to id {itemId}: {exn.Message}"

    member _.ImportFile itemId sourcePath =
        let fileInfo = FileInfo(sourcePath)
        let dataFile = (itemId, fileInfo.Extension) ||> constructFilePath

        try
            File.Copy(fileInfo.FullName, dataFile, true) // overwrite any existing file to ensure consistent data
                        
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot import file for id {id}: {exn.Message}"

    member _.ExportFile itemId targetPath overwrite =
        try
            // in order to optimize metadata access, we lookup the file by id only
            match Directory.GetFiles(filesSection, $"{itemId}.*") with
            | [| file |] -> File.Copy(file, targetPath, overwrite)
            | [| |] -> failwith "no file found"
            | _ -> failwith "found more than one file, vault is possibly damaged"
                        
        with
            | exn -> failwith $"[{nameof VaultBackend}] cannot export file for id {id}: {exn.Message}"

    member _.List () =
        metadataSection
        |> Directory.EnumerateFiles
        |> Seq.map (fun filename -> filename |> FileInfo |> (fun fi -> fi.Name |> itemIdFromFilename))
        |> Seq.toList

// ------------------------------------------------------------------------------------------------
// serialization handling

and DetailsConverter() =
    inherit JsonConverter<ItemDetail>()

    [<Literal>] static let DateTimePrefix = "#DateTime#"
    [<Literal>] static let DateTimeOffset = 11 // length of prefix + 1 for trailing whitespace
    [<Literal>] static let TimeSpanPrefix = "#TimeSpan#"
    [<Literal>] static let TimeSpanOffset = 11
    [<Literal>] static let FloatPrefix    = "#Float#"
    [<Literal>] static let FloatOffset    = 8

    override _.Read (reader: byref<Utf8JsonReader>, _: Type, _: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.False
        | JsonTokenType.True -> reader.GetBoolean() |> ItemDetail.Boolean

        | JsonTokenType.Number -> reader.GetDecimal() |> ItemDetail.Number

        | JsonTokenType.String ->
            // smart string handling
            reader.GetString()
            |> function
                | s when s.StartsWith(DateTimePrefix) -> s.Substring(DateTimeOffset) |> DateTime.Parse |> ItemDetail.DateTime
                | s when s.StartsWith(TimeSpanPrefix) -> s.Substring(TimeSpanOffset) |> TimeSpan.Parse |> ItemDetail.TimeSpan
                | s when s.StartsWith(FloatPrefix) -> s.Substring(FloatOffset) |> Double.Parse |> ItemDetail.Float
                | s -> s |> ItemDetail.String

        | _ -> failwith $"[{nameof VaultBackend}] unexpected data type while deserializing"
    
    override _.Write (writer: Utf8JsonWriter, value: ItemDetail, options: JsonSerializerOptions) =
        let dateTimeFormat = @"yyyy-MM-ddTHH\:mm\:sszzz"
        
        match value with
        | Boolean x -> writer.WriteBooleanValue x
        | Number x -> writer.WriteNumberValue x
        | String x -> writer.WriteStringValue x

        // not natively supported value types
        | DateTime x -> writer.WriteStringValue $"{DateTimePrefix} {x.ToString(dateTimeFormat)}"
        | Float x -> writer.WriteStringValue $"{FloatPrefix} {x.ToString()}" // preserve precision of number value by string wrapper
        | TimeSpan x -> writer.WriteStringValue $"{TimeSpanPrefix} {x.ToString()}"
        
and RelationListConverter() =
    inherit JsonConverter<Relation list>()

    override _.Read (reader: byref<Utf8JsonReader>, _: Type, options: JsonSerializerOptions) =
        JsonSerializer.Deserialize<Relation seq>(&reader, options)
        |> List.ofSeq
    
    override _.Write (writer: Utf8JsonWriter, value: Relation list, options: JsonSerializerOptions) =
        JsonSerializer.Serialize(writer, List.toSeq value, options)
