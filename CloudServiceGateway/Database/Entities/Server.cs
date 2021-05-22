namespace CloudServiceGateway.Database.Entities
{
    public class Server : BaseEntity
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Scheme { get; set; }
    }
}