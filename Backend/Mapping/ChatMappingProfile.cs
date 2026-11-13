using Mapster;
using AstronautHealthMonitoring.DTOs.Chat;
using AstronautHealthMonitoring.Models.Entities;

namespace AstronautHealthMonitoring.Mapping;

public class ChatMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ChatMessage, ChatMessageDto>()
            .Map(dest => dest.AstronautFullName, src => src.Astronaut != null ? src.Astronaut.FullName : string.Empty);

        config.NewConfig<SendChatMessageRequest, ChatMessage>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.AstronautId)
            .Ignore(dest => dest.Astronaut)
            .Ignore(dest => dest.SenderUserId)
            .Ignore(dest => dest.SenderRole)
            .Ignore(dest => dest.SentAt)
            .Ignore(dest => dest.ReadAt);
    }
}
