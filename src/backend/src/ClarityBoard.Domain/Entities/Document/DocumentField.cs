namespace ClarityBoard.Domain.Entities.Document;

public class DocumentField
{
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string FieldName { get; private set; } = default!; // vendor_name, invoice_number, total_amount, date, vat_amount
    public string? FieldValue { get; private set; }
    public decimal Confidence { get; private set; }
    public bool IsVerified { get; private set; }
    public string? CorrectedValue { get; private set; }

    private DocumentField() { }

    public static DocumentField Create(
        Guid documentId, string fieldName, string? fieldValue, decimal confidence)
    {
        return new DocumentField
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FieldName = fieldName,
            FieldValue = fieldValue,
            Confidence = confidence,
        };
    }

    public void Verify(string? correctedValue = null)
    {
        IsVerified = true;
        if (correctedValue is not null)
            CorrectedValue = correctedValue;
    }
}
