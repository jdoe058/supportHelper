using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace supportHelper;

public static class PlanFixController
{
    private static readonly HttpClient client = new() { BaseAddress = new Uri("https://zheka003.planfix.ru/rest/") };

    static PlanFixController()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "84466e5d2c61f49fe2bc068fda942283");
    }

    public static async void DeleteDirectoryEntry(int id, ConnectionModel model)
    {
        using var response = await client.DeleteAsync($"directory/{id}/entry/{model.key}");
        var data = await response.Content.ReadFromJsonAsync<Response>();

        if (response.StatusCode == HttpStatusCode.OK && data?.Result == "success")
            MainWindowViewModel.ConnectionsList.Remove(model);
    }

    public static async void UpdateDirectoryEntry(int id, DirectoryEntry entry, bool add = false)
    {
        var method = $"directory/{id}/entry/";
        if (!add) method += entry.Key;


        using var response = await client.PostAsJsonAsync(method,entry); 
        var data = await response.Content.ReadFromJsonAsync<Response>();

        if (response.StatusCode == HttpStatusCode.OK && data?.Result == "success" )
        {
            if (data?.Key is not null)
            {
                entry.Key = (int)data.Key;
                MainWindowViewModel.ConnectionsList.Add(new ConnectionModel(entry));
            }
        }
    }
    
    public static async IAsyncEnumerable<DirectoryEntry?> GetEntryList(int id)
    {
        string fieds = "name, key, parentKey"; await foreach (var n in GetFieldsIds(id)) { fieds += ", " + n; }

        using var response = await client.PostAsJsonAsync($"directory/{id}/entry/list",
            new { offset = 0, pageSize = 100, fields = fieds });

        var data = await response.Content.ReadFromJsonAsync<DirectoryEntryListResponse>();
       
        if (data?.DirectoryEntries is not null)
            foreach (var i in data.DirectoryEntries)
                yield return i;
    }

    static async IAsyncEnumerable<string?> GetFieldsIds(int id)
    {
        using var resp = await client.GetAsync($"directory/{id}?fields=fields");
        DirectoryByIdResponse? r = await resp.Content.ReadFromJsonAsync<DirectoryByIdResponse>();
        if (r?.Directory?.Fields is not null)
            foreach (var f in r.Directory.Fields)
                yield return f.Id.ToString();
    }


    public class Field
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public int Type { get; set; } = 0;

        [JsonPropertyName("objectType")]
        public int ObjectType { get; set; } = 0;
    }

    public class CustomFieldDatum
    {
        [JsonPropertyName("field")]
        public Field? Field { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("stringValue")]
        public string? StringValue { get; set; }
    }

    public class Directory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("fields")]
        public List<Field>? Fields { get; set; }
    }

    public class DirectoryEntry
    {
        [JsonPropertyName("key")]
        public int Key { get; set; }

        [JsonPropertyName("parentKey")]
        public int ParentKey { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("customFieldData")]
        public List<CustomFieldDatum>? CustomFieldData { get; set; }
    }

    class DirectoryEntryListResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyName("directoryEntries")]
        public List<DirectoryEntry>? DirectoryEntries { get; set; }
    }

    class DirectoryByIdResponse
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyName("directory")]
        public Directory? Directory { get; set; }
    }

    class Response
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("key")]
        public int? Key { get; set; }

        [JsonPropertyName("directory")]
        public Directory? Directory { get; set; }

        [JsonPropertyName("directoryEntries")]
        public List<DirectoryEntry>? DirectoryEntries { get; set; }
    }
}
