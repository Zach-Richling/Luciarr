namespace Luciarr.Web.Data
{
    public class ConfigSetting
    {
        public string Id { get; set; }
        public string? Value { get; set; }

        public ConfigSetting(string Id, string? Value)
        {
            this.Id = Id;
            this.Value = Value;
        }
    }
}
