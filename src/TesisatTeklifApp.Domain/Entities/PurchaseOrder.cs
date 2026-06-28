using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>Satınalma siparişi (tedarikçiye verilen).</summary>
public class PurchaseOrder : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;   // SA-2026-0001

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Today;
    public DateTime? ExpectedDate { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

/// <summary>Satınalma siparişi kalemi.</summary>
public class PurchaseOrderItem : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public decimal ReceivedQuantity { get; set; }
    public bool IsReceived { get; set; }
}
