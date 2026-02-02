namespace WhoOwesWho.Common.Groups.Dto;

public sealed record GroupDetailsResponseDto(
    Guid Id,
    string Title,
    Guid CreatorUserId,
    List<MemberDto> Members,
    List<PaymentDto> Payments);
