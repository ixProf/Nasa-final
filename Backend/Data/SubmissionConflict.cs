using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AstronautHealthMonitoring.Data;

public static class SubmissionConflict
{
    public static bool IsDuplicate(DbUpdateException error,string table) =>
        error.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation} pg &&
        pg.ConstraintName==$"IX_{table}_ClientGeneratedId";
}
