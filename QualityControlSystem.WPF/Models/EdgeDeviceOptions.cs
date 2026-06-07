namespace QualityControlSystem.WPF.Models
{
    public class EdgeDeviceOptions
    {
        public string Host { get; set; } = string.Empty;
        public int SshPort { get; set; } = 22;
        public string UserName { get; set; } = string.Empty;
        public string? PrivateKeyPath { get; set; }
        public string RemoteDirectory { get; set; } = string.Empty;
        public string VirtualEnvironmentPath { get; set; } = string.Empty;
        public string PhotomakerScript { get; set; } = string.Empty;
        public string OperatingScript { get; set; } = string.Empty;
        public string RemoteTemplateDirectory { get; set; } = string.Empty;
        public string LocalTemplateDirectory { get; set; } = "Templates";
        public int PhotomakerPort { get; set; } = 8081;
        public int OperatingPort { get; set; } = 8082;
        public int SshCommandTimeoutSeconds { get; set; } = 20;
    }
}
