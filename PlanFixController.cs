using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace supportHelper
{
    public static class PlanFixController
    {
        public static async IAsyncEnumerable<DirectoryEntry?> GetEntryList(HttpClient http, int id)
        {
            string fieds = "name, key, parentKey"; await foreach (var n in GetFieldsIds(http, id)) { fieds += ", " + n; }

            using var response = await http.PostAsJsonAsync($"directory/{id}/entry/list",
                new { offset = 0, pageSize = 100, fields = fieds });

            var data = await response.Content.ReadFromJsonAsync<DirectoryEntryListResponse>();

            if (data?.DirectoryEntries != null)
                foreach (var i in data.DirectoryEntries)
                    yield return i;
            yield return null;
        }

        static async IAsyncEnumerable<string?> GetFieldsIds(HttpClient http, int id)
        {
            using var resp = await http.GetAsync($"directory/{id}?fields=fields");
            DirectoryByIdResponse? r = await resp.Content.ReadFromJsonAsync<DirectoryByIdResponse>();
            if (r?.Directory?.Fields is not null)
                foreach (var f in r.Directory.Fields)
                    yield return f.Id.ToString();
            yield return null;
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
            public string Value { get; set; } = string.Empty;

            [JsonPropertyName("stringValue")]
            public string StringValue { get; set; } = string.Empty;
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
    }
}
