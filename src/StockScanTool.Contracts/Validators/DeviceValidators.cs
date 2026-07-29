using FluentValidation;

namespace StockScanTool.Contracts.Validators;

public class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator()
    {
        RuleFor(x => x.DeviceName)
            .NotEmpty().WithMessage("Device name is required.")
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters.");

        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("Store ID must be a positive integer.");
    }
}

public class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
{
    public UpdateDeviceRequestValidator()
    {
        RuleFor(x => x.DeviceName)
            .NotEmpty().WithMessage("Device name is required.")
            .MaximumLength(100).WithMessage("Device name must not exceed 100 characters.");

        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("Store ID must be a positive integer.");
    }
}
