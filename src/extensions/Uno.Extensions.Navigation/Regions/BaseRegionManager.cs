﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions;

public abstract class BaseRegionManager : IRegionManager
{
    protected ILogger Logger { get; }

    public abstract NavigationContext CurrentContext { get; }

    protected INavigationService Navigation { get; }

    protected Stack<Dialog> OpenDialogs { get; } = new Stack<Dialog>();

    protected virtual bool CanGoBack => false;

    protected IViewModelManager ViewModelManager { get; }

    private IDialogFactory DialogProvider { get; }

    public BaseRegionManager(
        ILogger logger,
        INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory)
    {
        Logger = logger;
        Navigation = navigation;
        ViewModelManager = viewModelManager;
        DialogProvider = dialogFactory;
    }

    public async Task NavigateAsync(NavigationContext context)
    {
        var request = context.Request;

        var navigationHandled = await EndCurrentNavigationContext(context);

        if (context.CancellationToken.IsCancellationRequested)
        {
            await Task.FromCanceled(context.CancellationToken);
        }

        if (!navigationHandled)
        {
            await DoNavigation(context);

            await ViewModelManager.StartViewModel(context);
        }

        context = context with { CanCancel = CanGoBack || OpenDialogs.Any() };

        if (context.CanCancel)
        {
            context.CancellationToken.Register(() =>
            {
                Navigation.NavigateToPreviousViewAsync(context.Request.Sender);
            });
        }
    }

    protected virtual Task DoNavigation(NavigationContext context)
    {
        return DoForwardNavigation(context);
    }

    protected async Task DoForwardNavigation(NavigationContext context)
    {
        ViewModelManager.CreateViewModel(context);

        await ViewModelManager.InitializeViewModel(context);

        var dialog = DialogProvider.CreateDialog(Navigation, context);
        if (dialog is not null)
        {
            OpenDialogs.Push(dialog);
        }
        else
        {
            RegionNavigate(context);
        }
    }

    protected abstract void RegionNavigate(NavigationContext context);

    private async Task<bool> EndCurrentNavigationContext(NavigationContext navigationContext)
    {
        // If this is back navigation, then make sure it's used to close
        // any of the open dialogs
        if (navigationContext.IsBackNavigation && OpenDialogs.Any())
        {
            await CloseDialog(navigationContext);
            return true;
        }

        // If there's a current nav context, make sure it's stopped before
        // we proceed - this could cancel the navigation, so need to know
        // before we remove anything from backstack
        if (CurrentContext is not null)
        {
            // Stop the currently active viewmodel
            await ViewModelManager.StopViewModel(CurrentContext);

            if (!CanGoBack || navigationContext.IsBackNavigation)
            {
                ViewModelManager.DisposeViewModel(CurrentContext);
            }

            // Check if navigation was cancelled - if it is,
            // then indicate that navigation has been handled
            if (navigationContext.IsCancelled)
            {
                var completion = navigationContext.ResultCompletion;
                if (completion is not null)
                {
                    completion.SetResult(Options.Option.None<object>());
                }

                return true;
            }

            // If this is a back navigation then we need to pass back
            // any data to the current context. This is done by setting
            // the results on the ResultCompletion object
            // Note: We note performing the back navigation here, we just
            // passing data back to any caller that's waiting on it.
            if (navigationContext.IsBackNavigation)
            {
                var responseData = navigationContext.Data.TryGetValue(string.Empty, out var response) ? response : default;

                var context = CurrentContext;

                var completion = context.ResultCompletion;
                if (completion is not null)
                {
                    if (context.Request.Result is not null && responseData is not null)
                    {
                        completion.SetResult(Options.Option.Some<object>(responseData));
                    }
                    else
                    {
                        completion.SetResult(Options.Option.None<object>());
                    }
                }
            }
        }

        return false;
    }

    protected async Task CloseDialog(NavigationContext navigationContext)
    {
        var dialog = OpenDialogs.Pop();

        var responseData = navigationContext.Data.TryGetValue(string.Empty, out var response) ? response : default;

        await ViewModelManager.StopViewModel(navigationContext);

        ViewModelManager.DisposeViewModel(navigationContext);

        responseData = dialog.Manager.CloseDialog(dialog, navigationContext, responseData);

        var completion = dialog.Context.ResultCompletion;
        if (completion is not null)
        {
            if (dialog.Context.Request.Result is not null && responseData is not null)
            {
                completion.SetResult(Options.Option.Some<object>(responseData));
            }
            else
            {
                completion.SetResult(Options.Option.None<object>());
            }
        }

        await ViewModelManager.StartViewModel(CurrentContext);
    }
}
