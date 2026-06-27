using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.BusinessLayer.Utils;

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

        private async Task UpdateTourMetricsAsync(Guid tourId)
        {
            var tour = await _tourRepository.GetByIdAsync(tourId);
            if (tour == null) return;

            var logs = await _logRepository.GetLogsByTourIdAsync(tourId);
            var (popularity, childFriendliness) = TourMetricsCalculator.Calculate(tour, logs);

            // Use the dedicated metrics update (ExecuteUpdateAsync) to avoid
            // EF Core tracking conflicts with waypoints when UpdateAsync is called.
            await _tourRepository.UpdateMetricsAsync(tourId, popularity, childFriendliness);
        }

        public async Task<Log> CreateLogAsync(CreateLogDto createLogDto)
        {
            var tour = await _tourRepository.GetByIdAsync(createLogDto.TourId);


            if (tour is null)
            {
                throw new KeyNotFoundException("Tour does not exist");    
            }

            if (createLogDto.TotalDistanceKm == 0.0)
            {
                createLogDto.TotalDistanceKm = tour.DistanceKm;
            }

            if (createLogDto.TotalTimeMin == TimeSpan.Zero)
            {
                createLogDto.TotalTimeMin = tour.EstimatedTime;
            }

            var newLog = new Log
            {
                TourId = createLogDto.TourId,
                DateTime = createLogDto.DateTime.ToUniversalTime(),
                Comment = createLogDto.Comment,
                Rating = createLogDto.Rating,
                TotalDistanceKm = createLogDto.TotalDistanceKm,
                TotalTimeMin = createLogDto.TotalTimeMin,
                Difficulty = createLogDto.Difficulty
            };

            var createdLog = await _logRepository.AddAsync(newLog);
            await UpdateTourMetricsAsync(newLog.TourId);

            return createdLog;
        }
        public async Task<List<Log>> GetAllLogsAsync()
        {
            return await _logRepository.GetAllLogsAsync();
        }

        public async Task<List<Log>> GetLogsByTourIdAsync(Guid tourId)
        {
            return await _logRepository.GetLogsByTourIdAsync(tourId);
        }
        public async Task<Log?> GetLogByIdAsync(Guid id)
        {
            return await _logRepository.GetByIdAsync(id);
        }
        public async Task UpdateLogAsync(Guid id, CreateLogDto updateLogDto)
        {
            var tour = await _tourRepository.GetByIdAsync(updateLogDto.TourId);
            var log = await _logRepository.GetByIdAsync(id);

            if (tour is null)
            {
                throw new KeyNotFoundException("Tour does not exist");    
            }

            if (log is null)
            {
                throw new KeyNotFoundException("Log does not exist");    
            }

            if (updateLogDto.TotalDistanceKm == 0.0)
            {
                updateLogDto.TotalDistanceKm = tour.DistanceKm;
            }

            if (updateLogDto.TotalTimeMin == TimeSpan.Zero)
            {
                updateLogDto.TotalTimeMin = tour.EstimatedTime;
            }

            var oldTourId = log.TourId;

            log.TourId = updateLogDto.TourId;
            log.DateTime = updateLogDto.DateTime.ToUniversalTime();
            log.Comment = updateLogDto.Comment;
            log.Rating = updateLogDto.Rating;
            log.TotalDistanceKm = updateLogDto.TotalDistanceKm;
            log.TotalTimeMin = updateLogDto.TotalTimeMin;
            log.Difficulty = updateLogDto.Difficulty;
            log.UpdatedAt = DateTime.UtcNow;

            await _logRepository.UpdateAsync(log);
            await UpdateTourMetricsAsync(log.TourId);
            if (oldTourId != log.TourId)
            {
                await UpdateTourMetricsAsync(oldTourId);
            }
        }
        public async Task DeleteLogAsync(Guid id)
        {
            var log = await _logRepository.GetByIdAsync(id);
            if (log == null)
            {
                throw new KeyNotFoundException($"Log with ID {id} not found.");
            }
            var tourId = log.TourId;
            await _logRepository.DeleteAsync(id);
            await UpdateTourMetricsAsync(tourId);
        }
    }
}