// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal class ApiClient
    {
        public string Host { get; }

        readonly HttpClient client;
        readonly JsonSerializerOptions jsonSerializerOptions;
        readonly JsonSerializerOptions jsonDeserializerOptions;
        readonly MediaTypeHeaderValue mediaType;

        public ApiClient(string hostAddress)
        {
            Host = hostAddress;

            client = new HttpClient();
            jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            jsonDeserializerOptions = new JsonSerializerOptions();

            jsonDeserializerOptions.Converters.Add(new JsonObjectConverter());

            mediaType = new MediaTypeHeaderValue("application/json");
        }

        async ValueTask<HttpResponseMessage> SendRequest(IQueryBuilder queryBuilder)
        {
            using var jsonStream = new MemoryStream();

            await JsonSerializer.SerializeAsync(jsonStream, new ApiRequest
            {
                // Pluralized entity name.
                EntityName = queryBuilder.EntityName,
                IsSelectQuery = queryBuilder.IsSelectQuery,
                SqlQuery = queryBuilder.SqlQuery.ToString(),
                SqlParameters = queryBuilder.SqlParameters
            }, jsonSerializerOptions);

            using var jsonContent = new StreamContent(jsonStream);

            // Set the media type to json.
            jsonContent.Headers.ContentType = mediaType;

            return await client.PostAsync(Host, jsonContent);
        }

        public async ValueTask<object[][]> GetResponse(IQueryBuilder queryBuilder)
        {
            using var response = await SendRequest(queryBuilder);
            using var jsonStream = await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<object[][]>(jsonStream, jsonDeserializerOptions);
        }
    }
}
