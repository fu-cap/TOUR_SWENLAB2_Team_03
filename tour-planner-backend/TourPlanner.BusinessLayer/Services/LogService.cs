using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.BusinessLayer.Services
{
    public class LogService: ILogService
    {
        private readonly ILogRepository _logRepository;
        private readonly ITourRepository _tourRepository;
        public LogService(ILogRepository logRepository, ITourRepository tourRepository)
        {
            _logRepository = logRepository;
            _tourRepository = tourRepository;
        }

        public async Task<Log> CreateLogAsync(CreateLogDto createLogDto)
        {
            var tour = await _tourRepository.GetByIdAsync(createLogDto.tour_id);


            if (tour is null)
            {
                throw new KeyNotFoundException("Tour does not exist");    
            }

            if (createLogDto.total_distance_km == 0.0)
            {
                createLogDto.total_distance_km = tour.Distance_km;
            }

            if (createLogDto.total_time_min == TimeSpan.Zero)
            {
                createLogDto.total_time_min = tour.EstimatedTime;
            }

            var newLog = new Log
            {
                tour_id = createLogDto.tour_id,
                date_time = createLogDto.date_time.ToUniversalTime(),
                comment = createLogDto.comment,
                rating = createLogDto.rating,
                total_distance_km = createLogDto.total_distance_km,
                total_time_min = createLogDto.total_time_min,
                difficulty = createLogDto.difficulty
            };

            var createdLog = await _logRepository.AddAsync(newLog);

            return createdLog;
        }
        public async Task<List<Log>> GetAllLogsAsync()
        {
            return await _logRepository.GetAllLogsAsync();
        }
        public async Task<Log?> GetLogByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
        public async Task UpdateLogAsync(Guid id, CreateLogDto updateLogDto)
        {
            throw new NotImplementedException();
        }
        public async Task DeleteLogAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}