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

        public async Task<List<Log>?> GetLogsByTourIdAsync(Guid tourId)
        {
            var tour = await _tourRepository.GetByIdAsync(tourId);
            if (tour is null)
            {
                return null;
            }
            return await _logRepository.GetLogsByTourIdAsync(tourId);
        }
        public async Task<Log?> GetLogByIdAsync(Guid id)
        {
            return await _logRepository.GetByIdAsync(id);
        }
        public async Task UpdateLogAsync(Guid id, CreateLogDto updateLogDto)
        {
            var tour = await _tourRepository.GetByIdAsync(updateLogDto.tour_id);
            var log = await _logRepository.GetByIdAsync(id);

            if (tour is null)
            {
                throw new KeyNotFoundException("Tour does not exist");    
            }

            if (log is null)
            {
                throw new KeyNotFoundException("Log does not exist");    
            }

            if (updateLogDto.total_distance_km == 0.0)
            {
                updateLogDto.total_distance_km = tour.Distance_km;
            }

            if (updateLogDto.total_time_min == TimeSpan.Zero)
            {
                updateLogDto.total_time_min = tour.EstimatedTime;
            }

            log.tour_id = updateLogDto.tour_id;
            log.date_time = updateLogDto.date_time.ToUniversalTime();
            log.comment = updateLogDto.comment;
            log.rating = updateLogDto.rating;
            log.total_distance_km = updateLogDto.total_distance_km;
            log.total_time_min = updateLogDto.total_time_min;
            log.difficulty = updateLogDto.difficulty;

            await _logRepository.UpdateAsync(log);

        }
        public async Task DeleteLogAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}