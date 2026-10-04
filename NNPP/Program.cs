using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using NNPP;
using NNPP.Reactor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddSingleton<Simulation>();
builder.Services.AddSingleton(sp => new AudioManager(sp.GetRequiredService<IJSRuntime>())
    .Register(AudioKeys.Sfx.Notification, "audio/notification.ogg")
    .Register(AudioKeys.Sfx.NotificationCritical, "audio/notificationcritical.ogg")
    .Register(AudioKeys.Sfx.MetalCry, "audio/metalcry.ogg")
    .Register(AudioKeys.Sfx.ReactorExplosion, "audio/reactorexplosion.ogg")
    .Register(AudioKeys.Sfx.AnnouncerMeltdown, "audio/announcermeltdown.ogg")
    .Register(AudioKeys.Sfx.MeltdownAlarm, "audio/meltdownalarm.ogg")
    .Register(AudioKeys.Sfx.ScramActive, "audio/scramactive.ogg")
    
    .Register(AudioKeys.Music.Overheat, "audio/overheat.ogg")
    .Register(AudioKeys.Music.Meltdown, "audio/meltdown.ogg")
    .Register(AudioKeys.Music.Evacuate, "audio/evacuate.ogg")
    .Register(AudioKeys.Music.Shutdown, "audio/shutdown.ogg")
);

var app = builder.Build();
app.Services.GetRequiredService<Simulation>().Start();
await app.RunAsync();