using FluentValidation;
using MinimalApiDemo.DTOs;

namespace MinimalApiDemo.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно")
            .MinimumLength(2).WithMessage("Имя должно содержать минимум 2 символа")
            .MaximumLength(100).WithMessage("Имя должно содержать максимум 100 символов")
            .Matches("^[a-zA-Zа-яА-ЯёЁ\\s]+$").WithMessage("Имя может содержать только буквы и пробелы");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email")
            .MaximumLength(100).WithMessage("Email должен содержать максимум 100 символов");
    }
}
