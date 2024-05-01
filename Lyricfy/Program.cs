using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LyricfyLibraries;

namespace Lyricfy;

public class Program
{
    public static async Task Main(string[] args)
    {
        
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        
        
        builder.Configuration.AddJsonFile("/appsettings.json");
        
        LyricsFetcher.Options = new LyricsFetcherOptions
        {
            ProxyImageUrlStart = builder.Configuration["xd"],
            ProxyUrlStart = builder.Configuration["xdd"]
        };

        await builder.Build().RunAsync();
    }
}