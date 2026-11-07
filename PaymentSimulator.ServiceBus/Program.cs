using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentSimulator.ServiceBus.Gateway;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();
});

builder.UseNServiceBus(context =>
{
    var endpointConfiguration = new EndpointConfiguration("PaymentSimulator.ServiceBus");

    endpointConfiguration.UseTransport<LearningTransport>();
    endpointConfiguration.UsePersistence<LearningPersistence>();
    endpointConfiguration.SendFailedMessagesTo("error");
    endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
    endpointConfiguration.EnableInstallers();

    return endpointConfiguration;
});

var host = builder.Build();
await host.RunAsync();
