using System;
using System.Collections.Generic;

namespace NexusCore.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; } // Ex: "Academe"
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public Guid? OpenIddictApplicationId { get; set; }
        public virtual ICollection<Plan> Plans { get; set; } = new List<Plan>();
    }
}