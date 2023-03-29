using ConfigDataProtectionSample;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args is not [_, var key, var value])
{
    Console.WriteLine("Provide a config value in the format:");
    Console.WriteLine($"ConfigCreator.exe section:key value");
    return;
}

await Host.CreateDefaultBuilder(args)
    //.ConfigureAppConfiguration((hostContext, config) =>
    //{

    //})
    .ConfigureServices((hostContext, services) =>
    {
        services.AddDataProtection(dpo =>
        {
            dpo.ApplicationDiscriminator = nameof(ConfigDataProtectionSample);
        });

        services.AddHostedService(provider => new Worker(provider.GetRequiredService<IDataProtectionProvider>(), key, value));
    })
    .RunConsoleAsync();

internal class Worker : BackgroundService
{
    private readonly string _key;
    private readonly string _value;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public Worker(IDataProtectionProvider dataProtectionProvider, string key, string value)
        => (_dataProtectionProvider, _key, _value) = (dataProtectionProvider, key, value);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var protector = _dataProtectionProvider.CreateProtector(_key);
        Console.WriteLine(protector.Protect(_value));

        Environment.Exit(0);

        return Task.CompletedTask;
    }
}