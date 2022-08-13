using Fluid;

namespace Metalsharp.LiquidTemplates;

public class LiquidTemplates : IMetalsharpPlugin
{
	const string _defaultTemplateDirectory = "liquid-templates";

	readonly string _templateDirectory;
	readonly bool _loadFromFilesystem;

	public LiquidTemplates(string templateDirectory, bool loadFromFilesystem = true)
	{
		_templateDirectory = templateDirectory;
		_loadFromFilesystem = loadFromFilesystem;
	}

	string VirtualTemplateDirectory =>
		_loadFromFilesystem
		? _defaultTemplateDirectory
		: _templateDirectory;

	public void Execute(MetalsharpProject project)
	{
		if (_loadFromFilesystem)
		{
			project.AddInput(_templateDirectory, "fluid-templates");
		}
		IFluidTemplate? layout = null;

		if (project.InputFiles.SingleOrDefault(f => f.FilePath == $"{VirtualTemplateDirectory}\\layout.liquid") is MetalsharpFile layoutFile)
		{
			new FluidParser().TryParse(layoutFile.Text, out layout);
		}

		foreach (var output in project.OutputFiles)
		{
			if (output.Metadata.TryGetValue("template", out object? templateFileObject)
				&& templateFileObject is string templateFilePath
				&& project.InputFiles.SingleOrDefault(f => f.FilePath == templateFilePath) is MetalsharpFile templateFile
				&& new FluidParser().TryParse(templateFile.Text, out var template)
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
