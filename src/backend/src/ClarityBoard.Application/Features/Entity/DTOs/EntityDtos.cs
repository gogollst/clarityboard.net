namespace ClarityBoard.Application.Features.Entity.DTOs;

public record LegalEntityDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string LegalForm { get; init; }
    public string? RegistrationNumber { get; init; }
    public string? TaxId { get; init; }
    public string? VatId { get; init; }
    public required string Street { get; init; }
    public required string City { get; init; }
    public required string PostalCode { get; init; }
    public required string Country { get; init; }
    public required string Currency { get; init; }
    public required string ChartOfAccounts { get; init; }
    public required int FiscalYearStartMonth { get; init; }
    public Guid? ParentEntityId { get; init; }
    public required bool IsActive { get; init; }
    public string? DatevClientNumber { get; init; }
    public string? DatevConsultantNumber { get; init; }
    public Guid? ManagingDirectorId { get; init; }
    public string? ManagingDirectorName { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record EntitySwitchDto
{
    public required Guid EntityId { get; init; }
    public required string EntityName { get; init; }
    public required string Role { get; init; }
    public required string AccessToken { get; init; }
}
