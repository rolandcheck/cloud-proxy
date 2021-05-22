using System;

namespace CloudServiceGateway.Database.Entities
{
    public abstract class BaseEntity 
    {
        public Guid Id { get; set; }
    }
}
