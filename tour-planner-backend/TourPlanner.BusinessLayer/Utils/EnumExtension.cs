using System;
using System.ComponentModel;
using System.Reflection;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Utils
{
    public static class EnumExtensions
    {
        public static string ToApiString(this TransportType transportType)
        {
            FieldInfo? field = transportType.GetType().GetField(transportType.ToString());

            if (field != null)
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    return attribute.Description;
                }
            }

            return transportType.ToString().ToLower(); 
        }

        public static TransportType ParseFromApiString(string apiString)
        {
            foreach (var field in typeof(TransportType).GetFields())
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description.Equals(apiString, StringComparison.OrdinalIgnoreCase))
                    {
                        // Gefunden! Konvertiere den Feldnamen in das echte Enum
                        return (TransportType)field.GetValue(null)!;
                    }
                }
            }

            throw new ArgumentException($"The transport type '{apiString}' is invalid or is not used");
        }
    }
}