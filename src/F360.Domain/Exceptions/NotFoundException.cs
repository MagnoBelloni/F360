namespace F360.Domain.Exceptions;

public class NotFoundException(string message) : BusinessException(message, 404);