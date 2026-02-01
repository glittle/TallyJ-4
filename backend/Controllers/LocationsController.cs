using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Locations;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for managing voting location operations including creation, retrieval, updates, and deletion.
/// </summary>
[ApiController]
[Route("api/elections/{electionGuid}/locations")]
[Authorize]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    /// <summary>
    /// Initializes a new instance of the LocationsController.
    /// </summary>
    /// <param name="locationService">The location service for location operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of locations for the specified election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="pageNumber">The page number (starting from 1).</param>
    /// <param name="pageSize">The number of items per page (1-200).</param>
    /// <returns>A paginated response containing the locations.</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<LocationDto>>> GetLocationsByElection(
        Guid electionGuid,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 200." });
        }

        var result = await _locationService.GetLocationsByElectionAsync(electionGuid, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific location by its GUID.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election (for route binding).</param>
    /// <param name="locationGuid">The GUID of the location.</param>
    /// <returns>The location information.</returns>
    [HttpGet("{locationGuid}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> GetLocation(Guid electionGuid, Guid locationGuid)
    {
        var location = await _locationService.GetLocationByGuidAsync(locationGuid);

        if (location == null)
        {
            return NotFound(ApiResponse<LocationDto>.ErrorResponse("Location not found"));
        }

        if (location.ElectionGuid != electionGuid)
        {
            return BadRequest(ApiResponse<LocationDto>.ErrorResponse("Location does not belong to the specified election"));
        }

        return Ok(ApiResponse<LocationDto>.SuccessResponse(location));
    }

    /// <summary>
    /// Creates a new location.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election (for route binding).</param>
    /// <param name="createDto">The location creation data.</param>
    /// <returns>The created location information.</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LocationDto>>> CreateLocation(Guid electionGuid, CreateLocationDto createDto)
    {
        if (createDto.ElectionGuid != electionGuid)
        {
            return BadRequest(ApiResponse<LocationDto>.ErrorResponse("Election GUID in the route must match the one in the request body"));
        }

        try
        {
            var location = await _locationService.CreateLocationAsync(createDto);

            return CreatedAtAction(
                nameof(GetLocation),
                new { electionGuid = location.ElectionGuid, locationGuid = location.LocationGuid },
                ApiResponse<LocationDto>.SuccessResponse(location, "Location created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LocationDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election (for route binding).</param>
    /// <param name="locationGuid">The GUID of the location to update.</param>
    /// <param name="updateDto">The updated location data.</param>
    /// <returns>The updated location information.</returns>
    [HttpPut("{locationGuid}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> UpdateLocation(
        Guid electionGuid,
        Guid locationGuid,
        UpdateLocationDto updateDto)
    {
        try
        {
            var location = await _locationService.UpdateLocationAsync(locationGuid, updateDto);

            if (location == null)
            {
                return NotFound(ApiResponse<LocationDto>.ErrorResponse("Location not found"));
            }

            if (location.ElectionGuid != electionGuid)
            {
                return BadRequest(ApiResponse<LocationDto>.ErrorResponse("Location does not belong to the specified election"));
            }

            return Ok(ApiResponse<LocationDto>.SuccessResponse(location, "Location updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LocationDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Deletes a location.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election (for route binding).</param>
    /// <param name="locationGuid">The GUID of the location to delete.</param>
    /// <returns>A response indicating success or failure.</returns>
    [HttpDelete("{locationGuid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLocation(Guid electionGuid, Guid locationGuid)
    {
        var location = await _locationService.GetLocationByGuidAsync(locationGuid);

        if (location == null)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Location not found"));
        }

        if (location.ElectionGuid != electionGuid)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse("Location does not belong to the specified election"));
        }

        var result = await _locationService.DeleteLocationAsync(locationGuid);

        if (!result)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Location not found"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Location deleted successfully"));
    }
}
