// Empty host. No endpoint, no dependency injection, no business logic: those
// arrive with T6.
//
// Types are spelled out rather than inferred: the .editorconfig var policy of
// A.4 only allows `var` where the type is apparent on the right-hand side,
// which is not the case for a factory call.

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.Run();
