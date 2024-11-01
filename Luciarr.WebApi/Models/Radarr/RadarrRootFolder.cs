namespace Luciarr.WebApi.Models.Radarr
{
    public class RadarrRootFolder
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public bool Accessible { get; set; }
        public long FreeSpace { get; set; }
    }
}
