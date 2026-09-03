// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: UIToolkitFramework not yet converted
// 
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings.Utilities;

namespace Unity.SmartStrings.Extensions.Time.Utilities;

/// <summary>
/// The class contains <see cref="TimeTextInfo"/> definitions for common languages.
/// </summary>
static partial class CommonLanguagesTimeTextInfo
{
    [AutoStaticsCleanupOnCodeReload] // custom entries hold user PluralRuleDelegate instances; clear on reload
    internal static readonly Dictionary<string, TimeTextInfo> s_CustomLanguage = new();

    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for the English language.
    /// </summary>
    public static TimeTextInfo English => new()
    {
        PluralRule = PluralRules.GetPluralRule("en"),
        Ptxt_week = new[] { "{0} week", "{0} weeks" },
        Ptxt_day = new[] { "{0} day", "{0} days" },
        Ptxt_hour = new[] { "{0} hour", "{0} hours" },
        Ptxt_minute = new[] { "{0} minute", "{0} minutes" },
        Ptxt_second = new[] { "{0} second", "{0} seconds" },
        Ptxt_millisecond = new[] { "{0} millisecond", "{0} milliseconds" },
        Ptxt_w = new[] { "{0}w" },
        Ptxt_d = new[] { "{0}d" },
        Ptxt_h = new[] { "{0}h" },
        Ptxt_m = new[] { "{0}m" },
        Ptxt_s = new[] { "{0}s" },
        Ptxt_ms = new[] { "{0}ms" },
        Ptxt_lessThan = "less than {0}"
    };
    
    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for the French language.
    /// </summary>
    public static TimeTextInfo French => new()
    {
        PluralRule = PluralRules.GetPluralRule("fr"),
        Ptxt_week = new[] { "{0} semaine", "{0} semaines" },
        Ptxt_day = new[] { "{0} jour", "{0} jours" },
        Ptxt_hour = new[] { "{0} heure", "{0} heures" },
        Ptxt_minute = new[] { "{0} minute", "{0} minutes" },
        Ptxt_second = new[] { "{0} seconde", "{0} secondes" },
        Ptxt_millisecond = new[] { "{0} milliseconde", "{0} millisecondes" },
        Ptxt_w = new[] { "{0}sem" },
        Ptxt_d = new[] { "{0}j" },
        Ptxt_h = new[] { "{0}h" },
        Ptxt_m = new[] { "{0}m" },
        Ptxt_s = new[] { "{0}s" },
        Ptxt_ms = new[] { "{0}ms" },
        Ptxt_lessThan = "moins que {0}"
    };

    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for the Spanish language.
    /// </summary>
    public static TimeTextInfo Spanish => new()
    {
        PluralRule = PluralRules.GetPluralRule("es"),
        Ptxt_week = new[] { "{0} semana", "{0} semanas" },
        Ptxt_day = new[] { "{0} día", "{0} días" },
        Ptxt_hour = new[] { "{0} hore", "{0} horas" },
        Ptxt_minute = new[] { "{0} minuto", "{0} minutos" },
        Ptxt_second = new[] { "{0} segundo", "{0} segundos" },
        Ptxt_millisecond = new[] { "{0} milisegundo", "{0} milisegundos" },
        Ptxt_w = new[] { "{0}sem" },
        Ptxt_d = new[] { "{0}d" },
        Ptxt_h = new[] { "{0}h" },
        Ptxt_m = new[] { "{0}m" },
        Ptxt_s = new[] { "{0}s" },
        Ptxt_ms = new[] { "{0}ms" },
        Ptxt_lessThan = "menos que {0}"
    };

    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for the Portuguese language.
    /// </summary>
    public static TimeTextInfo Portuguese => new()
    {
        PluralRule = PluralRules.GetPluralRule("pt"),
        Ptxt_week = new[] { "{0} semana", "{0} semanas" },
        Ptxt_day = new[] { "{0} dia", "{0} dias" },
        Ptxt_hour = new[] { "{0} hora", "{0} horas" },
        Ptxt_minute = new[] { "{0} minuto", "{0} minutos" },
        Ptxt_second = new[] { "{0} segundo", "{0} segundos" },
        Ptxt_millisecond = new[] { "{0} milissegundo", "{0} milissegundos" },
        Ptxt_w = new[] { "{0}sem" },
        Ptxt_d = new[] { "{0}d" },
        Ptxt_h = new[] { "{0}h" },
        Ptxt_m = new[] { "{0}m" },
        Ptxt_s = new[] { "{0}s" },
        Ptxt_ms = new[] { "{0}ms" },
        Ptxt_lessThan = "menos do que {0}"
    };

    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for the Italian language.
    /// </summary>
    public static TimeTextInfo Italian => new()
    {
        PluralRule = PluralRules.GetPluralRule("it"),
        Ptxt_week = new[] { "{0} settimana", "{0} settimane" },
        Ptxt_day = new[] { "{0} giorno", "{0} giorni" },
        Ptxt_hour = new[] { "{0} ora", "{0} ore" },
        Ptxt_minute = new[] { "{0} minuto", "{0} minuti" },
        Ptxt_second = new[] { "{0} secondo", "{0} secondi" },
        Ptxt_millisecond = new[] { "{0} millisecondo", "{0} millisecondi" },
        Ptxt_w = new[] { "{0}set" },
        Ptxt_d = new[] { "{0}g" },
        Ptxt_h = new[] { "{0}h" },
        Ptxt_m = new[] { "{0}m" },
        Ptxt_s = new[] { "{0}s" },
        Ptxt_ms = new[] { "{0}ms" },
        Ptxt_lessThan = "meno di {0}"
    };

    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for the German language.
    /// </summary>
    public static TimeTextInfo German => new()
    {
        PluralRule = PluralRules.GetPluralRule("de"),
        Ptxt_week = new[] { "{0} Woche", "{0} Wochen" },
        Ptxt_day = new[] { "{0} Tag", "{0} Tage" },
        Ptxt_hour = new[] { "{0} Stunde", "{0} Stunden" },
        Ptxt_minute = new[] { "{0} Minute", "{0} Minuten" },
        Ptxt_second = new[] { "{0} Sekunde", "{0} Sekunden" },
        Ptxt_millisecond = new[] { "{0} Millisekunde", "{0} Millisekunden" },
        Ptxt_w = new[] { "{0}w" },
        Ptxt_d = new[] { "{0}t" },
        Ptxt_h = new[] { "{0}h" },
        Ptxt_m = new[] { "{0}m" },
        Ptxt_s = new[] { "{0}s" },
        Ptxt_ms = new[] { "{0}ms" },
        Ptxt_lessThan = "weniger als {0}"
    };

