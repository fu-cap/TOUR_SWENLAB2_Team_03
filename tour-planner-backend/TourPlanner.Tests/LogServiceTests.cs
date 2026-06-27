using Moq;
using System.ComponentModel.DataAnnotations;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class LogServiceTests
    {
        private Mock<ILogRepository> _logRepositoryMock = null!;
        private Mock<ITourRepository> _tourRepositoryMock = null!;
        private LogService _logService = null!;

        [SetUp]
        public void SetUp()
        {
            _logRepositoryMock = new Mock<ILogRepository>();
            _tourRepositoryMock = new Mock<ITourRepository>();
            _logService = new LogService(_logRepositoryMock.Object, _tourRepositoryMock.Object);
        }

        [Test]
        public void CreateLogAsync_TourNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var dto = new CreateLogDto { TourId = Guid.NewGuid(), Comment = "Nice day" };
            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(dto.TourId)).ReturnsAsync((Tour?)null);

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _logService.CreateLogAsync(dto));
        }

        [Test]
        public async Task CreateLogAsync_ShouldDefaultDistanceAndTimeFromTour_IfZero()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            var tour = new Tour { Id = tourId, Name = "Test Tour", DistanceKm = 15.5, EstimatedTime = TimeSpan.FromHours(2) };
            var dto = new CreateLogDto
            {
                TourId = tourId,
                Comment = "Nice day",
                TotalDistanceKm = 0.0,
                TotalTimeMin = TimeSpan.Zero,
                DateTime = DateTime.UtcNow
            };

            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            Log? capturedLog = null;
            _logRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Log>()))
                .Callback<Log>(l => capturedLog = l)
                .ReturnsAsync((Log l) => l);

            // Act
            var result = await _logService.CreateLogAsync(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(capturedLog, Is.Not.Null);
            Assert.That(capturedLog!.TotalDistanceKm, Is.EqualTo(tour.DistanceKm));
            Assert.That(capturedLog.TotalTimeMin, Is.EqualTo(tour.EstimatedTime));
            _logRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Log>()), Times.Once);
        }

        [Test]
        public async Task CreateLogAsync_ShouldUseProvidedDistanceAndTime_IfNonZero()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            var tour = new Tour { Id = tourId, Name = "Test Tour", DistanceKm = 15.5, EstimatedTime = TimeSpan.FromHours(2) };
            var dto = new CreateLogDto
            {
                TourId = tourId,
                Comment = "Nice day",
                TotalDistanceKm = 10.0,
                TotalTimeMin = TimeSpan.FromHours(1),
                DateTime = DateTime.UtcNow
            };

            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            Log? capturedLog = null;
            _logRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Log>()))
                .Callback<Log>(l => capturedLog = l)
                .ReturnsAsync((Log l) => l);

            // Act
            var result = await _logService.CreateLogAsync(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(capturedLog, Is.Not.Null);
            Assert.That(capturedLog!.TotalDistanceKm, Is.EqualTo(dto.TotalDistanceKm));
            Assert.That(capturedLog.TotalTimeMin, Is.EqualTo(dto.TotalTimeMin));
        }

        [Test]
        public async Task GetAllLogsAsync_ShouldReturnLogsFromRepository()
        {
            // Arrange
            var expectedLogs = new List<Log> { new Log { Comment = "Log 1", DateTime = DateTime.UtcNow, Rating = 5 } };
            _logRepositoryMock.Setup(repo => repo.GetAllLogsAsync()).ReturnsAsync(expectedLogs);

            // Act
            var result = await _logService.GetAllLogsAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedLogs));
            _logRepositoryMock.Verify(repo => repo.GetAllLogsAsync(), Times.Once);
        }

        [Test]
        public async Task GetLogsByTourIdAsync_ShouldReturnLogsFromRepository()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            var expectedLogs = new List<Log> { new Log { TourId = tourId, Comment = "Log 1", DateTime = DateTime.UtcNow, Rating = 5 } };
            _logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(expectedLogs);

            // Act
            var result = await _logService.GetLogsByTourIdAsync(tourId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedLogs));
            _logRepositoryMock.Verify(repo => repo.GetLogsByTourIdAsync(tourId), Times.Once);
        }

        [Test]
        public async Task GetLogByIdAsync_ShouldReturnLogFromRepository()
        {
            // Arrange
            var logId = Guid.NewGuid();
            var expectedLog = new Log { Id = logId, Comment = "Log 1", DateTime = DateTime.UtcNow, Rating = 5 };
            _logRepositoryMock.Setup(repo => repo.GetByIdAsync(logId)).ReturnsAsync(expectedLog);

            // Act
            var result = await _logService.GetLogByIdAsync(logId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedLog));
            _logRepositoryMock.Verify(repo => repo.GetByIdAsync(logId), Times.Once);
        }

        [Test]
        public void UpdateLogAsync_TourNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var logId = Guid.NewGuid();
            var dto = new CreateLogDto { TourId = Guid.NewGuid(), Comment = "Updated comment" };
            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(dto.TourId)).ReturnsAsync((Tour?)null);

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _logService.UpdateLogAsync(logId, dto));
        }

        [Test]
        public void UpdateLogAsync_LogNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var logId = Guid.NewGuid();
            var tourId = Guid.NewGuid();
            var tour = new Tour { Id = tourId, Name = "Test Tour" };
            var dto = new CreateLogDto { TourId = tourId, Comment = "Updated comment" };
            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            _logRepositoryMock.Setup(repo => repo.GetByIdAsync(logId)).ReturnsAsync((Log?)null);

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _logService.UpdateLogAsync(logId, dto));
        }

        [Test]
        public async Task UpdateLogAsync_ShouldUpdateFieldsAndCallRepositoryUpdate()
        {
            // Arrange
            var logId = Guid.NewGuid();
            var tourId = Guid.NewGuid();
            var tour = new Tour { Id = tourId, Name = "Test Tour", DistanceKm = 10.0, EstimatedTime = TimeSpan.FromHours(1) };
            var existingLog = new Log { Id = logId, TourId = tourId, Comment = "Old Comment", DateTime = DateTime.UtcNow, Rating = 3 };
            var dto = new CreateLogDto
            {
                TourId = tourId,
                Comment = "Updated Comment",
                TotalDistanceKm = 12.0,
                TotalTimeMin = TimeSpan.FromHours(1.5),
                DateTime = DateTime.UtcNow
            };

            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(tour);
            _logRepositoryMock.Setup(repo => repo.GetByIdAsync(logId)).ReturnsAsync(existingLog);
            _logRepositoryMock.Setup(repo => repo.UpdateAsync(existingLog)).Returns(Task.CompletedTask);
            // Metrics recalculation side-effect: these are called by UpdateTourMetricsAsync
            _logRepositoryMock.Setup(repo => repo.GetLogsByTourIdAsync(tourId)).ReturnsAsync(new List<Log> { existingLog });
            _tourRepositoryMock.Setup(repo => repo.UpdateMetricsAsync(tourId, It.IsAny<double>(), It.IsAny<double>())).Returns(Task.CompletedTask);

            // Act
            await _logService.UpdateLogAsync(logId, dto);

            // Assert
            Assert.That(existingLog.Comment, Is.EqualTo(dto.Comment));
            Assert.That(existingLog.TotalDistanceKm, Is.EqualTo(dto.TotalDistanceKm));
            Assert.That(existingLog.TotalTimeMin, Is.EqualTo(dto.TotalTimeMin));
            _logRepositoryMock.Verify(repo => repo.UpdateAsync(existingLog), Times.Once);
            _tourRepositoryMock.Verify(repo => repo.UpdateMetricsAsync(tourId, It.IsAny<double>(), It.IsAny<double>()), Times.Once);
        }

        [Test]
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void CreateLogDto_InvalidDifficultyRange_ShouldFailValidation(int invalidDifficulty)
        {
            // Arrange
            var dto = new CreateLogDto
            {
                TourId = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Comment = "Test Log",
                Difficulty = invalidDifficulty,
                Rating = 3
            };
            
            var context = new ValidationContext(dto, null, null);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(results.Any(r => r.MemberNames.Contains("Difficulty")), Is.True);
        }

        [Test]
        [TestCase(0)]
        [TestCase(6)]
        [TestCase(-1)]
        [TestCase(-100)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void CreateLogDto_InvalidRatingRange_ShouldFailValidation(int invalidRating)
        {
            // Arrange
            var dto = new CreateLogDto
            {
                TourId = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Comment = "Test Log",
                Difficulty = 3,
                Rating = invalidRating
            };
            
            var context = new ValidationContext(dto, null, null);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.That(isValid, Is.False);
            Assert.That(results.Any(r => r.MemberNames.Contains("Rating")), Is.True);
        }

        [Test]
        public void CreateLogDto_ValidRanges_ShouldPassValidation()
        {
            // Arrange
            var dto = new CreateLogDto
            {
                TourId = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Comment = "Test Log",
                Difficulty = 3,
                Rating = 3
            };
            
            var context = new ValidationContext(dto, null, null);
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, context, results, true);

            // Assert
            Assert.That(isValid, Is.True);
        }
    }
}
