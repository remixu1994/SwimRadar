using SwimRadar.Data;

public interface IVideoService
{
    Task<List<SwimVideo>> GetVideosAsync();
    Task<SwimVideo?> GetVideoAsync(Guid id);
    Task<VideoPreviewResult> EnsurePreviewAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步当前视频目录的信息
    /// </summary>
    Task SyncVideoInfo(string path);

    Task UpdateVideo(SwimVideo item);
    Task DeleteAsync(Guid guid);
}

public sealed record VideoPreviewResult(
    bool Success,
    VideoPreviewStatus Status,
    string? PreviewFilePath,
    string? ErrorMessage);
