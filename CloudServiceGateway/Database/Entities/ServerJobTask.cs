namespace CloudServiceGateway.Database.Entities
{
    public class ServerJobTask : BaseEntity
    {
        public Server Server { get; set; }
        public JobTask Task { get; set; }
    }
}