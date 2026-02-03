using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhoOwesWho.Api.Groups.Services;
using WhoOwesWho.Common.Groups.Dto;

namespace WhoOwesWho.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class GroupsController(IGroupService groupService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateGroupResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateGroupResponseDto>> CreateGroup([FromBody] CreateGroupRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var result = await groupService.CreateGroupAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(GetGroupDetails), new { groupId = result.GroupId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<GroupSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<GroupSummaryDto>>> GetAllGroups(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var result = await groupService.GetAllGroupsAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{groupId:guid}")]
    [ProducesResponseType(typeof(GroupDetailsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupDetailsResponseDto>> GetGroupDetails(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var result = await groupService.GetGroupDetailsAsync(userId, groupId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpDelete("{groupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            await groupService.DeleteGroupAsync(userId, groupId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{groupId:guid}/members")]
    [ProducesResponseType(typeof(AddMemberResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddMemberResponseDto>> AddMember(Guid groupId, [FromBody] AddMemberRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var result = await groupService.AddMemberAsync(userId, groupId, request, cancellationToken);
            return CreatedAtAction(nameof(GetGroupDetails), new { groupId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{groupId:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid memberId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            await groupService.RemoveMemberAsync(userId, groupId, memberId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{groupId:guid}/payments")]
    [ProducesResponseType(typeof(AddPaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddPaymentResponseDto>> AddPayment(Guid groupId, [FromBody] AddPaymentRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var result = await groupService.AddPaymentAsync(userId, groupId, request, cancellationToken);
            return CreatedAtAction(nameof(GetGroupDetails), new { groupId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpDelete("{groupId:guid}/payments/{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePayment(Guid groupId, Guid paymentId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            await groupService.RemovePaymentAsync(userId, groupId, paymentId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    [HttpGet("{groupId:guid}/debts")]
    [ProducesResponseType(typeof(List<DebtResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DebtResponseDto>>> GetGroupDebts(Guid groupId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var result = await groupService.GetGroupDebtsAsync(userId, groupId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid User ID format.");
        }

        return userId;
    }
}
