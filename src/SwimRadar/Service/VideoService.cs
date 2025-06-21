using Microsoft.EntityFrameworkCore;
using SwimRadar;
using SwimRadar.Data;

public class VideoService(ApplicationDbContext dbContext) : IVideoService
{
    public async Task<List<SwimVideo?>> GetVideosAsync()
    {
        List<SwimVideo?> swimVideos = await dbContext.SwimVideos.ToListAsync();
        return await Task.FromResult(swimVideos);
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
        //判断Item.FileName变化了，同时修改实际的文件名称
        
        SwimVideo? swimVideo = await dbContext.SwimVideos.SingleOrDefaultAsync(x=>x.Id == item.Id);
        if (swimVideo == null)
        {
            return;
        }
        swimVideo.FileName = item.FileName;
        swimVideo.FileSize = item.FileSize;
        swimVideo.Description = item.Description;
        swimVideo.FilePath = item.FilePath;
        
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid guid)
    {
        SwimVideo? swimVideo = await dbContext.SwimVideos.SingleOrDefaultAsync(x=>x.Id == guid);

        if (swimVideo == null)
        {
            return;
        }

        dbContext.Remove(swimVideo);
        await dbContext.SaveChangesAsync();
    }
}