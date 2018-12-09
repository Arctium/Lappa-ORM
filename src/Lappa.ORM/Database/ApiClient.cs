// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
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

        // sql query
        Task<HttpResponseMessage> SendRequest(IQueryBuilder queryBuilder, Func<object, string> serializeFunction)
        {
            var serializedRequest = serializeFunction(new ApiRequest
            {
                // Pluralized entity name.
                EntityName = queryBuilder.EntityName,
                IsSelectQuery = queryBuilder.IsSelectQuery,
                SqlQuery = queryBuilder.SqlQuery.ToString(),
                SqlParameters = queryBuilder.SqlParameters
            });

            var stringContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");

            return client.PostAsync(Host, stringContent);
        }

        public async Task<object[][]> GetResponse(IQueryBuilder queryBuilder, Func<object, string> serializeFunction, Func<string, object[][]> deserializeFunction)
        {
            using (var response = await SendRequest(queryBuilder, serializeFunction))
            {
                var jsonContent = await response.Content.ReadAsStringAsync();

                return deserializeFunction(jsonContent);
            }
        }
    }
}
