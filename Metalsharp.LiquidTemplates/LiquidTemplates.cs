using Fluid;

namespace Metalsharp.LiquidTemplates;

public class LiquidTemplates : IMetalsharpPlugin
{
	const string _defaultVirtualTemplateDirectory = "liquid-templates";

	readonly string _templateDirectory;
	readonly bool _loadFromFilesystem;

	/// <summary>
	/// Instantiates `LiquidTemplates`. The template files can be added to Metalsharp manually, or `LiquidTemplates` can add them automatically.
	/// </summary>
	/// <param name="templateDirectory">The directory in which the liquid template files are located. If `loadFromFileSystem` is `true`, then this is the name of the directory on disk. If `loadFromFileSystem` is false, then this is the name of the virtual directory in Metalsharp.</param>
	/// <param name="loadFromFilesystem">Whether `LiquidTemplates` should add the template files to Metalsharp.</param>
	public LiquidTemplates(string templateDirectory, bool loadFromFilesystem = true)
	{
		_templateDirectory = templateDirectory;
		_loadFromFilesystem = loadFromFilesystem;
	}

	/// <summary>
	/// The name of the virtual directory in Metalsharp containing the liquid template files. This can be manually-specified, or a default directory if `LiquidTemplates` automatically adds the input files to Metalsharp.
	/// </summary>
	string VirtualTemplateDirectory =>
		_loadFromFilesystem
		? _defaultVirtualTemplateDirectory
		: _templateDirectory;

	public void Execute(MetalsharpProject project)
	{
		if (_loadFromFilesystem)
		{
			project.AddInput(_templateDirectory, _defaultVirtualTemplateDirectory);
		}
		IFluidTemplate? layout = null;

		if (project.InputFiles.SingleOrDefault(f => f.FilePath == $"{VirtualTemplateDirectory}\\layout.liquid") is MetalsharpFile layoutFile)
		{
			new FluidParser().TryParse(layoutFile.Text, out layout);
		}

		foreach (var output in project.OutputFiles.Where(f => f.Extension == ".html"))
		{
			if (output.Metadata.TryGetValue("template", out object? templateFileObject)
				&& templateFileObject is string templateFilePath
				&& project.InputFiles.SingleOrDefault(f => f.FilePath == templateFilePath) is MetalsharpFile templateFile
				&& new FluidParser().TryParse(templateFile.Text, out IFluidTemplate template)
			)
			{
				output.Text = template.Render(new TemplateContext(new { content = output.Text }));
			}

			if (layout is not null)
			{
				output.Text = layout.Render(new TemplateContext(new { content = output.Text }));
			}
		}
	}
}
