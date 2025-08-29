using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using CarInsurance.Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<Result<List<CarDto>>> ListCarsAsync()
    {
        var cars = await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();

        return cars;
    }

    public async Task<Result<InsuranceValidityResponse>> IsInsuranceValidAsync(long carId, string dateString)
    {
        if (!DateOnly.TryParse(dateString, out var date))
            return Errors.General.InvalidDateFormat();

        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists)
            return Errors.General.NotFound(nameof(Car), carId);

        var isValid = await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );

        return new InsuranceValidityResponse(
            carId,
            date.ToString("yyyy-MM-dd"),
            isValid
        );
    }

    public async Task<Result<InsuranceClaimResponse>> CreateInsuranceClaim(
        long carId,
        CreateInsuranceClaimRequest request
    )
    {
        if (!DateTime.TryParse(request.ClaimDate, out var claimDate))
            return Errors.General.InvalidDateFormat();

        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists)
            return Errors.General.NotFound(nameof(Car), carId);

        var isValid = await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= DateOnly.FromDateTime(claimDate) &&
            p.EndDate >= DateOnly.FromDateTime(claimDate)
        );

        if (!isValid)
            return Errors.InsuranceClaim.NoActivePolicy(carId, DateOnly.FromDateTime(claimDate));

        var validation = ValidateClaimRequest(DateOnly.FromDateTime(claimDate), request.Amount, request.Description);

        if (validation != null)
            return validation;

        var claim = new InsuranceClaim
        {
            CarId = carId,
            ClaimDate = DateOnly.FromDateTime(claimDate),
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

    private static Error? ValidateClaimRequest(DateOnly claimDate, decimal amount, string description)
    {
        if (claimDate > DateOnly.FromDateTime(DateTime.UtcNow))
            return Errors.InsuranceClaim.InvalidClaimDate();

        if (amount <= 0)
            return Errors.InsuranceClaim.InvalidAmount();

        if (string.IsNullOrWhiteSpace(description))
            return Errors.InsuranceClaim.RequiredDescription();

        return null;
    }

    public async Task<Result<CarHistoryResponse>> GetCarHistoryAsync(long carId)
    {
        var car = await _db.Cars
            .Include(c => c.Policies)
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == carId);

        if (car == null)
            return Errors.General.NotFound(nameof(Car), carId);

        var policies = car.Policies
            .OrderBy(p => p.StartDate)
            .Select(p => new InsurancePolicyDto(
                p.StartDate,
                p.EndDate,
                p.Provider
            )).ToList();

        var claims = car.Claims
            .OrderBy(c => c.ClaimDate)
            .Select(c => new InsuranceClaimResponse(
                c.Id,
                c.CarId,
                c.ClaimDate,
                c.Description,
                c.Amount,
                c.Status
            )).ToList();

        return new CarHistoryResponse(
            car.Id,
            car.Vin,
            car.Make,
            car.Model,
            policies,
            claims
        );
    }
}
