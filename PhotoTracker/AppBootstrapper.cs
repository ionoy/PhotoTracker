using System.Diagnostics;
using System.Windows;

namespace PhotoTracker
{
	using System;
	using System.Collections.Generic;
	using Caliburn.Micro;

	public class AppBootstrapper : BootstrapperBase
	{
		SimpleContainer container;
	    private Window _mainWindow;

	    public AppBootstrapper()
		{
			Start();
		}

		protected override void Configure()
		{
			container = new SimpleContainer();

			container.Singleton<IWindowManager, WindowManager>();
			container.Singleton<IEventAggregator, EventAggregator>();
			container.PerRequest<IShell, MainViewModel>();
		}

		protected override object GetInstance(Type service, string key)
		{
			var instance = container.GetInstance(service, key);
			if (instance != null)
				return instance;

			throw new InvalidOperationException("Could not locate any instances.");
		}

		protected override IEnumerable<object> GetAllInstances(Type service)
		{
			return container.GetAllInstances(service);
		}

		protected override void BuildUp(object instance)
		{
			container.BuildUp(instance);
		}

		protected override void OnStartup(object sender, System.Windows.StartupEventArgs e)
		{
			DisplayRootViewFor<IShell>();

		    _mainWindow = Application.MainWindow;
		    _mainWindow.Closed += (o, args) => Process.GetCurrentProcess().Kill();
		}
	}
}