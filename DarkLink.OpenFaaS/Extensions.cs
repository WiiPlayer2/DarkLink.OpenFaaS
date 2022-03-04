using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DarkLink.OpenFaaS;

public static class Extensions
{
    public static IConfigurationBuilder AddOpenFaaSSecrets(this IConfigurationBuilder configurationBuilder, params (string SecretName, string ConfigName)[] mapping)
        => configurationBuilder.AddOpenFaaSSecrets(mapping.ToDictionary(o => o.SecretName, o => o.ConfigName));

    public static IConfigurationBuilder AddOpenFaaSSecrets(this IConfigurationBuilder configurationBuilder, IReadOnlyDictionary<string, string> mapping)
    {
        configurationBuilder.Add(new SecretsConfigurationSource(mapping));
        return configurationBuilder;
    }

    public static IServiceCollection AddGatewayClient(this IServiceCollection services)
        => services.AddSingleton(sp => new GatewayClient(new Uri(sp.GetRequiredService<IConfiguration>().GetValue<string>("OPENFAAS_URL"))));

    public static WebApplicationBuilder AddOpenFaaS(this WebApplicationBuilder builder, params (string SecretName, string ConfigName)[] secretsMapping)
    {
        builder.Configuration.AddOpenFaaSSecrets(secretsMapping);
        builder.Services.AddGatewayClient();
        return builder;
    }

    public static IEndpointConventionBuilder MapFunction<TInput>(this IEndpointRouteBuilder builder, FunctionObjectDelegate<TInput> function)
        => builder.MapFunction<TInput>((context, input) => Task.FromResult(function(context, input)));

    public static IEndpointConventionBuilder MapFunction<TInput>(this IEndpointRouteBuilder builder, FunctionObjectAsyncDelegate<TInput> function)
        => builder.MapFunction(async (context, requestString) =>
        {
            var input = JsonSerializer.Deserialize<TInput>(requestString);
            var output = await function(context, input);
            var responseString = JsonSerializer.Serialize(output);
            return responseString;
        });

    public static IEndpointConventionBuilder MapFunction(this IEndpointRouteBuilder builder, FunctionStringDelegate function)
        => builder.MapFunction((context, requestString) => Task.FromResult(function(context, requestString)));

    public static IEndpointConventionBuilder MapFunction(this IEndpointRouteBuilder builder, FunctionStringAsyncDelegate function)
        => builder.MapFunction(async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestString = await reader.ReadToEndAsync();

            var responseString = await function(context, requestString);
            await using var writer = new StreamWriter(context.Response.Body);
            await writer.WriteAsync(responseString);
            await writer.FlushAsync();
            await context.Response.CompleteAsync();
        });

    public static IEndpointConventionBuilder MapFunction(this IEndpointRouteBuilder builder, FunctionContextDelegate function)
        => builder.MapPost("/", async context =>
        {
            try
            {
                await function(context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                await using var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync(e.ToString());
                await writer.FlushAsync();
                await context.Response.CompleteAsync();
            }
        });
}
