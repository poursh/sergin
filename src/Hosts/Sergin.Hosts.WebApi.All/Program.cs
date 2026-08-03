using Sergin.MeterMinder;
using Sergin.SharedKernel.Modules;
using Sergin.UserAccess;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("sergin-all");

IReadOnlyCollection<ISerginModule> modules = [new MeterMinderModule(), new UserAccessModule()];

builder.AddSerginWebApi(modules);

WebApplication app = builder.Build();

await app.UseSerginWebApiAsync(modules);

await app.RunAsync();

public partial class Program;
