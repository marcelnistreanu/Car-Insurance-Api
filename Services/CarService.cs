using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }

    public async Task<InsuranceClaimResponse> CreateInsuranceClaim(long carId, CreateInsuranceClaimRequest request)
    {
        var insuranceValid = await IsInsuranceValidAsync(carId, DateOnly.FromDateTime(request.ClaimDate));

        if (!insuranceValid)
            throw new InvalidOperationException($"No valid insurance for car {carId} on {request.ClaimDate:yyyy-MM-dd}");

        if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero");
        if (string.IsNullOrWhiteSpace(request.Description)) throw new ArgumentException("Description is required");

        var claim = new InsuranceClaim
        {
            CarId = carId,
            ClaimDate = DateOnly.FromDateTime(request.ClaimDate),
            Description = request.Description,
            Amount = request.Amount,
            Status = ClaimStatusEnumType.Pending
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return new InsuranceClaimResponse(
            claim.Id,
            claim.CarId,
            claim.ClaimDate,
            claim.Description,
            claim.Amount,
            claim.Status
        );
    }
}
