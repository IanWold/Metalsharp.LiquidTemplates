namespace Metalsharp.LiquidTemplates;

public static class Extensions
{
	public static MetalsharpProject UseLiquidTemplates(this MetalsharpProject project, string templateDirectory, bool loadFromFilesystem = true) =>
		project.Use(new LiquidTemplates(templateDirectory, loadFromFilesystem));
}
