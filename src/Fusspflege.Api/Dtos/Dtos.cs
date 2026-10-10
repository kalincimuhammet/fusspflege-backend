using System.ComponentModel.DataAnnotations;

namespace Fusspflege.Api.Dtos;

public record TreatmentDto(int Id, string Name, int DurationMinutes, int PriceCents);

public record SlotDto(DateTime StartUtc, DateTime EndUtc);

public class CreateAppointmentRequest
{
    [Range(1, int.MaxValue)]
    public int TreatmentId { get; set; }

    public DateTime StartUtc { get; set; }

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Die Einwilligung zur Datenverarbeitung ist erforderlich.")]
    public bool Consent { get; set; }
}

public record AppointmentCreatedResponse(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string TreatmentName);


public record AppointmentDetailsDto(
    DateTime StartUtc,
    DateTime EndUtc,
    string TreatmentName,
    string Status,
    bool CanCancel);