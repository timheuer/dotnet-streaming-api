using SharedChatTypes;
using System.IO;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/ack", async (Data data) =>
{
    // stream the response
    async Task StreamContentUpdatesAsync(Stream stream)
    {
        var streamWriter = new StreamWriter(stream);

        string responseId = Guid.NewGuid().ToString(); // this is just for this sample, you would use the response id from the openai response

        // your logic to 'do stuff' here -- this is just a sample of the output
        // maybe you call openai stuff, get chat completions, and iterate through them
        // or you call your own rest endpoints, using the data\messages to query your own data store

        // prepare the response data to stream out in the expected openai wire format
        CompletionChunk completionChunk = new()
        {
            Model = "gpt-3.5-turbo",
            Id = responseId,
            Choices =
                [
                    new()
                    {
                        Delta = new() { Content = $"This is just a sample from ASP.NET Minimal API hosted on a web app\n\nThe info you provided was: {data.messages?.LastOrDefault(m=>m.Role == "user")?.Content}" },
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
        await streamWriter.WriteLineAsync(string.Empty);
        await streamWriter.FlushAsync();
    }

    return Results.Stream(StreamContentUpdatesAsync, "text/event-stream");
})
.WithName("ack")
.WithOpenApi();

app.UseHttpsRedirection();

app.Run();
