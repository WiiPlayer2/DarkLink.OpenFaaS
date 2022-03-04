using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarkLink.OpenFaaS
{
    public class GatewayClient
    {
        private const string HEADER_X_CALLBACK_URL = "X-Callback-Url";

        private const string HEADER_X_CALL_ID = "X-Call-Id";

        private static readonly Encoding utf8 = new UTF8Encoding(false);

        private readonly HttpClient httpClient;

        public GatewayClient(Uri url)
        {
            httpClient = new HttpClient
            {
                BaseAddress = url,
            };
        }

        private Task<HttpResponseMessage> SendAsync(bool isAsync, string function, HttpContent content, CancellationToken cancellationToken)
        {
            var basePath = isAsync ? "async-function" : "function";
            return httpClient.PostAsync($"/{basePath}/{function}", content, cancellationToken);
        }

        public async Task<Guid> CallAsync(string function, string? arg, string? callbackUrl = default, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(arg ?? string.Empty, utf8);
            if (callbackUrl is not null)
                content.Headers.Add(HEADER_X_CALLBACK_URL, callbackUrl);
            var response = await SendAsync(true, function, content, cancellationToken);
            var callId = Guid.Parse(response.Headers.GetValues(HEADER_X_CALL_ID).First());
            return callId;
        }

        public Task<Guid> CallAsync(string function, object? arg, string? callbackUrl = default, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default)
            => CallAsync(function, JsonSerializer.Serialize(arg, jsonOptions), callbackUrl, cancellationToken);

        public async Task<Stream> InvokeRawAsync(string function, string? arg, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(false, function, new StringContent(arg ?? string.Empty, utf8), cancellationToken);
            var responseStream = response.Content.ReadAsStreamAsync(cancellationToken);
            return await responseStream;
        }

        public async Task<string> InvokeAsync(string function, string? arg, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(false, function, new StringContent(arg ?? string.Empty, utf8), cancellationToken);
            var responseString = response.Content.ReadAsStringAsync(cancellationToken);
            return await responseString;
        }

        public async Task<TOutput?> InvokeAsync<TOutput>(string function, object? arg, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default)
        {
            var inputString = JsonSerializer.Serialize(arg, jsonOptions);
            var responseStream = await InvokeRawAsync(function, inputString, cancellationToken);
            var outputObject = await JsonSerializer.DeserializeAsync<TOutput>(responseStream, jsonOptions, cancellationToken);
            return outputObject;
        }
    }
}
