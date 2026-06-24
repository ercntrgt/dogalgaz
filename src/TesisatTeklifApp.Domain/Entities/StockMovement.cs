using TesisatTeklifApp.Domain.Common;
using TesisatTeklifApp.Domain.Enums;

namespace TesisatTeklifApp.Domain.Entities;

/// <summary>
/// Stok hareket geçmişi kaydı. Her düşüş/iade/giriş burada loglanır.
/// </summary>
public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int? OfferId { get; set; }
    public Offer? Offer { get; set; }

    public MovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }
    public string? Description { get; set; }
}
