namespace LedgerLoop.Api.Services;

public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message)
    {
    }
}

public class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(Guid id) : base($"Document {id} was not found.")
    {
    }
}

public class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException(string message) : base(message)
    {
    }
}
