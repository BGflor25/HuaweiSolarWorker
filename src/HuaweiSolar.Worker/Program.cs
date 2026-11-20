using HuaweiSolar.Worker;
using HuaweiSolar.Worker.Api;
using HuaweiSolar.Worker.Huawei;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<InverterOptions>()
    .Bind(builder.Configuration.GetSection(InverterOptions.SectionName))
    .Validate(o => o.ApiUrl is not null, "Inverter:ApiUrl is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "Inverter:Host is required")
    .Validate(o => o.Port is > 0 and <= 65535, "Inverter:Port must be between 1 and 65535")
    .Validate(o => o.Interval > TimeSpan.Zero, "Inverter:Interval must be positive")
    .Validate(o => o.RequestTimeout > TimeSpan.Zero, "Inverter:RequestTimeout must be positive")
    .ValidateOnStart();

builder.Services.AddSingleton<IInverterReader, InverterReader>();

builder.Services.AddHttpClient<PvApiClient>()
    .AddStandardResilienceHandler();

builder.Services.AddHostedService<InverterWorker>();

builder.Build().Run();
