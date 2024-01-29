// See https://aka.ms/new-console-template for more information
using circuitbreaker.example.services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");

var services = new ServiceCollection();
services.AddLogging(options =>
{
    options.ClearProviders();
    options.AddConsole();
});
services.AddSingleton<WaterTreatmentPlantService>();

var serviceProvider = services.BuildServiceProvider();
var waterTreatmentPlantService = serviceProvider.GetRequiredService<WaterTreatmentPlantService>();

var originsByDates = await waterTreatmentPlantService.GetOriginsByDates();

Console.WriteLine(originsByDates);
Console.ReadLine();