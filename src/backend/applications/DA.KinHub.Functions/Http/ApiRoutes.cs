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

    public static class KinList
    {
        public const string Bootstrap = "api/kinlist/bootstrap";
        public const string FamilyContext = "api/kinlist/family-context";
    }
}
