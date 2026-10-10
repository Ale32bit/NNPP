using System.Collections.ObjectModel;

namespace NNPP;

public class MusicCredits
{
    public static readonly IDictionary<string, string> Credits = new ReadOnlyDictionary<string, string>(
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AudioKeys.Music.Overheat] = "Kelly Bailey - Electric Guitar Ambience",
            [AudioKeys.Music.Meltdown] = "Vern Carson - Oppress the Oppressors",
            [AudioKeys.Music.Evacuate] = "KETSU! - BALLISTIC - Sock.clip (OVERDRIVE MIX)",
            [AudioKeys.Music.Shutdown] = "Alastair King - New Life Anthem",
            [AudioKeys.Music.Ignition] = "Joel Nielsen - On a Rail 2",
            [AudioKeys.Music.DayShift] = "cleanmindsounds - Dark Industrial Cinematic",
            [AudioKeys.Music.NightShift] = "The Commodores - Nightshift",
            [AudioKeys.Music.Tier3] = "ClaireLiz - cat beatbox challenge",
        });
}