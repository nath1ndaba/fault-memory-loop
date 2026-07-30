namespace FaultMemoryLoop.Application.Contracts;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);
