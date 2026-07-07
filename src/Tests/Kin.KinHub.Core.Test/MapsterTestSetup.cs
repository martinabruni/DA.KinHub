using System.Runtime.CompilerServices;
using Kin.KinHub.Identity.Business.Common;
using Kin.KinHub.KinList.Business.Common;
using Kin.KinHub.KinRecipe.Business.Common;
using Mapster;

namespace Kin.KinHub.Core.Test;

internal static class MapsterTestSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        new IdentityMappingProfile().Register(TypeAdapterConfig.GlobalConfig);
        new KinListMappingProfile().Register(TypeAdapterConfig.GlobalConfig);
        new KinRecipeMappingProfile().Register(TypeAdapterConfig.GlobalConfig);
    }
}
