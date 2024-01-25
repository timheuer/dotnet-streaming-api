# Streaming Examples
These are just examples of how to stream/chunk responses using ASP.NET in two ways:

- Using ASP.NET Minimal API pattern where this may be hosted in a container or other web app type hosting
- Using Azure Functions where you are streaming from an HttpTrigger function endpoint

These samples do not try to share an example of the content logic, but use OpenAI responses as an example of the format that is streamed as that is a pattern that OpenAI plugins expect and serve as a good use case.