    /// <summary>
    /// Adds a <see cref="TimeTextInfo"/> for a language.
    /// </summary>
    /// <param name="twoLetterIsoLanguageName">The string to get the associated <see cref="System.Globalization.CultureInfo"/></param>
    /// <param name="timeTextInfo">The localized <see cref="TimeTextInfo"/></param>
    public static void AddLanguage(string twoLetterIsoLanguageName, TimeTextInfo timeTextInfo)
    {
        var c = twoLetterIsoLanguageName.ToLower();
        s_CustomLanguage.Add(c, timeTextInfo);
    }

    /// <summary>
    /// Gets the <see cref="TimeTextInfo"/> for a certain language.
    /// If the language is not implemented, the result will be <see langword="null"/>.
    /// </summary>
    /// <param name="twoLetterIsoLanguageName"></param>
    /// <returns>
    /// The <see cref="TimeTextInfo"/> for a certain language.
    /// If the language is not implemented, the result will be <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Custom languages can be added with <see cref="AddLanguage"/>.
    /// Custom languages override any built-in language with the same twoLetterISOLanguageName.
    /// </remarks>
    public static TimeTextInfo GetTimeTextInfo(string twoLetterIsoLanguageName)
    {
        if (s_CustomLanguage.TryGetValue(twoLetterIsoLanguageName, out var timeTextInfo))
            return timeTextInfo;

        return twoLetterIsoLanguageName switch
        {
            "en" => English,
            "fr" => French,
            "es" => Spanish,
            "pt" => Portuguese,
            "it" => Italian,
            "de" => German,
            _ => null
        };
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
