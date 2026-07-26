using Sergin.HeadEnd;
using Sergin.SharedKernel.Modules;
using Sergin.UserAccess;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("sergin-all");

IReadOnlyCollection<ISerginModule> modules = [new HeadEndModule(), new UserAccessModule()];

builder.AddSerginWebApi(modules);

WebApplication app = builder.Build();

await app.UseSerginWebApiAsync(modules);

await app.RunAsync();

public partial class Program;
