using Scalar.AspNetCore;
using Sergin.HeadEnd;
using Sergin.SharedKernel.Application.Commands;
using Sergin.SharedKernel.Application.Events;
using Sergin.SharedKernel.Application.Localizations;
using Sergin.SharedKernel.Application.Securities.Authorization;
using Sergin.SharedKernel.Application.Securities.Users;
using Sergin.SharedKernel.Infrastracture.Data;
using Sergin.SharedKernel.Infrastracture.WebApi.Users;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;
using Sergin.SharedKernel.Infrastructure.Data.EFCore.Interceptors;
using Sergin.SharedKernel.Infrastructure.Events;
using Sergin.SharedKernel.Infrastructure.Localizations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("sergin-all");

IConfigurationSection serginSection = builder.Configuration.GetRequiredSection("Sergin");

builder.Services.AddMediatR(
    options =>
    {
        options.RegisterHeadEndCommands();

        options.AddOpenBehavior(typeof(PermissionCheckPipelineBehavior<,>));
        options.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
    });

builder.Services.AddOpenApi();

builder.Services.AddScoped<IEventDispatcher, DefaultEventDispatcher>();
builder.Services.AddScoped<EventDispatcherInterceptor>();

builder.Services.AddScoped<IDbConnectionFactory>(p => new PostgresDbConnectionFactory(serginSection.GetConnectionString("Database")!));

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IUserContextFactory, InternalUserContextFactory>();
builder.Services.AddScoped(p => p.GetRequiredService<IUserContextFactory>().CreateUserContext());

builder.Services.AddSingleton<ILocalizer, DefaultLocalizer>();

builder.Services.AddHeadEndModule(serginSection);

WebApplication app = builder.Build();

await app.RunHeadEndModule();

app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

await app.RunAsync();
