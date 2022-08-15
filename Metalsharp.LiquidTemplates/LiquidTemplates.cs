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
			project.Log.Debug($"Adding templates in file system at {_templateDirectory} to Inputs at {_defaultVirtualTemplateDirectory}");
			project.AddInput(_templateDirectory, _defaultVirtualTemplateDirectory);
		}
		IFluidTemplate? layout = null;

		if (project.InputFiles.SingleOrDefault(f => f.FilePath == $"{VirtualTemplateDirectory}\\layout.liquid") is MetalsharpFile layoutFile)
		{
			project.Log.Debug("Parsing layout.liquid...");

			var success = new FluidParser().TryParse(layoutFile.Text, out layout);

			if (success)
			{
				project.Log.Debug("Parsed layout.liquid");
			}
			else
			{
				project.Log.Error("Did not parse layout.liquid");
			}
		}

		foreach (var output in project.OutputFiles.Where(f => f.Extension == ".html"))
		{
			if (output.Metadata.TryGetValue("template", out object? templateFileObject) && templateFileObject is string templateFilePath)
			{
				if (project.InputFiles.SingleOrDefault(f => f.FilePath == templateFilePath) is MetalsharpFile templateFile)
				{
					if (new FluidParser().TryParse(templateFile.Text, out IFluidTemplate template))
					{
						project.Log.Debug($"Rendering file {output.FilePath} with template {templateFilePath}");
						output.Text = template.Render(new TemplateContext(new { content = output.Text }));
					}
					else
					{
						project.Log.Error($"Unable to parse template file {templateFilePath}");
					}
				}
				else
				{
					project.Log.Error($"Unable to find template file {templateFilePath}");
				}
			}

			if (layout is not null)
			{
				project.Log.Debug($"Rendering file {output.FilePath} with layout");
				output.Text = layout.Render(new TemplateContext(new { content = output.Text }));
			}
		}
	}
}
