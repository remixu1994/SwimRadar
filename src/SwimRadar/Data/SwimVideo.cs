using Microsoft.EntityFrameworkCore;

namespace SwimRadar.Data;

public class SwimVideo
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }

    public string VideoDescription { get; set; }
    
}