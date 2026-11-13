using AstronautHealthMonitoring.Data;
using AstronautHealthMonitoring.DTOs.Astronaut;
using AstronautHealthMonitoring.DTOs.Auth;
using AstronautHealthMonitoring.DTOs.Common;
using AstronautHealthMonitoring.Models.Entities;
using AstronautHealthMonitoring.Services.Interfaces;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AstronautHealthMonitoring.Services.Implementations;

public class AstronautService : IAstronautService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<AstronautService> _logger;

    public AstronautService(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<AstronautService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<AstronautDto>> GetAstronautsPagedAsync(
        int? missionId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .AsQueryable();

        if (missionId.HasValue)
        {
            query = query.Where(a => a.MissionId == missionId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(a => a.AstronautCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<AstronautDto>>(items);

        return new PagedResult<AstronautDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<AstronautDto>> GetAllAstronautsAsync(CancellationToken cancellationToken = default)
    {
        var astronauts = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .OrderBy(a => a.AstronautCode)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<AstronautDto>>(astronauts);
    }

    public async Task<AstronautDto?> GetAstronautByIdAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);

        return astronaut != null ? _mapper.Map<AstronautDto>(astronaut) : null;
    }

    public async Task<AstronautDto?> GetAstronautByCodeAsync(string astronautCode, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AstronautCode == astronautCode, cancellationToken);

        return astronaut != null ? _mapper.Map<AstronautDto>(astronaut) : null;
    }

    public async Task<AstronautDto?> GetAstronautByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        return astronaut != null ? _mapper.Map<AstronautDto>(astronaut) : null;
    }

    public async Task<AstronautDto> CreateAstronautAsync(CreateAstronautRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Conflict checks for duplicate username, email, and astronaut code
        if (await _userManager.FindByNameAsync(request.Username) != null)
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");
        }

        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        if (await _dbContext.Astronauts.AnyAsync(a => a.AstronautCode == request.AstronautCode, cancellationToken))
        {
            throw new InvalidOperationException($"Astronaut code '{request.AstronautCode}' already exists.");
        }

        if (request.MissionId.HasValue)
        {
            var missionExists = await _dbContext.Missions.AnyAsync(m => m.Id == request.MissionId.Value, cancellationToken);
            if (!missionExists)
            {
                throw new KeyNotFoundException($"Mission with ID {request.MissionId.Value} not found.");
            }
        }

        // 2. Validate initial password via Identity password policy
        var tempUser = new ApplicationUser { UserName = request.Username, Email = request.Email };
        foreach (var validator in _userManager.PasswordValidators)
        {
            var validationResult = await validator.ValidateAsync(_userManager, tempUser, request.InitialPassword);
            if (!validationResult.Succeeded)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.Description));
                throw new ArgumentException($"Password does not meet requirements: {errors}");
            }
        }

        // 3. Atomically create Identity user and domain Astronaut in one transaction
        var isRelational = _dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
        await using var transaction = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FullName = fullName,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var userCreateResult = await _userManager.CreateAsync(user, request.InitialPassword);
            if (!userCreateResult.Succeeded)
            {
                var errors = string.Join("; ", userCreateResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create Identity user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, UserRoles.Astronaut);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign astronaut role: {errors}");
            }

            var astronaut = _mapper.Map<Astronaut>(request);
            astronaut.UserId = user.Id;
            astronaut.FullName = fullName;
            astronaut.IsActive = true;
            astronaut.CreatedAt = DateTime.UtcNow;

            _dbContext.Astronauts.Add(astronaut);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Astronaut '{AstronautCode}' and Identity user '{Username}' provisioned successfully.",
                astronaut.AstronautCode, user.UserName);

            if (astronaut.MissionId.HasValue)
            {
                await _dbContext.Entry(astronaut).Reference(a => a.Mission).LoadAsync(cancellationToken);
            }

            return _mapper.Map<AstronautDto>(astronaut);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task<AstronautDto> UpdateAstronautAsync(int astronautId, UpdateAstronautRequest request, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);

        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} not found.");
        }

        if (request.MissionId.HasValue && request.MissionId != astronaut.MissionId)
        {
            var missionExists = await _dbContext.Missions.AnyAsync(m => m.Id == request.MissionId.Value, cancellationToken);
            if (!missionExists)
            {
                throw new KeyNotFoundException($"Mission with ID {request.MissionId.Value} not found.");
            }
        }

        _mapper.Map(request, astronaut);
        astronaut.FullName = $"{request.FirstName} {request.LastName}".Trim();
        astronaut.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(astronaut.UserId))
        {
            var user = await _userManager.FindByIdAsync(astronaut.UserId);
            if (user != null && user.FullName != astronaut.FullName)
            {
                user.FullName = astronaut.FullName;
                await _userManager.UpdateAsync(user);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (astronaut.MissionId.HasValue)
        {
            await _dbContext.Entry(astronaut).Reference(a => a.Mission).LoadAsync(cancellationToken);
        }

        return _mapper.Map<AstronautDto>(astronaut);
    }

    public async Task<bool> DeactivateAstronautAsync(int astronautId, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);

        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} not found.");
        }

        astronaut.IsActive = false;
        astronaut.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(astronaut.UserId))
        {
            var user = await _userManager.FindByIdAsync(astronaut.UserId);
            if (user != null)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                await _userManager.UpdateSecurityStampAsync(user);

                var activeTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Astronaut with ID {AstronautId} deactivated. Identity account locked and tokens revoked.", astronautId);
        return true;
    }

    public async Task<bool> AssignMissionAsync(int astronautId, int? missionId, CancellationToken cancellationToken = default)
    {
        var astronaut = await _dbContext.Astronauts
            .FirstOrDefaultAsync(a => a.Id == astronautId, cancellationToken);

        if (astronaut == null)
        {
            throw new KeyNotFoundException($"Astronaut with ID {astronautId} not found.");
        }

        if (missionId.HasValue)
        {
            var missionExists = await _dbContext.Missions.AnyAsync(m => m.Id == missionId.Value, cancellationToken);
            if (!missionExists)
            {
                throw new KeyNotFoundException($"Mission with ID {missionId.Value} not found.");
            }
        }

        astronaut.MissionId = missionId;
        astronaut.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

