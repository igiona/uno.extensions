﻿using Microsoft.Extensions.Hosting;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;


#if WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Playground;

public sealed partial class App : Application
{
	private Window? _window;
	public Window? Window => _window;

	public App()
	{
		this.InitializeComponent();
	}

	/// <summary>
	/// Invoked when the application is launched normally by the end user.  Other entry points
	/// will be used such as when the application is launched to open a specific file.
	/// </summary>
	/// <param name="args">Details about the launch request and process.</param>
	protected async override void OnLaunched(LaunchActivatedEventArgs args)
	{
#if DEBUG
		if (System.Diagnostics.Debugger.IsAttached)
		{
			// this.DebugSettings.EnableFrameRateCounter = true;
		}
#endif

#if NET5_0_OR_GREATER && WINDOWS
			_window = new Window();
			_window.Activate();
#else
		_window = Window.Current;
#endif





		// Option 1: Ad-hoc hosting of Navigation
		//_host = BuildAppHost();
		//var f = new Frame();
		//_window.Content = f;
		//_window.AttachServices(_host.Services);
		//f.Navigate(typeof(MainPage));
		//await Task.Run(() => _host.StartAsync());

		// Option 2: Ad-hoc hosting using root content control
		//_host = BuildAppHost();
		//var root = new ContentControl
		//{
		//	HorizontalAlignment = HorizontalAlignment.Stretch,
		//	VerticalAlignment = VerticalAlignment.Stretch,
		//	HorizontalContentAlignment = HorizontalAlignment.Stretch,
		//	VerticalContentAlignment = VerticalAlignment.Stretch
		//};
		//_window.Content = root;
		//var services = _window.AttachServices(_host.Services);
		//var startup = root.Host(services, initialRoute: "");
		//await Task.Run(() => _host.StartAsync());
		//await startup;

		// Option 3: Default hosting
		//_host = BuildAppHost();
		//_window.AttachNavigation(_host.Services,
		//	// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
		//	// initialRoute: ""
		//	// Option 2: Specify route name
		//	// initialRoute: "Shell"
		//	// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
		//	initialViewModel: typeof(ShellViewModel)
		//	);
		//await Task.Run(() => _host.StartAsync());

		// Option 4: Let Navigation do everything
		// a) No loadingview
		//_host = await _window.InitializeNavigation(BuildAppHost,
		// b) Use loadingview
		//		_host = await _window.InitializeNavigationWithExtendedSplash(BuildAppHost,
		//#if WINDOWS_UWP
		//			args.SplashScreen,
		//#else
		//			args.UWPLaunchActivatedEventArgs.SplashScreen,
		//#endif
		//			// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
		//			// initialRoute: ""
		//			// Option 2: Specify route name
		//			// initialRoute: "Shell"
		//			// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
		//			initialViewModel: typeof(ShellViewModel)
		//		);


		// Option 5: Manually create ShellView
		var root = new ShellView();
		_window.Content = root;

		_host = await _window.InitializeNavigationWithExtendedSplash(BuildAppHost,
#if !WIN_UI
					args.SplashScreen,
#else
					args.UWPLaunchActivatedEventArgs.SplashScreen,
#endif
					// Option 1: This requires Shell to be the first RouteMap - best for perf as no reflection required
					// initialRoute: ""
					// Option 2: Specify route name
					// initialRoute: "Shell"
					// Option 3: Specify the view model. To avoid reflection, you can still define a routemap
					initialViewModel: typeof(ShellViewModel),
					navigationRoot: root.SplashScreen
				);


		//var services = _window.AttachServices(_host.Services);
		//var startup = root.NavigationRoot.Host(services, initialRoute: "");
		//await Task.Run(() => _host.StartAsync());
		////await startup;
		//_window.Activate();

		var notif = _host.Services.GetRequiredService<IRouteNotifier>();
		notif.RouteChanged += RouteUpdated;


		var logger = _host.Services.GetRequiredService<ILogger<App>>();
		if (logger.IsEnabled(LogLevel.Trace)) logger.LogTraceMessage("LogLevel:Trace");
		if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebugMessage("LogLevel:Debug");
		if (logger.IsEnabled(LogLevel.Information)) logger.LogInformationMessage("LogLevel:Information");
		if (logger.IsEnabled(LogLevel.Warning)) logger.LogWarningMessage("LogLevel:Warning");
		if (logger.IsEnabled(LogLevel.Error)) logger.LogErrorMessage("LogLevel:Error");
		if (logger.IsEnabled(LogLevel.Critical)) logger.LogCriticalMessage("LogLevel:Critical");
	}


	public void RouteUpdated(object? sender, RouteChangedEventArgs? e)
	{
		try
		{
			var rootRegion = e?.Region.Root();
			var route = rootRegion?.GetRoute();
			if (route is null)
			{
				return;
			}


#if !__WASM__ && !WINUI
			CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
			{
				var appTitle = ApplicationView.GetForCurrentView();
				appTitle.Title = "Commerce: " + (route + "").Replace("+", "/");
			});
#endif

		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
		}
	}

}
