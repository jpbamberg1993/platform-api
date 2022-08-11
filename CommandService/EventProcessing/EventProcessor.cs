using System.Formats.Asn1;
using System.Text.Json;
using AutoMapper;
using CommandService.Data;
using CommandService.Dtos;
using CommandService.Models;

namespace CommandService.EventProcessing;

public class EventProcessor : IEventProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;

    public EventProcessor(IServiceScopeFactory scopeFactory, IMapper mapper)
    {
        _scopeFactory = scopeFactory;
        _mapper = mapper;
    }
    
    public void ProcessEvent(string message)
    {
        var eventType = DetermineEvent(message);

        switch (eventType)
        {
            case EventType.PlatformPublished:
                AddPlatform(message);
                break;
            default:
                break;
        }
    }

    private EventType DetermineEvent(string notificationMessage)
    {
        Console.WriteLine("--> Determining event.");

        var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

        switch (eventType.Event)
        {
            case "Platform_Published":
                Console.WriteLine("--> Platform published event detected.");
                return EventType.PlatformPublished;
            default:
                Console.WriteLine("--> Could not determine event type.");
                return EventType.Undetermined;
        }
    }

    private void AddPlatform(string platformPublishMessage)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICommandRepo>();

        var platformPublishDto = JsonSerializer.Deserialize<PlatformPublishDto>(platformPublishMessage);

        try
        {
            var platform = _mapper.Map<Platform>(platformPublishDto);
            if (!repo.PlatformExistsWithExternalId(platform.ExternalId))
            {
                repo.CreatePlatform(platform);
                repo.SaveChanges();
                Console.WriteLine("--> Platform created.");
            }
            else
            {
                Console.WriteLine("--> Platform already exists.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"--> Could not add Platform to db. {e.Message}");
            Console.WriteLine(e);
        }
    }
}

public enum EventType
{
    PlatformPublished,
    Undetermined,
}