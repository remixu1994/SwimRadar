using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SwimRadar;
using SwimRadar.Data;

public class VideoService(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<VideoService> logger) : IVideoService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> PreviewLocks = new();

    public async Task<List<SwimVideo>> GetVideosAsync()
    {
        List<SwimVideo> swimVideos = await dbContext.SwimVideos.ToListAsync();
        return await Task.FromResult(swimVideos);
    }

    public Task<SwimVideo?> GetVideoAsync(Guid id)
    {
        return dbContext.SwimVideos.SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task<VideoPreviewResult> EnsurePreviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var previewLock = PreviewLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await previewLock.WaitAsync(cancellationToken);

        try
        {
            SwimVideo? swimVideo = await dbContext.SwimVideos.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (swimVideo is null)
            {
                return new VideoPreviewResult(false, VideoPreviewStatus.Failed, null, "Video not found.");
            }

            if (swimVideo.PreviewStatus == VideoPreviewStatus.Ready &&
                !string.IsNullOrWhiteSpace(swimVideo.PreviewFilePath) &&
                File.Exists(swimVideo.PreviewFilePath))
            {
                return new VideoPreviewResult(true, VideoPreviewStatus.Ready, swimVideo.PreviewFilePath, null);
            }

            if (!File.Exists(swimVideo.FilePath))
            {
                swimVideo.PreviewStatus = VideoPreviewStatus.Failed;
                swimVideo.PreviewError = "Original video file was not found.";
                await dbContext.SaveChangesAsync(cancellationToken);
                return new VideoPreviewResult(false, swimVideo.PreviewStatus, null, swimVideo.PreviewError);
            }

            string previewDirectory = Path.Combine(Const.RootPath, ".previews");
            Directory.CreateDirectory(previewDirectory);

            string previewPath = Path.Combine(previewDirectory, $"{swimVideo.Id}.mp4");
            string temporaryPreviewPath = Path.Combine(previewDirectory, $"{swimVideo.Id}.tmp.mp4");

            swimVideo.PreviewStatus = VideoPreviewStatus.Processing;
            swimVideo.PreviewError = null;
            swimVideo.PreviewFilePath = previewPath;
            await dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                if (File.Exists(temporaryPreviewPath))
                {
                    File.Delete(temporaryPreviewPath);
                }

                await GeneratePreviewAsync(swimVideo.FilePath, temporaryPreviewPath, cancellationToken);

                if (File.Exists(previewPath))
                {
                    File.Delete(previewPath);
                }

                File.Move(temporaryPreviewPath, previewPath);

                swimVideo.PreviewStatus = VideoPreviewStatus.Ready;
                swimVideo.PreviewGeneratedAt = DateTime.UtcNow;
                swimVideo.PreviewError = null;
                await dbContext.SaveChangesAsync(cancellationToken);

                return new VideoPreviewResult(true, swimVideo.PreviewStatus, previewPath, null);
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                logger.LogWarning(exception, "Failed to generate video preview for {VideoId}", swimVideo.Id);

                if (File.Exists(temporaryPreviewPath))
                {
                    File.Delete(temporaryPreviewPath);
                }

                swimVideo.PreviewStatus = VideoPreviewStatus.Failed;
                swimVideo.PreviewError = exception.Message;
                await dbContext.SaveChangesAsync(cancellationToken);

                return new VideoPreviewResult(false, swimVideo.PreviewStatus, null, swimVideo.PreviewError);
            }
        }
        finally
        {
            previewLock.Release();
        }
    }

    /// <summary>
    /// 同步当前视频目录的信息
    /// </summary>
    public async Task SyncVideoInfo(string path)
    {
        var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).ToList();
        directories.Add(path);
        string lastDirectoryName = Path.GetFileName(path);
        var user = await dbContext.Users.SingleOrDefaultAsync(x =>
            !string.IsNullOrEmpty(x.Name) &&
            x.Name.ToLower().Equals(lastDirectoryName));
        foreach (string directory in directories)
        {
            var files = Directory.GetFiles(directory, "*.mp4");
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var swimVideo = new SwimVideo
                {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                    FilePath = fileInfo.FullName,
                    PreviewStatus = VideoPreviewStatus.Pending,
                    User = user
                };
                if (!dbContext.SwimVideos.Any(x => x.FileName == fileInfo.Name && x.FileSize == fileInfo.Length))
                {
                    await dbContext.SwimVideos.AddAsync(swimVideo);
                }
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateVideo(SwimVideo item)
    {
        SwimVideo? swimVideo = await dbContext.SwimVideos.SingleOrDefaultAsync(x => x.Id == item.Id);
        if (swimVideo == null)
        {
            return;
        }
        bool sourceChanged = swimVideo.FilePath != item.FilePath || swimVideo.FileSize != item.FileSize;

        swimVideo.FileName = item.FileName;
        swimVideo.FileSize = item.FileSize;
        swimVideo.Description = item.Description;
        swimVideo.FilePath = item.FilePath;

        if (sourceChanged)
        {
            swimVideo.PreviewFilePath = null;
            swimVideo.PreviewStatus = VideoPreviewStatus.Pending;
            swimVideo.PreviewGeneratedAt = null;
            swimVideo.PreviewError = null;
        }
        else
        {
            swimVideo.PreviewFilePath = item.PreviewFilePath;
            swimVideo.PreviewStatus = item.PreviewStatus;
            swimVideo.PreviewGeneratedAt = item.PreviewGeneratedAt;
            swimVideo.PreviewError = item.PreviewError;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid guid)
    {
        SwimVideo? swimVideo = await dbContext.SwimVideos.SingleOrDefaultAsync(x => x.Id == guid);

        if (swimVideo == null)
        {
            return;
        }

        dbContext.Remove(swimVideo);
        await dbContext.SaveChangesAsync();
    }

    private async Task GeneratePreviewAsync(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        string ffmpegPath = configuration["VideoPreview:FFmpegPath"] ?? "ffmpeg";
        string arguments = string.Join(" ", [
            "-y",
            "-i", Quote(inputPath),
            "-vf", Quote("scale=w='min(1280,iw)':h='min(720,ih)':force_original_aspect_ratio=decrease"),
            "-c:v libx264",
            "-preset veryfast",
            "-b:v 2500k",
            "-maxrate 2800k",
            "-bufsize 5600k",
            "-c:a aac",
            "-b:a 128k",
            "-movflags +faststart",
            Quote(outputPath)
        ]);

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException("FFmpeg is not installed or VideoPreview:FFmpegPath is invalid.", exception);
        }

        string standardError = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {standardError}");
        }

        if (!File.Exists(outputPath))
        {
            throw new InvalidOperationException("FFmpeg finished but did not create a preview file.");
        }
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
