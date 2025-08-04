using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartBus_BusinessObjects.DTOS
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // Models/Auth/RegisterRequest.cs
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Otp { get; set; } // client sends OTP for verification
    }

    // Models/Auth/ResetPasswordRequest.cs
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
        public string NewPassword { get; set; }
    }

    public class UserDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class BusRouteDTO
    {
        public string Id { get; set; }
        public string RouteCode { get; set; }
        public string RouteName { get; set; }
        public decimal? DistanceKm { get; set; }
        public string BusType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public int? TripsPerDay { get; set; }
        public int? TripDuration { get; set; }
        public string TripInterval { get; set; }
        public string PathToDestination { get; set; }
        public string PathToStart { get; set; }
    }


    public class BusRouteDetailDTO : BusRouteDTO
    {
        public List<string> StopNames { get; set; } = new();
        public List<string> TripHours { get; set; } = new();
        public List<string> TicketPrices { get; set; } = new();
    }


    public class BusVehicleLocationDTO
    {
        public int VehicleId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Speed { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class TicketTypeDisplayDTO
    {
        public int TicketTypeId { get; set; }
        public string TicketName { get; set; }
        public decimal? Price { get; set; }
    }


    public class CreateTicketRequest
    {
        public int UserId { get; set; }
        public string RouteId { get; set; }
        public int TicketTypeId { get; set; }
    }

    public class TicketResponse
    {
        public string Qrcode { get; set; }
        public decimal? Price { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
        public int? RemainingUses { get; set; }
        public string TicketTypeName { get; set; }
        public string RouteName { get; set; }
    }

    public enum TimeRange
    {
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Last6Months
    }
    public class StatisticFilterRequest
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TimeRange Range { get; set; } = TimeRange.Last6Months;
    }

    public class StatisticResult
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }



}
