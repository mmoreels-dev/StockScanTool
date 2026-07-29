using FluentValidation;

namespace StockScanTool.Contracts.Validators;

public class UpdateInventoryRequestValidator : AbstractValidator<UpdateInventoryRequest>
{
    public UpdateInventoryRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Product ID must be a positive integer.");

        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("Store ID must be a positive integer.");

        RuleFor(x => x.QuantityOnHand)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity on hand cannot be negative.");
    }
}
