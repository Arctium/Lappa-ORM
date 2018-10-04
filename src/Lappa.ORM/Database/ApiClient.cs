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
        Task<HttpResponseMessage> SendRequest(string entityName, string content)
        {
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            // Combine entity name and method id.
            stringContent.Headers.Add("Entity", entityName);

            return client.PostAsync(Host, stringContent);
        }

        public async Task<object[][]> GetResponse(string entityName, string content, Func<string, object[][]> deserializeFunction)
        {
            using (var response = await SendRequest(entityName, content))
            {
                var jsonContent = await response.Content.ReadAsStringAsync();

                return deserializeFunction(jsonContent);
            }
        }
    }
}
