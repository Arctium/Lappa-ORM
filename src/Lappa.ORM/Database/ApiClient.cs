// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
        }

        Task<HttpResponseMessage> SendRequest(IQueryBuilder queryBuilder, bool isSelectQuery, (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) query)
        {
            var serializedRequest = JsonSerializer.Serialize(new ApiRequest(queryBuilder.EntityName, isSelectQuery, query.SqlQuery.ToString(), query.SqlParameters), jsonSerializerOptions);

            var stringContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");

            return client.PostAsync(Host, stringContent);
        }

        public async ValueTask<object[][]> GetResponse(IQueryBuilder queryBuilder, bool isSelectQuery, (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) query)
        {
            using var response = await SendRequest(queryBuilder, isSelectQuery, query);
            using var jsonStream = await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<object[][]>(jsonStream, jsonDeserializerOptions);
        }
    }
}
