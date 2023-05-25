using System;
using AutoMapper;
using UrlShortenerApp.Models.Entities;
using UrlShortenerApp.Utils.Extensions;

namespace UrlShortenerApp.Services.MapperProfiles;

public class UrlTableProfile : Profile
{
    public UrlTableProfile()
    {
        CreateMap<Url, UrlTableEntity>()
            .ForMember(urlTableEntity => urlTableEntity.RowKey, options => options.MapFrom(url => url.ShortcutCode))
            .ForMember(urlTableEntity => urlTableEntity.PartitionKey, options => options.MapFrom(url => new Uri(url.FullUrl).GetHost().GetFirstThreeLettersOfHost()))
            .ReverseMap();
    }
}