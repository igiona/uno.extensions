using AppKit;

namespace MyExtensionsApp.macOS;

public static class MainClass
{
	public static void Main(string[] args)
	{
		NSApplication.Init();
		NSApplication.SharedApplication.Delegate = new AppHead();
		NSApplication.Main(args);
	}
}
