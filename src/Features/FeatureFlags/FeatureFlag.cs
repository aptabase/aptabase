namespace Aptabase.Features.FeatureFlags
{
    public class FeatureFlag
    {
        public string Id { get; set; } = "";
        public string AppId { get; set; } = "";
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public string Environment { get; set; } = "";
    }
}
