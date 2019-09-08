// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lappa.ORM
{
    internal class ApiClient
    {
        public string Host { get; }

        readonly HttpClient client;

        public ApiClient(string hostAddress)
        {
            Host = hostAddress;

            client = new HttpClient();
        }

        Task<HttpResponseMessage> SendRequest(IQueryBuilder queryBuilder)
        {
            var serializedRequest = JsonSerializer.Serialize(new ApiRequest
            {
                // Pluralized entity name.
                EntityName = queryBuilder.EntityName,
                IsSelectQuery = queryBuilder.IsSelectQuery,
                SqlQuery = queryBuilder.SqlQuery.ToString(),
                SqlParameters = queryBuilder.SqlParameters
            }, new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

            var stringContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");

            return client.PostAsync(Host, stringContent);
        }

        public async Task<object[][]> GetResponse(IQueryBuilder queryBuilder)
        {
            using var response = await SendRequest(queryBuilder);
            using var jsonStream = await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<object[][]>(jsonStream);
        }
    }
}
