using BloggingPlatformApi.Data;
using BloggingPlatformApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloggingPlatformApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorModeratorUserPolicy")]
public class CommentsController(AppDbContext context, UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> EditComment(Guid commentId, [FromBody] CommentRequestModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        bool isModerator = User.IsInRole("Moderator");

        var comment = await context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null) return NotFound();

        if (!isModerator && comment.UserId != user.Id)
        {
            return Forbid(); 
        }

        comment.Content = model.Content;
        await context.SaveChangesAsync();

        return Ok(comment);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        bool isModerator = User.IsInRole("Moderator");
        var comment = await context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment is null) return NotFound();

        if (!isModerator && comment.UserId != user.Id)
        {
            return Forbid();
        }

        context.Comments.Remove(comment);
        await context.SaveChangesAsync();

        return NoContent();
    }

}