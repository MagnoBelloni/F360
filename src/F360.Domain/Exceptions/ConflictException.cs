namespace F360.Domain.Exceptions;

public class ConflictException(string message) : BusinessException(message, 409);
