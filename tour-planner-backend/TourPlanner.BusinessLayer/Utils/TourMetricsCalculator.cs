using System;
using System.Collections.Generic;
using System.Linq;
using TourPlanner.DataAccessLayer.Entities;
using TourPlanner.DataAccessLayer.Enums;

namespace TourPlanner.BusinessLayer.Utils
{
    public static class TourMetricsCalculator
    {
        public static (double Popularity, double ChildFriendliness) Calculate(Tour tour, List<Log>? logs)
        {
            var logsList = logs ?? new List<Log>();
            double popularity = logsList.Count;

            // 1. Base Score by Transport Type
            double baseScore = tour.TransportType switch
            {
                TransportType.CyclingRegular => 10.0,
                TransportType.FootWalking    => 10.0,
                TransportType.FootHiking     => 8.0,
                TransportType.CyclingRoad    => 6.0,
                TransportType.DrivingCar     => 3.0,
                TransportType.DrivingHgv     => 1.0,
                _                            => 5.0
            };

            double difficultyPenalty = 0.0;
            double distancePenalty = 0.0;
            double timePenalty = 0.0;

            if (logsList.Any())
            {
                // Derived from log statistics
                double avgDifficulty = logsList.Average(l => l.Difficulty); // 1 (easy) to 5 (hard)
                double avgTimeMin = logsList.Average(l => l.TotalTimeMin.TotalMinutes);
                double avgDistanceKm = logsList.Average(l => l.TotalDistanceKm);

                // Penalty based on average difficulty
                // Difficulty 1 -> 0 penalty; Difficulty 5 -> 6.0 penalty
                difficultyPenalty = (avgDifficulty - 1.0) * 1.5;

                // Penalty based on average distance
                // Distance up to 3 km is fully child-friendly; deduct 0.5 points per km above 3 km
                if (avgDistanceKm > 3.0)
                {
                    distancePenalty = (avgDistanceKm - 3.0) * 0.5;
                }

                // Penalty based on average time duration
                // Duration up to 30 mins is fully child-friendly; deduct 0.05 points per minute above 30 mins
                if (avgTimeMin > 30.0)
                {
                    timePenalty = (avgTimeMin - 30.0) * 0.05;
                }
            }
            else
            {
                // If no logs exist, use tour's default distance and estimated time
                double distanceKm = tour.DistanceKm;
                double timeMin = tour.EstimatedTime.TotalMinutes;

                if (distanceKm > 3.0)
                {
                    distancePenalty = (distanceKm - 3.0) * 0.5;
                }

                if (timeMin > 30.0)
                {
                    timePenalty = (timeMin - 30.0) * 0.05;
                }
            }

            double childFriendliness = baseScore - difficultyPenalty - distancePenalty - timePenalty;

            // Clamp and round the final score
            childFriendliness = Math.Clamp(childFriendliness, 0.0, 10.0);
            childFriendliness = Math.Round(childFriendliness, 1);

            return (popularity, childFriendliness);
        }
        public static (double Co2SavedGrams, double Co2EmittedGrams) ComputeCo2(TransportType transportType, double distanceKm)
        {
            // Baseline: average car emits 120 g CO2/km
            const double carEmissionPerKm = 120.0;

            double emittedPerKm = transportType switch
            {
                // Zero-emission active transport
                TransportType.FootWalking    => 0.0,
                TransportType.FootHiking     => 0.0,
                TransportType.CyclingRegular => 0.0,
                TransportType.CyclingRoad    => 0.0,
                // Motorised: car & HGV emit the full baseline
                TransportType.DrivingCar     => carEmissionPerKm,
                TransportType.DrivingHgv     => carEmissionPerKm * 1.5, // HGV ~50% more
                _                            => carEmissionPerKm
            };

            double emittedGrams = Math.Round(emittedPerKm * distanceKm, 1);
            double savedGrams   = Math.Round(Math.Max(0.0, (carEmissionPerKm - emittedPerKm) * distanceKm), 1);

            return (savedGrams, emittedGrams);
        }
    }
}
