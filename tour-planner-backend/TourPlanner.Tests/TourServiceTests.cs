using Moq;
using Moq.Protected;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Dtos;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.DataAccessLayer.Enums;
using System.Net;
using System.Net.Http.Json;

namespace TourPlanner.Tests
{
    [TestFixture]
    public class TourServiceTests
    {
        private Mock<ITourRepository> _tourRepositoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private TourService _tourService;

        [SetUp]
        public void SetUp()
        {
            _tourRepositoryMock = new Mock<ITourRepository>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _tourService = new TourService(_tourRepositoryMock.Object, _httpClient);
            
            Environment.SetEnvironmentVariable("OpenRoute_ApiKey", "test-key");
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
                userId = Guid.NewGuid(),
                name = "Test Tour",
                description = "Description",
                TransportType = TransportType.DrivingCar,
                waypoints = new List<WaypointDto>
                {
                    new WaypointDto { label = "Start", latitude = 48.0, longitude = 16.0 },
                    new WaypointDto { label = "End", latitude = 48.1, longitude = 16.1 }
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
    }
}
