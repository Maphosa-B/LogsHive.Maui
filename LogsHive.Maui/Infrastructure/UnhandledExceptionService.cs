using System;
using LogsHive.Maui.Services;

namespace LogsHive.Maui.Infrastructure;

internal sealed class UnhandledExceptionService
{
    private readonly LogsHiveService _logs;

    public UnhandledExceptionService(LogsHiveService logs)
    {
        _logs = logs;
    }


    public void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

        TaskScheduler.UnobservedTaskException += TaskSchedulerUnhandledException;


#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser
            += AndroidUnhandledException;
#endif


        _logs.LogLocally("[LogsHive] Unhandled exception handlers registered.");
    }


    private void CurrentDomainUnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Capture(exception, "AppDomain");
        }
    }


    private void TaskSchedulerUnhandledException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();

        Capture(e.Exception, "TaskScheduler");
    }


#if ANDROID

    private void AndroidUnhandledException(
        object? sender,
        Android.Runtime.RaiseThrowableEventArgs e)
    {
        Capture(
            new Exception(
                e.Exception?.Message,
                e.Exception),
            "Android");
    }

#endif


    private void Capture(Exception exception, string source)
    {
        _ = _logs.CaptureAsync(
            exception,
            new Dictionary<string, string>
            {
                ["CaptureType"] = "Unhandled",
                ["Source"] = source
            });
    }
}