using FaultMemoryLoop.Application.Interfaces;
using FaultMemoryLoop.Domain.Entities;
using FaultMemoryLoop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FaultMemoryLoop.Infrastructure.Repositories;

public class EmployeeRepository(FaultMemoryLoopDbContext dbContext) : IEmployeeRepository
{
    public Task<Employee?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        dbContext.Employees.FirstOrDefaultAsync(e => e.Email == email, ct);

    public async Task<Employee> AddAsync(Employee employee, CancellationToken ct = default)
    {
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(ct);
        return employee;
    }
}
