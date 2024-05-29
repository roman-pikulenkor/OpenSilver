using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;

namespace OpenSilver.Simulator.BlazorSupport
{
    internal sealed class BlazorDispatcher : Dispatcher
    {
        private readonly System.Windows.Threading.Dispatcher _originalDispatcher;

        public BlazorDispatcher(System.Windows.Threading.Dispatcher windowsDispatcher)
        {
            _originalDispatcher = windowsDispatcher ??
                throw new ArgumentNullException(nameof(windowsDispatcher));
        }

        public override bool CheckAccess()
            => _originalDispatcher.CheckAccess();

        public override Task InvokeAsync(Action workItem)
            => InvokeWithExceptionHandling(() =>
            {
                if (CheckAccess())
                {
                    workItem();
                    return Task.CompletedTask;
                }

                return _originalDispatcher.InvokeAsync(workItem).Task;
            });

        public override Task InvokeAsync(Func<Task> workItem)
            => InvokeWithExceptionHandling(() =>
            {
                if (CheckAccess())
                {
                    return workItem();
                }

                return _originalDispatcher.InvokeAsync(workItem).Task.Unwrap();
            });

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
            => InvokeWithExceptionHandling(() =>
            {
                if (CheckAccess())
                {
                    return Task.FromResult(workItem());
                }

                return _originalDispatcher.InvokeAsync(workItem).Task;
            });

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
            => InvokeWithExceptionHandling(() =>
            {
                if (CheckAccess())
                {
                    return workItem();
                }

                return _originalDispatcher.InvokeAsync(workItem).Task.Unwrap();
            });

        private Task<TResult> InvokeWithExceptionHandling<TResult>(Func<Task<TResult>> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                // Rethrow the exception preserving the original stack trace.
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw; // This will never be hit, but it keeps the compiler happy.
            }
        }

        private Task InvokeWithExceptionHandling(Func<Task> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                // Rethrow the exception preserving the original stack trace.
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw; // This will never be hit, but it keeps the compiler happy.
            }
        }
    }
}
