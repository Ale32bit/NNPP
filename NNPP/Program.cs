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
builder.Services.AddSingleton<PersistentStorage>();
builder.Services.AddSingleton(sp => new AudioManager(sp.GetRequiredService<IJSRuntime>())
    .Register(AudioKeys.Sfx.Notification, "audio/notification.ogg")
    .Register(AudioKeys.Sfx.NotificationCritical, "audio/notificationcritical.ogg")
    .Register(AudioKeys.Sfx.MetalCry, "audio/metalcry.ogg")
    .Register(AudioKeys.Sfx.ReactorExplosion, "audio/reactorexplosion.ogg")
    .Register(AudioKeys.Sfx.AnnouncerMeltdown, "audio/announcermeltdown.ogg")
    .Register(AudioKeys.Sfx.MeltdownAlarm, "audio/meltdownalarm.ogg")
    .Register(AudioKeys.Sfx.MeltdownExplosion, "audio/meltdownexplosion.ogg")
    .Register(AudioKeys.Sfx.ScramActive, "audio/scramactive.ogg")
    .Register(AudioKeys.Sfx.RodControl, "audio/rodcontrol.ogg")
    .Register(AudioKeys.Sfx.ControlInteract, "audio/controlinteract.ogg")
    .Register(AudioKeys.Sfx.ControlDenied, "audio/controldenied.ogg")
    .Register(AudioKeys.Sfx.AuthTrigger, "audio/redtrigger.ogg")
    .Register(AudioKeys.Sfx.Authorize, "audio/authorize.ogg")
    .Register(AudioKeys.Sfx.PowerOrder, "audio/powerorder.ogg")
    
    .Register(AudioKeys.Music.Overheat, "audio/overheat.ogg")
    .Register(AudioKeys.Music.Meltdown, "audio/meltdown.ogg")
    .Register(AudioKeys.Music.Evacuate, "audio/evacuate.ogg")
    .Register(AudioKeys.Music.Shutdown, "audio/shutdown.ogg")
    .Register(AudioKeys.Music.Ignition, "audio/ignition.ogg")
);

var app = builder.Build();
await app.RunAsync();