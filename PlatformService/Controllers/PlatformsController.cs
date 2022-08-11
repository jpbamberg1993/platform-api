using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;
using RabbitMQ.Client;

namespace PlatformService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlatformsController : ControllerBase
{
    private readonly IPlatformRepo _platformRepo;
    private readonly IMapper _mapper;
    private readonly ICommandDataClient _commandDataClient;
    private readonly IMessageBusClient _messageBusClient;

    public PlatformsController(
        IPlatformRepo platformRepo,
        IMapper mapper,
        ICommandDataClient commandDataClient,
        IMessageBusClient messageBusClient)
    {
        _platformRepo = platformRepo;
        _mapper = mapper;
        _commandDataClient = commandDataClient;
        _messageBusClient = messageBusClient;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
        Console.WriteLine("--> Getting platforms...");
        var platformItems = _platformRepo.GetAllPlatforms();
        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platformItems));
    }

    [HttpGet("{id}", Name = nameof(GetPlatformById))]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
        var platformItem = _platformRepo.GetPlatformById(id);
        if (platformItem != null)
        {
            return Ok(_mapper.Map<PlatformReadDto>(platformItem));
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto platformCreateDto)
    {
        var platformModel = _mapper.Map<Platform>(platformCreateDto);
        _platformRepo.CreatePlatform(platformModel);
        _platformRepo.SaveChanges();

        var platformReadDto = _mapper.Map<PlatformReadDto>(platformModel);

        // send sync message
        try
        {
            await _commandDataClient.SendPlatformToCommand(platformReadDto);
        }
        catch (Exception e)
        {
            Console.WriteLine($"--> Could not send synchronously: {e.Message}.");
        }

        // send async message
        try
        {
            var platformPublishedDto = _mapper.Map<PlatformPublishDto>(platformModel);
            platformPublishedDto.Event = "Platform_Published";
            _messageBusClient.PublishNewPlatform(platformPublishedDto);
        }
        catch (Exception e)
        {
            Console.WriteLine($"--> Could not send asynchronously: {e.Message}");
            Console.WriteLine(e);
        }

        return CreatedAtRoute(nameof(GetPlatformById), new { Id = platformReadDto.Id }, platformReadDto);
    }
}