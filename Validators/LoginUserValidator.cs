using FluentValidation;
using MinimalApiDemo.Models;

namespace MinimalApiDemo.Validators;

public class LoginUserValidator : AbstractValidator<LoginUser>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .MinimumLength(3).WithMessage("Имя пользователя должно содержать минимум 3 символа")
            .MaximumLength(50).WithMessage("Имя пользователя должно содержать максимум 50 символов")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Имя пользователя может содержать только буквы, цифры и подчеркивания");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен содержать минимум 6 символов")
            .Matches("[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву")
            .Matches("[a-z]").WithMessage("Пароль должен содержать хотя бы одну строчную букву")
            .Matches("[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру");
    }
}
