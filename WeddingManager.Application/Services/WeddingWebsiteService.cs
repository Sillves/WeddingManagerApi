using System.Text.Json;
using AutoMapper;
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
    IMapper mapper) : IWeddingWebsiteService
{
    public async Task<Result<WeddingWebsiteDto>> GetByWeddingIdAsync(Guid weddingId)
    {
        var website = await websiteRepository.GetByWeddingIdAsync(weddingId);
        if (website == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found for this wedding"));
        }

        return Result<WeddingWebsiteDto>.Ok(mapper.Map<WeddingWebsiteDto>(website));
    }

    public async Task<Result<WeddingWebsiteDto>> CreateAsync(Guid userId, Guid weddingId, CreateWeddingWebsiteRequestDto request)
    {
        var wedding = await weddingRepository.GetByIdAsync(weddingId);
        if (wedding == null)
        {
            return Result<WeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Wedding not found"));
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
        return Result<WeddingWebsiteDto>.Ok(mapper.Map<WeddingWebsiteDto>(createdWebsite!));
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
        return Result<WeddingWebsiteDto>.Ok(mapper.Map<WeddingWebsiteDto>(updatedWebsite!));
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
        return Result<WeddingWebsiteDto>.Ok(mapper.Map<WeddingWebsiteDto>(updatedWebsite!));
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
        return Result<WeddingWebsiteDto>.Ok(mapper.Map<WeddingWebsiteDto>(updatedWebsite!));
    }

    public async Task<Result<PublicWeddingWebsiteDto>> GetPublicBySlugAsync(string slug)
    {
        var website = await websiteRepository.GetPublishedBySlugAsync(slug);
        if (website == null)
        {
            return Result<PublicWeddingWebsiteDto>.Fail(new Error(ErrorCodes.NotFound, "Website not found or not published"));
        }

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

        // Include events if the events section is enabled
        var content = JsonDocument.Parse(website.Content);
        if (content.RootElement.TryGetProperty("events", out var eventsSection) &&
            eventsSection.TryGetProperty("enabled", out var enabled) &&
            enabled.GetBoolean() &&
            eventsSection.TryGetProperty("showFromWeddingEvents", out var showEvents) &&
            showEvents.GetBoolean())
        {
            var events = await eventRepository.GetByWeddingIdAsync(website.WeddingId);
            dto.Events = events.Select(e => mapper.Map<EventDto>(e)).ToList();
        }

        return Result<PublicWeddingWebsiteDto>.Ok(dto);
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
