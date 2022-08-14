namespace Metalsharp.LiquidTemplates;

public static class Extensions
{
	/// <summary>
	/// `LiquidTemplates` automatically applies liquid template files to HTML files in Metalsharp's output collection. 
	/// 
	/// If an output file specifies a specific liquid template file in its metadata with the key "template", then that template file will be applied. If there is a "layout.liquid" file present in the specified template directory, that layout file will be applied to all HTML files regardless of whether they specify a template, and will be applied after the template file.
	/// </summary>
	/// <param name="project">The Metalsharp project.</param>
	/// <param name="templateDirectory">The directory in which the liquid template files are located. If `loadFromFileSystem` is `true`, then this is the name of the directory on disk. If `loadFromFileSystem` is false, then this is the name of the virtual directory in Metalsharp.</param>
	/// <param name="loadFromFilesystem">Whether `LiquidTemplates` should add the template files to Metalsharp.</param>
	/// <returns></returns>
	public static MetalsharpProject UseLiquidTemplates(this MetalsharpProject project, string templateDirectory, bool loadFromFilesystem = true) =>
		project.Use(new LiquidTemplates(templateDirectory, loadFromFilesystem));
}
