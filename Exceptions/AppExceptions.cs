namespace AuctionSystem.API.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, int id)
        : base($"{resource} with id {id} was not found.") { }
}
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
