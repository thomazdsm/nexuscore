using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NexusCore.Domain.Entities
{
    public class Plan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } // Ex: "Plano Profissional"
        public decimal Price { get; set; }
        public string BillingCycle { get; set; } // Ex: "Monthly", "Yearly"
        public bool IsActive { get; set; }

        // Relação com Produto
        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}