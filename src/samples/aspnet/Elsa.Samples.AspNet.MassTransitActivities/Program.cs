using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.AspNet.MassTransitActivities.Messages;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management =>
    {
        management.UseWorkflowDefinitions(dm => dm.UseEntityFrameworkCore());
        management.UseWorkflowInstances(w => w.UseEntityFrameworkCore());
    });
    
    // Configure runtime feature to use EF Core.
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
    });
    
    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp();
    
    // Use JavaScript and Liquid.
    elsa.UseJavaScript();
    elsa.UseLiquid();
    
    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });
    
    // Use default authentication (JWT).
    elsa.UseDefaultAuthentication();
    
    // Configure MassTransit.
    elsa.UseMassTransit(massTransit =>
    {
        massTransit.UseRabbitMq(rabbitMqConnectionString);
        massTransit.AddMessageType<OrderCompleted>();
        massTransit.AddMessageType<OrderCreated>();
    });
});

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();