namespace ClarityBoard.Application.Features.Accounting.DTOs;

public record JournalEntryDto
{
    public required Guid Id { get; init; }
    public required long EntryNumber { get; init; }
    public required DateOnly EntryDate { get; init; }
    public required DateOnly PostingDate { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public required string? SourceType { get; init; }
    public required DateTime CreatedAt { get; init; }
    public IReadOnlyList<JournalEntryLineDto> Lines { get; init; } = [];
}

public record JournalEntryLineDto
{
    public required Guid Id { get; init; }
    public required int LineNumber { get; init; }
    public required Guid AccountId { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountName { get; init; }
    public string? AccountNameDe { get; init; }
    public string? AccountNameEn { get; init; }
    public string? AccountNameRu { get; init; }
    public required decimal DebitAmount { get; init; }
    public required decimal CreditAmount { get; init; }
    public required string Currency { get; init; }
    public string? VatCode { get; init; }
    public decimal? VatAmount { get; init; }
    public string? CostCenter { get; init; }
    public string? Description { get; init; }
}

public record AccountDto
{
    public required Guid Id { get; init; }
    public required string AccountNumber { get; init; }
    public required string Name { get; init; }
    public required string AccountType { get; init; }
    public required int AccountClass { get; init; }
    public required bool IsActive { get; init; }
    public string? VatDefault { get; init; }
    public string? NameDe { get; init; }
    public string? NameEn { get; init; }
    public string? NameRu { get; init; }
}

public record AccountDetailDto
{
    public required Guid Id { get; init; }
    public required string AccountNumber { get; init; }
    public required string Name { get; init; }
    public required string AccountType { get; init; }
    public required int AccountClass { get; init; }
    public required bool IsActive { get; init; }
    public string? VatDefault { get; init; }
    public string? DatevAuto { get; init; }
    public string? CostCenterDefault { get; init; }
    public string? BwaLine { get; init; }
    public required bool IsAutoPosting { get; init; }
    public required bool IsSystemAccount { get; init; }
    public Guid? ParentId { get; init; }
    public string? NameDe { get; init; }
    public string? NameEn { get; init; }
    public string? NameRu { get; init; }
    public int JournalEntryCount { get; set; }
    public DateOnly? LastBookingDate { get; set; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

public record CreateJournalEntryLineRequest
{
    public required Guid AccountId { get; init; }
    public required decimal DebitAmount { get; init; }
    public required decimal CreditAmount { get; init; }
    public string Currency { get; init; } = "EUR";
    public decimal ExchangeRate { get; init; } = 1.0m;
    public string? VatCode { get; init; }
    public decimal? VatAmount { get; init; }
    public string? CostCenter { get; init; }
    public string? Description { get; init; }
}
