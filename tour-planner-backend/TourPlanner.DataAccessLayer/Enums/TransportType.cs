using System.ComponentModel;
using NpgsqlTypes;

namespace TourPlanner.DataAccessLayer.Enums
{
    public enum TransportType
    {
        [Description("driving-car")]
        [PgName("driving-car")]
        DrivingCar,
        
        [Description("driving-hgv")]
        [PgName("driving-hgv")]
        DrivingHgv,
        
        [Description("cycling-regular")]
        [PgName("cycling-regular")]
        CyclingRegular,
        
        [Description("cycling-road")]
        [PgName("cycling-road")]
        CyclingRoad,
        
        [Description("foot-walking")]
        [PgName("foot-walking")]
        FootWalking,
        
        [Description("foot-hiking")]
        [PgName("foot-hiking")]
        FootHiking,
    }
}