using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.BusinessLayer.Dtos;

namespace TourPlanner.BusinessLayer.Services
{
    public interface ITourService
    {
        Task<Tour> CreateTourAsync(CreateTourDto createTourDto);
        Task<List<Tour>> GetAllToursAsync();
        Task<Tour?> GetTourByIdAsync(Guid id);
        Task UpdateTourAsync(Guid id, CreateTourDto updateTourDto);
        Task DeleteTourAsync(Guid id);
    }
}