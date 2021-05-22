using CloudServiceGateway.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloudServiceGateway.Database
{
    public class CsContext : DbContext
    {
        public CsContext(DbContextOptions<CsContext> options) : base(options) { }
        
        public DbSet<Server> Servers { get; set; }
        public DbSet<JobTask> Tasks { get; set; }
        public DbSet<ServerJobTask> ServerJobTasks { get; set; }
    }
}