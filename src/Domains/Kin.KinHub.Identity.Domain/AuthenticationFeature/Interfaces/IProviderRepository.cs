using Kin.KinHub.Shared.Kernel.Interfaces;
using Kin.KinHub.Shared.Kernel.Models;

namespace Kin.KinHub.Identity.Domain.AuthenticationFeature;

public interface IProviderRepository
 : IRepository<Provider, int>
{

}
