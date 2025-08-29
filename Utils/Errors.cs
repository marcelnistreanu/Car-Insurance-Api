namespace CarInsurance.Api.Utils;

public class Errors
{
    public static class InsuranceClaim
    {
        public static Error NoActivePolicy(long carId, DateOnly claimDate) =>
            new Error("no.active.policy", $"No active insurance policy for Car Id {carId} on {claimDate:yyyy-MM-dd}");

        public static Error InvalidClaimDate() =>
            new Error("invalid.claim.date", "Claim date cannot be in the future");

        public static Error InvalidAmount() =>
            new Error("invalid.amount", "Amount must be greater than zero");

        public static Error RequiredDescription() =>
            new Error("required.description", "Description is required");
    }

    public static class General
    {
        public static Error NotFound(string entityName, long id) =>
            new Error("record.not.found", $"{entityName} not found for Id {id}");

        public static Error InvalidDateFormat() =>
            new Error("invalid.date.format", "Invalid date format. Use YYYY-MM-DD.");
    }
}
