using CarInsurance.Api.Models;

namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);
public record CreateInsuranceClaimRequest(string ClaimDate, string Description, decimal Amount);
public record InsuranceClaimResponse(long Id, long CarId, DateOnly ClaimDate, string Description, decimal Amount, ClaimStatusEnumType Status);

public record CarHistoryResponse(
    long CarId,
    string Vin,
    string? Make,
    string? Model,
    List<InsurancePolicyDto> Policies,
    List<InsuranceClaimResponse> Claims
);

public record InsurancePolicyDto(
    DateOnly StartDate,
    DateOnly EndDate,
    string? Provider
);