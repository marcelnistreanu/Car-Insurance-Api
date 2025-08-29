using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using CarInsurance.Api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<IActionResult> GetCars()
    {
        var result = await _service.ListCarsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error?.Message ?? "An unknown error occurred." });

        return Ok(result.Value);
    }

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<IActionResult> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        var result = await _service.IsInsuranceValidAsync(carId, date);
        if (!result.IsSuccess)
        {
            if (result.Error?.Code == "record.not.found")
                return NotFound(new { message = result.Error.Message });
            if (result.Error?.Code == "invalid.date.format")
                return BadRequest(new { message = result.Error.Message });
            return BadRequest(new { message = result.Error?.Message ?? "An unknown error occurred." });
        }

        return Ok(result.Value);
    }

    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<InsuranceClaimResponse>> RegisterClaim(
        long carId,
        [FromBody] CreateInsuranceClaimRequest request
    )
    {
        var result = await _service.CreateInsuranceClaim(carId, request);

        if (!result.IsSuccess)
        {
            if (result.Error?.Code == "record.not.found")
                return NotFound(new { message = result.Error.Message });
            if (result.Error?.Code == "no.valid.policy")
                return BadRequest(new { message = result.Error.Message });
            if (result.Error?.Code == "invalid.date.format")
                return BadRequest(new { message = result.Error.Message });
            return BadRequest(new { message = result.Error?.Message ?? "An unknown error occurred." });
        }

        return Created();
    }

    [HttpGet("/api/cars/{carId:long}/history")]
    public async Task<IActionResult> GetCarHistory(long carId)
    {
        var result = await _service.GetCarHistoryAsync(carId);

        if (!result.IsSuccess)
        {
            if (result.Error?.Code == "record.not.found")
                return NotFound(new { message = result.Error.Message });

            return BadRequest(new { message = result.Error?.Message ?? "An unknown error occurred." });
        }

        return Ok(result.Value);
    }
}
