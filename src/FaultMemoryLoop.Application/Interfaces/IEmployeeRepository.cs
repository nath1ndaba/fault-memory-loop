using FaultMemoryLoop.Domain.Entities;

namespace FaultMemoryLoop.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Employee> AddAsync(Employee employee, CancellationToken ct = default);
}
