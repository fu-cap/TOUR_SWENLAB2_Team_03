using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.API.Dtos;

namespace TourPlanner.BusinessLayer.Services
{
    public interface ITourService
    {
        Task<Tour> CreateTourAsync(CreateTourDto createTourDto);
        Task<List<Tour>> GetAllToursAsync();
    }
}