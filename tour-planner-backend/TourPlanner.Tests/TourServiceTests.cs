using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;
using TourPlanner.DataAccessLayer.Repositories;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class TourServiceTests
    {
        private Mock<ITourRepository> _tourRepositoryMock = null!;
        private Mock<ILogRepository> _logRepositoryMock = null!;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
        private HttpClient _httpClient = null!;
        private TourService _tourService = null!;

        [SetUp]
        public void SetUp()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OpenRoute_ApiKey")))
            {
                Environment.SetEnvironmentVariable("OpenRoute_ApiKey", "mock-test-key");
            }
            _tourRepositoryMock = new Mock<ITourRepository>();
            _logRepositoryMock = new Mock<ILogRepository>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _tourService = new TourService(_tourRepositoryMock.Object, _logRepositoryMock.Object, _httpClient);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        [Test]
        public async Task CreateTourAsync_ShouldAssignUniqueIds()
        {
            // Arrange
            var dto = new CreateTourDto
            {
                UserId = Guid.NewGuid(),
                Name = "Test Tour",
                Description = "Description",
                TransportType = TransportType.DrivingCar,
                Waypoints = new List<WaypointDto>
                {
                    new WaypointDto { Label = "Start", Latitude = 48.0, Longitude = 16.0 },
                    new WaypointDto { Label = "End", Latitude = 48.1, Longitude = 16.1 }
                }
            };

            // Mock ORS Response
            var orsResponse = new
            {
                routes = new[]
                {
                    new
                    {
                        summary = new { distance = 1000.0, duration = 60.0 },
                        geometry = "some-geometry"
                    }
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(orsResponse)
                });

            Tour? capturedTour = null;
            _tourRepositoryMock
                .Setup(repo => repo.AddAsync(It.IsAny<Tour>()))
                .Callback<Tour>(t => capturedTour = t)
                .ReturnsAsync((Tour t) => t);

            // Act
            var result = await _tourService.CreateTourAsync(dto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(capturedTour, Is.Not.Null);
            Assert.That(capturedTour!.Id, Is.Not.EqualTo(Guid.Empty), "Tour ID should not be empty");
            Assert.That(capturedTour.Waypoints.Count, Is.EqualTo(2));
            foreach (var wp in capturedTour.Waypoints)
            {
                Assert.That(wp.Id, Is.Not.EqualTo(Guid.Empty), "Waypoint ID should not be empty");
                Assert.That(wp.TourId, Is.EqualTo(capturedTour.Id), "Waypoint TourId should match Tour ID");
            }
        }

        [Test]
        public async Task GetToursByUserIdAsync_WithSearch_ShouldCallRepositoryWithSearch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var search = "lake";
            var expectedTours = new List<Tour>();
            _tourRepositoryMock
                .Setup(repo => repo.GetToursByUserIdAsync(userId, search))
                .ReturnsAsync(expectedTours);

            // Act
            var result = await _tourService.GetToursByUserIdAsync(userId, search);

            // Assert
            Assert.That(result, Is.EqualTo(expectedTours));
            _tourRepositoryMock.Verify(repo => repo.GetToursByUserIdAsync(userId, search), Times.Once);
        }

        [Test]
        public async Task GetAllToursAsync_ShouldReturnToursFromRepository()
        {
            // Arrange
            var expectedTours = new List<Tour> { new Tour { Name = "Tour 1" } };
            _tourRepositoryMock.Setup(repo => repo.GetAllToursAsync()).ReturnsAsync(expectedTours);

            // Act
            var result = await _tourService.GetAllToursAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedTours));
            _tourRepositoryMock.Verify(repo => repo.GetAllToursAsync(), Times.Once);
        }

        [Test]
        public async Task GetTourByIdAsync_ShouldReturnTourFromRepository()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            var expectedTour = new Tour { Id = tourId, Name = "Tour 1" };
            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(expectedTour);

            // Act
            var result = await _tourService.GetTourByIdAsync(tourId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedTour));
            _tourRepositoryMock.Verify(repo => repo.GetByIdAsync(tourId), Times.Once);
        }

        [Test]
        public async Task DeleteTourAsync_ShouldCallRepositoryDelete()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            _tourRepositoryMock.Setup(repo => repo.DeleteAsync(tourId)).Returns(Task.CompletedTask);

            // Act
            await _tourService.DeleteTourAsync(tourId);

            // Assert
            _tourRepositoryMock.Verify(repo => repo.DeleteAsync(tourId), Times.Once);
        }

        [Test]
        public void UpdateTourAsync_TourNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            var dto = new CreateTourDto { Name = "Updated Name", Waypoints = new List<WaypointDto>() };
            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync((Tour?)null);

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _tourService.UpdateTourAsync(tourId, dto));
        }

        [Test]
        public void UpdateTourAsync_FewerThanTwoWaypoints_ShouldThrowArgumentException()
        {
            // Arrange
            var tourId = Guid.NewGuid();
            var dto = new CreateTourDto
            {
                Name = "Updated Name",
                Waypoints = new List<WaypointDto> { new WaypointDto { Label = "Start" } }
            };
            var existingTour = new Tour { Id = tourId, Name = "Existing Tour" };
            _tourRepositoryMock.Setup(repo => repo.GetByIdAsync(tourId)).ReturnsAsync(existingTour);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await _tourService.UpdateTourAsync(tourId, dto));
        }

        [Test]
        public void CreateTourAsync_FewerThanTwoWaypoints_ShouldThrowArgumentException()
        {
            // Arrange
            var dto = new CreateTourDto
            {
                Name = "New Tour",
                Waypoints = new List<WaypointDto> { new WaypointDto { Label = "Start" } }
            };

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => await _tourService.CreateTourAsync(dto));
        }
    }
}
