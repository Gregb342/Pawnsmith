// A.6 — The API serves the compiled front from wwwroot. Same origin, so there is
// deliberately no CORS configuration here.
//
// Nothing else belongs in this file yet: no endpoint, no dependency injection,
// no business logic. Those arrive with T6.
//
// Types are spelled out rather than inferred: the .editorconfig var policy of A.4
// only allows `var` where the type is apparent on the right-hand side, which is
// not the case for a factory call.

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Single-page application: any unknown path is handed back to index.html so the
// client-side router, when there is one, can resolve it.
app.MapFallbackToFile("index.html");

app.Run();
