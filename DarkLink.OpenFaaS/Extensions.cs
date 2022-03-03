using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DarkLink.OpenFaaS;

public static class Extensions
{
    public static IEndpointConventionBuilder MapFunction<TInput>(this IEndpointRouteBuilder builder, FunctionObjectDelegate<TInput> function)
        => builder.MapFunction(async (context, requestString) =>
        {
            var input = JsonSerializer.Deserialize<TInput>(requestString);
            var output = await function(context, input);
            var responseString = JsonSerializer.Serialize(output);
            return responseString;
        });

    public static IEndpointConventionBuilder MapFunction(this IEndpointRouteBuilder builder, FunctionStringDelegate function)
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

    public static IEndpointConventionBuilder MapFunction(this IEndpointRouteBuilder builder, Delegates function)
        => builder.MapPost("/", async context =>
        {
            try
            {
                await function(context);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await using var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync(e.ToString());
                await writer.FlushAsync();
                await context.Response.CompleteAsync();
            }
        });
}
