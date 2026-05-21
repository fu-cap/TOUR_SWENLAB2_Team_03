using System.ComponentModel;

namespace TourPlanner.DataAccessLayer.Enums
{
    public enum TransportType
    {
        [Description("driving-car")]
        DrivingCar,
        
        [Description("driving-hgv")]
        DrivingHgv,
        
        [Description("cycling-regular")]
        CyclingRegular,
        [Description("cycling-road")]
        CyclingRoad,
        
        [Description("foot-walking")]
        FootWalking,
        [Description("foot-hiking")]
        FootHiking,

    }
}