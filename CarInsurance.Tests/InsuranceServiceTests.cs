using Microsoft.EntityFrameworkCore;
using CarInsurance.Api.Services;
using CarInsurance.Api.Data;

namespace CarInsurance.Api.CarInsurance.Tests;

public class InsuranceServiceTests
{
    private AppDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new AppDbContext(options);
        SeedData.EnsureSeeded(db);
        return db;
    }

    [Theory]
    [InlineData(1, "2024-01-01", true)]
    [InlineData(1, "2024-12-31", true)]
    [InlineData(1, "2023-12-31", false)]
    [InlineData(1, "2025-01-01", true)]
    [InlineData(1, "2025-12-31", true)]
    [InlineData(1, "2026-01-01", false)]
    public async Task IsInsuranceValid_BoundaryCases(long carId, string date, bool expected)
    {
        var db = GetDbContext(nameof(IsInsuranceValid_BoundaryCases));
        var service = new CarService(db);

        var result = await service.IsInsuranceValidAsync(carId, date);

        Assert.Equal(expected, result.Value?.Valid);
    }

    [Fact]
    public async Task IsInsuranceValid_InvalidCar_ShouldReturnNotFound()
    {
        var db = GetDbContext(nameof(IsInsuranceValid_InvalidCar_ShouldReturnNotFound));
        var service = new CarService(db);

        var result = await service.IsInsuranceValidAsync(999, "2024-06-01");

        Assert.False(result.IsSuccess);
        Assert.Equal("record.not.found", result.Error?.Code);
    }

    [Fact]
    public async Task IsInsuranceValid_InvalidDateFormat_ShouldReturnError()
    {
        var db = GetDbContext(nameof(IsInsuranceValid_InvalidDateFormat_ShouldReturnError));
        var service = new CarService(db);

        var result = await service.IsInsuranceValidAsync(1, "invalid-date");

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid.date.format", result.Error?.Code);
    }
}