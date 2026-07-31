using FluentValidation;

namespace StockScanTool.Contracts.Validators;

public class SubmitSaleRequestValidator : AbstractValidator<SubmitSaleRequest>
{
    public SubmitSaleRequestValidator()
    {
        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("Store ID must be a positive integer.");

        RuleFor(x => x.ScanningDeviceId)
            .GreaterThan(0).WithMessage("Scanning device ID must be a positive integer.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Sale must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("Product ID must be a positive integer.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }
}

public class DeviceLoginRequestValidator : AbstractValidator<DeviceLoginRequest>
{
    public DeviceLoginRequestValidator()
    {
        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("API key is required.")
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("API key is required.");
    }
}

public class AdminLoginRequestValidator : AbstractValidator<AdminLoginRequest>
{
    public AdminLoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
