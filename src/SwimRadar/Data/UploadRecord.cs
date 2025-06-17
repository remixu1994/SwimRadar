namespace SwimRadar.Data;

public class UploadRecord
{
    public Guid Id { get; set; }

    public required string FileName { get; set; }

    public DateTime DateTime { get; set; }

    public long FileSize { get; set; }
    
    public ApplicationUser User { get; set; }

    public required string UploadPath { get; set; }
}