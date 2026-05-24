using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NpgsqlTypes;

namespace TourPlanner.DataAccessLayer.Utils
{
    public static class EnumExtensions
    {
        // Liest den Wert aus [PgName] oder [Description] oder nimmt den Standardnamen
        public static string ToPgName<T>(this T enumValue) where T : struct, Enum
        {
            var name = enumValue.ToString();
            var field = typeof(T).GetField(name);

            if (field == null) return name;

            // 1. Versuch: PgNameAttribute auslesen
            var pgNameAttr = field.GetCustomAttribute<PgNameAttribute>();
            if (pgNameAttr != null) return pgNameAttr.PgName;

            // 2. Versuch: DescriptionAttribute auslesen
            var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null) return descAttr.Description;

            // Fallback: Falls kein Attribut da ist
            return name;
        }

        // Konvertiert den String aus der DB wieder zurück in den C# Enum-Wert
        public static T FromPgName<T>(string pgName) where T : struct, Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                var pgNameAttr = field.GetCustomAttribute<PgNameAttribute>();
                if (pgNameAttr != null && pgNameAttr.PgName == pgName)
                {
                    return (T)field.GetValue(null)!;
                }

                var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null && descAttr.Description == pgName)
                {
                    return (T)field.GetValue(null)!;
                }
            }

            // Fallback auf Standard-Parsing, falls nichts gefunden wurde
            return Enum.TryParse<T>(pgName, true, out var result) ? result : default;
        }
    }
}