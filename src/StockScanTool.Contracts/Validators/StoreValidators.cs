using FluentValidation;

namespace StockScanTool.Contracts.Validators;

public class CreateStoreRequestValidator : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(200).WithMessage("Store name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}

public class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(200).WithMessage("Store name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}
