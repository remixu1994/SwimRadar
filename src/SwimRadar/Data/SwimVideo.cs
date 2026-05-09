using Microsoft.EntityFrameworkCore;

namespace SwimRadar.Data;

/// <summary>
/// Nas Swim Video Info
/// </summary>
public class SwimVideo
{
    public Guid Id { get; set; }
    
    public ApplicationUser? User { get; set; }

    public string? Description { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FilePath { get; set; }
    public string? PreviewFilePath { get; set; }
    public VideoPreviewStatus PreviewStatus { get; set; } = VideoPreviewStatus.Pending;
    public DateTime? PreviewGeneratedAt { get; set; }
    public string? PreviewError { get; set; }
}
