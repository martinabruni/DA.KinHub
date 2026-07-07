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
        TypeAdapterConfig.GlobalSettings.Apply(new IdentityMappingProfile());
        TypeAdapterConfig.GlobalSettings.Apply(new KinListMappingProfile());
        TypeAdapterConfig.GlobalSettings.Apply(new KinRecipeMappingProfile());
    }
}
