using PaperLess.Upload;
using Serilog.Events;
using Serilog;
using Microsoft.Extensions.Configuration;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json")
    .Build();

IConfigurationSection configSection = config.GetSection("Settings");

if (string.IsNullOrWhiteSpace(configSection["ApiUrl"]) || string.IsNullOrWhiteSpace(configSection["Token"]))
    Console.WriteLine("The appsettings.json file is incorrect!");
    

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();


Console.OutputEncoding = System.Text.Encoding.UTF8;

var result = DoUpload(args, configSection);

if (result != 0)
{
    Console.WriteLine("press enter to exit");
    Console.ReadLine();
}
return result;

static int DoUpload(string[] args, IConfigurationSection configSection)
{
    if (!args.Any())
    {
        Log.Error("No arguments !");
        return 1;
    }

    var file = args.First();
    Console.WriteLine("Uploading file: " + file);

    var target = configSection["ApiUrl"];
    var token = configSection["Token"]; 

    if (!File.Exists(file))
    {
        Log.Error($"File {file} does not exist");
        return 1;
    }
    if (string.IsNullOrWhiteSpace(token))
    {
        Log.Error($"Token is required");
        return 1;
    }

    var client = new PaperlessNgxClient(target, token);


    var tagIds = client.GetTags();

    var fi = new FileInfo(file);
    var now = DateTime.Now;
    var tag = $"{now.Year}-{now.Month.ToString("00")}";

    //if(args.Length > 1)
    //{
       Console.WriteLine($"Use tag [{tag}] press enter or change it:");
       var readline = Console.ReadLine();
       if(!string.IsNullOrWhiteSpace(readline))
       {
           tag = readline;
       }
    //}

    if (!tagIds.ContainsKey(tag))
    {
        var id = client.CreateTag(tag);
        if (id != default)
        {
            tagIds[tag] = id;
        }
    }

    if (!tagIds.ContainsKey(tag))
    {
        Log.Error($"Failed to create tag {tag}");
        return 1;
    }
    Console.Write($"\nUploading: {fi.Name} ");
    if (client.UploadDocument(file, tagIds[tag]))
    {
        Console.Write('\u2705');
        return 0;
    }
    else
    {
        Console.Write('\u274C');
        return 1;
    }
}

