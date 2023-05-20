using Remora.Results;

namespace Kobalt.Shared.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Attempts to execute the given lambda, transforming any exceptions into a <see cref="Result"/> with the exception as the error.
    /// </summary>
    /// <param name="lambda">The delegate to execute.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public static Result TryCatch(Action lambda)
    {
        try
        {
            lambda();
            return Result.FromSuccess();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// Attempts to execute the given lambda, transforming any exceptions into a <see cref="Result"/> with the exception as the error.
    /// </summary>
    /// <param name="lambda">The delegate to execute.</param>
    /// <returns>A result that may or not have succeeded, with a result in the former case.</returns>
    public static Result<T> TryCatch<T>(Func<T> lambda)
    {
        try
        {
            return lambda();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// Attempts to execute the given lambda, transforming any exceptions into a <see cref="Result"/> with the exception as the error.
    /// </summary>
    /// <param name="lambda">The delegate to execute.</param>
    /// <param name="state">A state object passed to the delegate.</param>
    /// <returns>A result that may or not have succeeded, with a result in the former case.</returns>
    public static Result<T> TryCatch<TState, T>(Func<TState, T> lambda, TState state)
    {
        try
        {
            return lambda(state);
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// Attempts to execute the given lambda, transforming any exceptions into a <see cref="Result"/> with the exception as the error.
    /// </summary>
    /// <param name="lambda">The delegate to execute.</param>
    /// <returns>A result that may or not have succeeded, returning the result in the former case.</returns>
    public static async Task<Result> TryCatchAsync(Func<Task> lambda)
    {
        try
        {
            await lambda();
            return Result.FromSuccess();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>
    /// Attempts to execute the given lambda, transforming any exceptions into a <see cref="Result"/> with the exception as the error.
    /// </summary>
    /// <param name="lambda">The delegate to execute.</param>
    /// <returns>A result that may or not have succeeded, returning the result in the former case.</returns>
    public static async Task<Result<T>> TryCatchAsync<T>(Func<Task<T>> lambda)
    {
        try
        {
            return await lambda();
        }
        catch (Exception e)
        {
            return e;
        }
    }
}
