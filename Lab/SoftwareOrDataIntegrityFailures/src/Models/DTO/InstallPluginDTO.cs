namespace Models.DTO;

public class InstallPluginDTO
{
  public string name { get; set; } = null!;
  public string version { get; set; } = null!;
  public string contentBase64 { get; set; } = null!;
}
