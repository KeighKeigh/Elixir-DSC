﻿namespace ELIXIRETD.API.Controllers
{
    public record Error(string Code, string Message)
    {
        public static readonly Error None = new(string.Empty, string.Empty);

        public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

        public static readonly Error ConditionNotMet = new("Error.ConditionNotMet", "The specified condition was not met.");

        public static implicit operator Result(Error error) => Result.Failure(error);
        public Result ToResult() => Result.Failure(this);
    }
}
