using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Locations;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing location operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle voting locations within elections.
/// </summary>
public class LocationService : ILocationService
{
    private readonly MainDbContext _context;
    private readonly ILogger<LocationService> _logger;

    /// <summary>
    /// Initializes a new instance of the LocationService.
    /// </summary>
    public LocationService(MainDbContext context, ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of locations for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of locations per page. Default is 50.</param>
    /// <returns>A paginated response containing location DTOs.</returns>
    public async Task<PaginatedResponse<LocationDto>> GetLocationsByElectionAsync(
        Guid electionGuid,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var locations = await query
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var locationDtos = locations.Select(l => MapToLocationDto(l)).ToList();

        _logger.LogInformation(
            "Retrieved {Count} locations for election {ElectionGuid} (page {PageNumber} of {TotalPages})",
            locationDtos.Count,
            electionGuid,
            pageNumber,
            (totalCount + pageSize - 1) / pageSize);

        return PaginatedResponse<LocationDto>.Create(locationDtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Retrieves a specific location by its unique identifier.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location.</param>
    /// <returns>The location DTO if found, otherwise null.</returns>
    public async Task<LocationDto?> GetLocationByGuidAsync(Guid locationGuid)
    {
        var location = await _context.Locations
            .Where(l => l.LocationGuid == locationGuid)
            .FirstOrDefaultAsync();

        if (location == null)
        {
            _logger.LogWarning("Location with GUID {LocationGuid} not found", locationGuid);
            return null;
        }

        var locationDto = MapToLocationDto(location);

        _logger.LogInformation("Retrieved location {LocationGuid}: {LocationName}", locationGuid, location.Name);

        return locationDto;
    }

    /// <summary>
    /// Creates a new location with the provided data.
    /// </summary>
    /// <param name="createDto">The location creation data.</param>
    /// <returns>The created location DTO.</returns>
    public async Task<LocationDto> CreateLocationAsync(CreateLocationDto createDto)
    {
        _logger.LogInformation("Creating new location: {LocationName} for election {ElectionGuid}", createDto.Name, createDto.ElectionGuid);

        var location = MapFromCreateLocationDto(createDto);
        location.LocationGuid = Guid.NewGuid();
        location.LocationTallyStatus = LocationTallyStatus.NotStarted;
        location.BallotsCollected = 0;

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        var locationDto = MapToLocationDto(location);

        _logger.LogInformation("Successfully created location {LocationGuid}: {LocationName}", location.LocationGuid, location.Name);

        return locationDto;
    }

    /// <summary>
    /// Updates an existing location with the provided data.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location to update.</param>
    /// <param name="updateDto">The updated location data.</param>
    /// <returns>The updated location DTO if found, otherwise null.</returns>
    public async Task<LocationDto?> UpdateLocationAsync(Guid locationGuid, UpdateLocationDto updateDto)
    {
        _logger.LogInformation("Updating location {LocationGuid}", locationGuid);

        var location = await _context.Locations
            .Where(l => l.LocationGuid == locationGuid)
            .FirstOrDefaultAsync();

        if (location == null)
        {
            _logger.LogWarning("Location with GUID {LocationGuid} not found for update", locationGuid);
            return null;
        }

        updateDto.CopyMatchingPropertiesTo(location, ignoreNulls: false);
        await _context.SaveChangesAsync();

        var locationDto = MapToLocationDto(location);

        _logger.LogInformation("Successfully updated location {LocationGuid}: {LocationName}", location.LocationGuid, location.Name);

        return locationDto;
    }

    /// <summary>
    /// Deletes a location by its unique identifier.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location to delete.</param>
    /// <returns>True if the location was successfully deleted, false if not found.</returns>
    public async Task<bool> DeleteLocationAsync(Guid locationGuid)
    {
        _logger.LogInformation("Deleting location {LocationGuid}", locationGuid);

        var location = await _context.Locations
            .Where(l => l.LocationGuid == locationGuid)
            .FirstOrDefaultAsync();

        if (location == null)
        {
            _logger.LogWarning("Location with GUID {LocationGuid} not found for deletion", locationGuid);
            return false;
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted location {LocationGuid}: {LocationName}", locationGuid, location.Name);

        return true;
    }

    // Explicit mapping helpers (replaces what was previously hidden in Mapster profiles).
    // These make the field renames and type conversions completely visible.

    private static LocationDto MapToLocationDto(Location location)
    {
        var dto = location.CopyMatchingPropertiesToNew<LocationDto>();

        // Renamed fields from entity (Long/Lat) to DTO (Longitude/Latitude)
        dto.Longitude = location.Long;
        dto.Latitude = location.Lat;

        // LocationType handling
        dto.LocationType = location.LocationTypeCode != null
            ? (LocationType?)location.LocationTypeEnum
            : null;

        return dto;
    }

    private static Location MapFromCreateLocationDto(CreateLocationDto dto)
    {
        var location = dto.CopyMatchingPropertiesToNew<Location>();

        // Renamed fields
        location.Long = dto.Longitude;
        location.Lat = dto.Latitude;

        return location;
    }
}



