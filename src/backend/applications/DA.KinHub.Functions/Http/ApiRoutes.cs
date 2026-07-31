namespace DA.KinHub.Functions.Http;

public static class ApiRoutes
{
    public static class Health
    {
        public const string Live = "health/live";
        public const string Ready = "health/ready";
    }

    public static class Metadata
    {
        public const string Version = "api/version";
        public const string Status = "api/status";
        public const string OpenApi = "api/openapi.json";
    }

    public static class KinHub
    {
        public const string Bootstrap = "api/kinhub/bootstrap";
        public const string Families = "api/kinhub/families";
        public const string FamilyDetails = "api/kinhub/families/details";
        public const string FamilyInvitations = "api/kinhub/families/invitations";
        public const string FamilyMembers = "api/kinhub/families/members";
        public const string FamilyContext = "api/kinhub/family-context";
        public const string Services = "api/kinhub/services";
        public const string ServiceAccess = "api/kinhub/services/{serviceKey}/access";
    }

    public static class KinList
    {
        public const string Items = "api/kinlist/items";
    }
}
