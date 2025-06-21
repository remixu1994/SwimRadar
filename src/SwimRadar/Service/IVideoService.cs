using SwimRadar.Data;

public interface IVideoService
{
    Task<List<SwimVideo?>> GetVideosAsync();

    /// <summary>
    /// 同步当前视频目录的信息
    /// </summary>
    Task SyncVideoInfo(string path);

    Task UpdateVideo(SwimVideo item);
    Task DeleteAsync(Guid guid);
}