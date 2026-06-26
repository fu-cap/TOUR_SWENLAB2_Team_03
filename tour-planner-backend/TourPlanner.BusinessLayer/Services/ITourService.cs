using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.BusinessLayer.Dtos;

namespace TourPlanner.BusinessLayer.Services
{
    public interface ITourService
    {
        Task<Tour> CreateTourAsync(CreateTourDto createTourDto);
        Task<List<Tour>> GetAllToursAsync();
        Task<List<Tour>> GetToursByUserIdAsync(Guid userId, string? search = null);
        Task<Tour?> GetTourByIdAsync(Guid id);
        Task UpdateTourAsync(Guid id, CreateTourDto updateTourDto);
        Task DeleteTourAsync(Guid id);
        Task<List<TourImportDto>> ExportToursAsync(Guid userId);
        Task ImportToursAsync(Guid userId, List<TourImportDto> tours);
    }
}