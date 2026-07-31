using FluentValidation;

namespace StockScanTool.Contracts.Validators;

public class CreatePermissionRequestValidator : AbstractValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Permission code is required.")
            .MaximumLength(100).WithMessage("Permission code must not exceed 100 characters.")
            .Matches("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$")
            .WithMessage("Permission code must be lowercase dot notation, e.g. reports.view.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Permission name is required.")
            .MaximumLength(200).WithMessage("Permission name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Permission group is required.")
            .MaximumLength(100).WithMessage("Group name must not exceed 100 characters.");
    }
}

public class UpdatePermissionRequestValidator : AbstractValidator<UpdatePermissionRequest>
{
    public UpdatePermissionRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Permission code is required.")
            .MaximumLength(100).WithMessage("Permission code must not exceed 100 characters.")
            .Matches("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$")
            .WithMessage("Permission code must be lowercase dot notation, e.g. reports.view.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Permission name is required.")
            .MaximumLength(200).WithMessage("Permission name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Permission group is required.")
            .MaximumLength(100).WithMessage("Group name must not exceed 100 characters.");
    }
}
