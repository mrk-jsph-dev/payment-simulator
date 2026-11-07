using PaymentSimulator.Api.GraphQL;
using PaymentSimulator.Api.Payments;
using PaymentSimulator.API.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<PaymentSubmitter>();

builder.Services.AddOpenApi();
builder.Services.AddGraphQLServer()
    .AddGraphqlTypes();

await builder.Services.AddSendOnlyNServiceBus("PaymentSimulator.API");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGraphQL("/graphql");

app.Run();
