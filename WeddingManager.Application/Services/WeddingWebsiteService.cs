using System.Text.Json;
using WeddingManager.Application.Mappings;
using WeddingManager.Domain.DTO;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;

namespace WeddingManager.Application.Services;

public class WeddingWebsiteService(
    IWeddingWebsiteRepository websiteRepository,
    IWeddingRepository weddingRepository,
    IEventRepository eventRepository,
    IInvitationFlowRepository flowRepository,
    ApplicationMapper mapper) : IWeddingWebsiteService
{
    public async Task<Result<WeddingWebsiteDto>> GetByWeddingIdAsync(Guid weddingId)
    {
        var website = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (website == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found for this wedding"));
        }

        return Result<WeddingWebsiteDto>.Ok(mapper.WebsiteToDto(website));
    }

    public async Task<Result<WeddingWebsiteDto>> CreateAsync(Guid userId, Guid weddingId, CreateWeddingWebsiteRequestDto request)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));
        }

        // Check if wedding has a valid date set
        if (wedding.Date == default || wedding.Date == DateTime.MinValue)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.Validation,
                "Please set a wedding date before creating your website"));
        }

        // Check subscription tier - only Starter and Pro can create websites
        var user = wedding.User;
        if (user.SubscriptionTier == SubscriptionTier.Free)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.Forbidden,
                "Website builder is only available for Starter and Pro subscriptions"));
        }

        var existing = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (existing != null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.Conflict, "Website already exists for this wedding"));
        }

        var website = new WeddingWebsite
        {
            Id = Guid.NewGuid(),
            WeddingId = weddingId,
            Template = request.Template,
            Content = GetDefaultContent(wedding),
            Settings = GetDefaultSettings(request.Template),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await websiteRepository.AddAsync(website);

        // Reload to get the wedding relationship for mapping
        var createdWebsite = await websiteRepository.GetByWeddingIdAsync(weddingId);
        return Result<WeddingWebsiteDto>.Ok(mapper.WebsiteToDto(createdWebsite!));
    }

    public async Task<Result<WeddingWebsiteDto>> UpdateAsync(Guid weddingId, UpdateWeddingWebsiteRequestDto request)
    {
        var website = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (website == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found"));
        }

        if (request.Template.HasValue)
        {
            website.Template = request.Template.Value;
        }

        if (request.Settings != null)
        {
            website.Settings = request.Settings.RootElement.GetRawText();
        }

        if (request.Content != null)
        {
            website.Content = request.Content.RootElement.GetRawText();
        }

        if (request.MetaDescription != null)
        {
            website.MetaDescription = request.MetaDescription;
        }

        await websiteRepository.UpdateAsync(website);

        var updatedWebsite = await websiteRepository.GetByWeddingIdAsync(weddingId);
        return Result<WeddingWebsiteDto>.Ok(mapper.WebsiteToDto(updatedWebsite!));
    }

    public async Task<Result<WeddingWebsiteDto>> PublishAsync(Guid weddingId)
    {
        var website = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (website == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found"));
        }

        website.IsPublished = true;
        website.PublishedAt = DateTime.UtcNow;

        await websiteRepository.UpdateAsync(website);

        var updatedWebsite = await websiteRepository.GetByWeddingIdAsync(weddingId);
        return Result<WeddingWebsiteDto>.Ok(mapper.WebsiteToDto(updatedWebsite!));
    }

    public async Task<Result<WeddingWebsiteDto>> UnpublishAsync(Guid weddingId)
    {
        var website = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (website == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found"));
        }

        website.IsPublished = false;

        await websiteRepository.UpdateAsync(website);

        var updatedWebsite = await websiteRepository.GetByWeddingIdAsync(weddingId);
        return Result<WeddingWebsiteDto>.Ok(mapper.WebsiteToDto(updatedWebsite!));
    }

    public async Task<Result<PublicWebsiteStateDto>> GetPublicBySlugAsync(string slug, Guid? unlockedFlowId)
    {
        var website = await websiteRepository.GetPublishedBySlugAsync(slug);
        if (website == null)
        {
            return Result<PublicWebsiteStateDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found or not published"));
        }

        // Resolve which flow (if any) personalises this visit, and whether a passcode is still required.
        var flows = (await flowRepository.GetByWeddingIdAsync(website.WeddingId)).ToList();
        InvitationFlow? activeFlow = null;
        if (flows.Count > 0)
        {
            var openFlow = flows.FirstOrDefault(f => f.Passcode == null);
            if (openFlow != null)
            {
                // Open flow: no code needed, but events still follow the flow.
                activeFlow = openFlow;
            }
            else
            {
                // Passcode-protected flows: require a valid unlocked flow belonging to this wedding.
                activeFlow = unlockedFlowId == null
                    ? null
                    : flows.FirstOrDefault(f => f.Id == unlockedFlowId.Value);

                if (activeFlow == null)
                {
                    return Result<PublicWebsiteStateDto>.Ok(new PublicWebsiteStateDto { RequiresPasscode = true });
                }
            }
        }

        var dto = await BuildPublicWebsiteDtoAsync(website, activeFlow);
        return Result<PublicWebsiteStateDto>.Ok(new PublicWebsiteStateDto { RequiresPasscode = false, Website = dto });
    }

    private async Task<PublicWeddingWebsiteDto> BuildPublicWebsiteDtoAsync(WeddingWebsite website, InvitationFlow? activeFlow)
    {
        var dto = new PublicWeddingWebsiteDto
        {
            WeddingSlug = website.Wedding.Slug,
            CoupleNames = website.Wedding.Title,
            WeddingDate = website.Wedding.Date,
            WeddingLocation = website.Wedding.Location,
            Template = website.Template,
            Settings = JsonDocument.Parse(website.Settings),
            Content = JsonDocument.Parse(website.Content)
        };

        // Include events if the events section is enabled, filtered to the active flow when present.
        var content = JsonDocument.Parse(website.Content);
        if (content.RootElement.TryGetProperty("events", out var eventsSection) &&
            eventsSection.TryGetProperty("enabled", out var enabled) &&
            enabled.GetBoolean() &&
            eventsSection.TryGetProperty("showFromWeddingEvents", out var showEvents) &&
            showEvents.GetBoolean())
        {
            IEnumerable<Event> events = await eventRepository.GetByWeddingIdForPublicAsync(website.WeddingId);
            if (activeFlow != null)
            {
                events = events.Where(e => activeFlow.EventIds.Contains(e.Id));
            }
            // Public, anonymous endpoint: never expose the guest list. EventToDto maps guests, so
            // strip them here — the website only needs name/date/location/description of an event.
            dto.Events = events.Select(e =>
            {
                var eventDto = mapper.EventToDto(e);
                eventDto.GuestDtos = new List<GuestDto>();
                return eventDto;
            }).ToList();
        }

        return dto;
    }

    public async Task<Result> DeleteAsync(Guid weddingId)
    {
        var website = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (website == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "Website not found"));
        }

        await websiteRepository.DeleteAsync(website.Id);
        return Result.Ok();
    }

    private static string GetDefaultContent(Wedding wedding)
    {
        var content = new
        {
            hero = new
            {
                coupleNames = wedding.Title,
                date = wedding.Date.ToString("yyyy-MM-dd"),
                tagline = "We're getting married!",
                backgroundImageId = (string?)null,
                backgroundImageUrl = (string?)null,
                displayStyle = "centered"
            },
            story = new
            {
                enabled = true,
                title = "Our Story",
                displayType = "timeline",
                items = Array.Empty<object>()
            },
            details = new
            {
                enabled = true,
                title = "Wedding Details",
                ceremony = new
                {
                    enabled = true,
                    title = "Ceremony",
                    venue = "",
                    address = wedding.Location,
                    date = wedding.Date.ToString("o"),
                    description = "",
                    mapUrl = ""
                },
                reception = new
                {
                    enabled = true,
                    title = "Reception",
                    venue = "",
                    address = "",
                    date = wedding.Date.ToString("o"),
                    description = "",
                    mapUrl = ""
                }
            },
            events = new
            {
                enabled = true,
                title = "Schedule",
                showFromWeddingEvents = true
            },
            gallery = new
            {
                enabled = true,
                title = "Gallery",
                displayType = "grid",
                images = Array.Empty<object>()
            },
            rsvp = new
            {
                enabled = true,
                title = "RSVP",
                description = "Please let us know if you can attend",
                deadline = wedding.Date.AddMonths(-1).ToString("yyyy-MM-dd")
            },
            footer = new
            {
                enabled = true,
                contactEmail = "",
                customMessage = "We can't wait to celebrate with you!"
            }
        };

        return JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = false });
    }

    private static string GetDefaultSettings(WebsiteTemplate template)
    {
        var templateSettings = template switch
        {
            WebsiteTemplate.ElegantClassic => new Dictionary<string, string>
            {
                ["primaryColor"] = "#8B7355",
                ["accentColor"] = "#D4AF37",
                ["fontFamily"] = "serif",
                ["backgroundPattern"] = "damask"
            },
            WebsiteTemplate.ModernMinimal => new Dictionary<string, string>
            {
                ["primaryColor"] = "#1A1A1A",
                ["accentColor"] = "#E0E0E0",
                ["fontFamily"] = "sans-serif",
                ["layoutDensity"] = "spacious"
            },
            WebsiteTemplate.RomanticGarden => new Dictionary<string, string>
            {
                ["primaryColor"] = "#8B4513",
                ["accentColor"] = "#FFB6C1",
                ["fontFamily"] = "script",
                ["floralStyle"] = "watercolor"
            },
            WebsiteTemplate.MinimalArchitecture => new Dictionary<string, string>
            {
                ["primaryColor"] = "#1A1A1A",
                ["accentColor"] = "#8E9794",
                ["fontFamily"] = "serif"
            },
            _ => new Dictionary<string, string>
            {
                ["primaryColor"] = "#8B7355",
                ["accentColor"] = "#D4AF37",
                ["fontFamily"] = "serif",
                ["backgroundPattern"] = "damask"
            }
        };

        var settings = new { templateSettings };
        return JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = false });
    }
}
