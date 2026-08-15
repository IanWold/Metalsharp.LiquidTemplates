using System.Text;
using Fluid;

namespace Metalsharp.LiquidTemplates;

/// <summary>
/// Instantiates `LiquidTemplates`. The template files can be added to Metalsharp manually, or `LiquidTemplates` can add them automatically.
/// </summary>
/// <param name="templateDirectory">The directory in which the liquid template files are located. If `loadFromFileSystem` is `true`, then this is the name of the directory on disk. If `loadFromFileSystem` is false, then this is the name of the virtual directory in Metalsharp.</param>
/// <param name="loadFromFilesystem">Whether `LiquidTemplates` should add the template files to Metalsharp.</param>
public class LiquidTemplates(string templateDirectory, bool loadFromFilesystem = true) : IMetalsharpPlugin
{
	const string _defaultVirtualTemplateDirectory = "liquid-templates";

	readonly Dictionary<string, IFluidTemplate> _templates = [];

	/// <summary>
	/// The name of the virtual directory in Metalsharp containing the liquid template files. This can be manually-specified, or a default directory if `LiquidTemplates` automatically adds the input files to Metalsharp.
	/// </summary>
	string VirtualTemplateDirectory =>
		loadFromFilesystem
		? _defaultVirtualTemplateDirectory
		: templateDirectory;

	/// <summary>
	/// Loads the liquid template files and renders them against the HTML files in the Metalsharp project's output collection.
	/// </summary>
	/// <param name="project">The Metalsharp project.</param>
	public void Execute(MetalsharpProject project)
	{
		if (loadFromFilesystem)
		{
			project.LogDebug($"Adding templates in file system at {templateDirectory} to Inputs at {_defaultVirtualTemplateDirectory}");
			project.AddInput(templateDirectory, _defaultVirtualTemplateDirectory);
		}

		var parser = new FluidParser();

		foreach (var templateFile in project.InputFiles.Where(f => f.Directory == VirtualTemplateDirectory))
		{
			if (parser.TryParse(templateFile.Text, out var template))
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

			if (output.Metadata.TryGetValue("template", out var templateFileObject) && templateFileObject is string templateName)
			{
				Render(templateName);
			}

			Render("layout");
		}
	}
}
