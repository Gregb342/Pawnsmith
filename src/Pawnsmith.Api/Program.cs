// A.6 and T6 - the API, and the compiled front it serves from wwwroot. Same
// origin, so there is deliberately no CORS configuration.
//
// Everything is built in ApiHost, so that the tests start the very same host
// (section G.13). This file only runs it.

using Pawnsmith.Api.Hosting;

WebApplication app = await ApiHost.BuildAsync(args);

await app.RunAsync();
