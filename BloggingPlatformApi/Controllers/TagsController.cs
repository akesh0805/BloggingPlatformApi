using BloggingPlatformApi.Data;
using BloggingPlatformApi.Entities;
using BloggingPlatformApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloggingPlatformApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
public class TagsController(AppDbContext context, UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] TagRequestModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var tag = new Tag
        {
            Name = model.Name
        };

        context.Tags.Add(tag);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTagById), new { id = tag.Id }, tag);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTagById(Guid id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");

        var tagQuery = context.Tags
            .Where(t => t.Id == id)
            .Include(t => t.PostTags)
                .ThenInclude(pt => pt.Post)
            .AsQueryable();

        if (!isAdmin)
        {
            tagQuery = tagQuery.Where(t => t.PostTags.Any(pt => pt.Post.UserId == user.Id));
        }

        var tag = await tagQuery.FirstOrDefaultAsync();

        if (tag is null) return NotFound();

        return Ok(new
        {
            tag.Id,
            tag.Name,
            Posts = tag.PostTags
                .Where(pt => isAdmin || pt.Post.UserId == user.Id)
                .Select(pt => new
                {
                    pt.Post.Id,
                    pt.Post.Title,
                    pt.Post.Content,
                    pt.Post.CreatedAt,
                    pt.Post.Status
                })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetTags()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");

        var tags = await context.Tags
            .Include(t => t.PostTags)
                .ThenInclude(pt => pt.Post)
            .ToListAsync();

        return Ok(tags.Select(tag => new
        {
            tag.Id,
            tag.Name,
            Posts = tag.PostTags
                .Where(pt => isAdmin || pt.Post.UserId == user.Id)
                .Select(pt => new
                {
                    pt.Post.Id,
                    pt.Post.Title,
                    pt.Post.Content,
                    pt.Post.CreatedAt,
                    pt.Post.Status
                })
        }));
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(Guid id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        bool isAdmin = User.IsInRole("Admin");

        var tag = await context.Tags
            .Include(t => t.PostTags)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tag == null) return NotFound();

        if (!isAdmin)
        {
            bool hasAccess = tag.PostTags.Any(pt => pt.Post.UserId == user.Id);
            if (!hasAccess)
            {
                return Forbid(); 
            }
        }

        context.Tags.Remove(tag);
        await context.SaveChangesAsync();

        return NoContent();
    }


}