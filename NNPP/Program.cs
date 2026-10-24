using System.Security.Cryptography;
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
    .Register(AudioKeys.Sfx.Notification, "audio/notification.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.NotificationCritical, "audio/notificationcritical.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.MetalCry, "audio/metalcry.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.ReactorExplosion, "audio/reactorexplosion.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.AnnouncerMeltdown, "audio/announcermeltdown.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.MeltdownAlarm, "audio/meltdownalarm.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.MeltdownExplosion, "audio/meltdownexplosion.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.ScramActive, "audio/scramactive.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.RodControl, "audio/rodcontrol.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.ControlInteract, "audio/controlinteract.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.ControlDenied, "audio/controldenied.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.AuthTrigger, "audio/redtrigger.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.Authorize, "audio/authorize.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.PowerOrder, "audio/powerorder.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.PowerOut, "audio/powerout.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.PowerIn, "audio/powerin.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.BlackOut, "audio/blackout.ogg", AudioType.Sfx)
    .Register(AudioKeys.Sfx.AlarmMalfunction, "audio/alarmmalfunction.ogg", AudioType.Alarm)
    .Register(AudioKeys.Sfx.AlarmCritical, "audio/alarmcritical.ogg", AudioType.Alarm)
    .Register(AudioKeys.Sfx.AlarmAttention, "audio/alarmattention.ogg", AudioType.Alarm)
    .Register(AudioKeys.Sfx.AlarmSevere, "audio/alarmsevere.ogg", AudioType.Alarm)
    .Register(AudioKeys.Sfx.AlarmOverheat, "audio/alarmoverheat.ogg", AudioType.Alarm)
    .Register(AudioKeys.Sfx.Alarm1, "audio/alarm1.ogg", AudioType.Alarm)
    .Register(AudioKeys.Sfx.AlarmOverspeed, "audio/alarmoverspeed.ogg", AudioType.Alarm)
    .Register(AudioKeys.Music.Overheat, "audio/overheat.ogg", AudioType.Music)
    .Register(AudioKeys.Music.Meltdown, "audio/meltdown.ogg", AudioType.Music)
    .Register(AudioKeys.Music.Evacuate, "audio/evacuate.ogg", AudioType.Music)
    .Register(AudioKeys.Music.Shutdown, "audio/shutdown.ogg", AudioType.Music)
    .Register(AudioKeys.Music.Ignition, "audio/ignition.ogg", AudioType.Music)
    .Register(AudioKeys.Music.DayShift, "audio/dayshift.ogg", AudioType.Music)
    .Register(AudioKeys.Music.NightShift, "audio/nightshift.ogg", AudioType.Music)
);

var app = builder.Build();
await app.RunAsync();