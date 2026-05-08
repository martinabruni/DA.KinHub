
using Kin.KinHub.Identity.PostgreSql.Models;

namespace Kin.KinHub.Identity.PostgreSql.AuthenticationFeature;

public sealed class ProviderRepository : PostgreSqlRepository<ProviderEntity, Provider, int>, IProviderRepository
{
    public ProviderRepository(IdentityDbContext context)
        : base(context) { }
}
