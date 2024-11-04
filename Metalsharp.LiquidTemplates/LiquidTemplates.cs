using System.Text;
using Fluid;

namespace Metalsharp.LiquidTemplates;

public class LiquidTemplates : IMetalsharpPlugin
{
	const string _defaultVirtualTemplateDirectory = "liquid-templates";

	readonly string _templateDirectory;
	readonly bool _loadFromFilesystem;

	readonly Dictionary<string, IFluidTemplate> _templates = new();

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
			project.LogDebug($"Adding templates in file system at {_templateDirectory} to Inputs at {_defaultVirtualTemplateDirectory}");
			project.AddInput(_templateDirectory, _defaultVirtualTemplateDirectory);
		}

		var parser = new FluidParser();

		foreach (var templateFile in project.InputFiles.Where(f => f.Directory == VirtualTemplateDirectory))
		{
			if (parser.TryParse(templateFile.Text, out IFluidTemplate template))
			{
				_templates.Add(templateFile.Name, template);
			}
		}

		foreach (var output in project.OutputFiles.Where(f => f.Extension == ".html"))
		{
			void Render(string templateName)
			{
				if (_templates.TryGetValue(templateName, out var template))
				{
					project.LogDebug($"Rendering file {output.FilePath} with template {templateName}");
					output.Contents = Encoding.Default.GetBytes(template.Render(output.GetTemplateContext()));
				}
				else
				{
					project.LogError($"Unable to parse template file {templateName}");
				}
			}

			if (output.Metadata.TryGetValue("template", out object? templateFileObject) && templateFileObject is string templateName)
			{
				Render(templateName);
			}

			Render("layout");
		}
	}
}
