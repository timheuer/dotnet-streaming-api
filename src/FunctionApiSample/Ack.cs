using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedChatTypes;
using System.Text.Json;

namespace FunctionApiSample;

public class Ack
{
    private readonly ILogger _logger;

    public Ack(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Ack>();
    }

    [Function("ack")]
    public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext functionContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting streaming response");

        // get the current http context
        var httpContext = functionContext.GetHttpContext() ?? throw new InvalidOperationException("HttpContext is null");

        // ensure the response is chunked
        httpContext.Response.Headers.ContentType = "text/event-stream";

        // get the body as the expected data type and serialize to make easier to work with in C#
        var data = await req.ReadFromJsonAsync<Data>();

        // get the response stream as a writer to write to
        var streamWriter = new StreamWriter(httpContext.Response.BodyWriter.AsStream());

        // your logic to 'do stuff' here -- this is just a sample of the output
        // maybe you call openai stuff, get chat completions, and iterate through them
        // or you call your own rest endpoints, using the data\messages to query your own data store

        string responseId = Guid.NewGuid().ToString(); // this is just for this sample, you would use the response id from the openai response


        // prepare the response data to stream out in the expected openai wire format
        CompletionChunk completionChunk = new()
        {
            Model = "gpt-3.5-turbo",
            Id = responseId,
            Choices =
                [
                    new()
                    {
                        Delta = new() { Content = $"This is just a sample from Azure Functions\n\nThe info you provided was: {data.messages?.LastOrDefault(m=>m.Role == "user")?.Content}" },
                        FinishReason = null
                    }
            ]
        };

        // each chunk is a line of text, followed by a blank line
        string dataLine = $"data: {JsonSerializer.Serialize(completionChunk)}";
        await streamWriter.WriteLineAsync(dataLine);
        await streamWriter.WriteLineAsync();
        await streamWriter.FlushAsync();

        // write stop line
        // the end of your data stream needs to contain a data line with finish_reason = stop
        // this tells the client that the stream is done
        // if using the openai api, you would get this from the response and likely part of your iteration
        // this is just a sample ensuring we are writing that
        CompletionChunk stopChunk = new()
        {
            Model = "gpt-3.5-turbo",
            Id = responseId,
            Choices =
                [
                    new()
                    {
                        Delta = new() { Content = null },
                        FinishReason = "stop"
                    }
            ]
        };

        // each chunk is a line of text, followed by a blank line
        string stopLine = $"data: {JsonSerializer.Serialize(stopChunk)}";
        await streamWriter.WriteLineAsync(stopLine);
        await streamWriter.WriteLineAsync();
        await streamWriter.FlushAsync();

        // write done line when completely done
        await streamWriter.WriteLineAsync("data: [DONE]");
        await streamWriter.WriteLineAsync();
        await streamWriter.FlushAsync();

        _logger.LogInformation("Streaming done.");
    }
}
