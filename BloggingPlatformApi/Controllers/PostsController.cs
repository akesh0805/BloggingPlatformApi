using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloggingPlatformApi.Data;
using BloggingPlatformApi.Models;
using BloggingPlatformApi.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
namespace BloggingPlatformApi.Controllers;

[Route("api/posts")]
[ApiController]
public class PostsController(
        AppDbContext context,
        UserManager<IdentityUser> userManager,
        IHubContext<NotificationHub> hubContext,
        IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
    public async Task<IActionResult> CreatePost([FromBody] PostRequestModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var post = new Post
        {
            Title = model.Title,
            Content = model.Content,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            CategoryId = model.CategoryId,
            PostTags = [.. model.TagIds.Select(tagId => new PostTag { TagId = tagId })]
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("ReceiveNotification", $"{user.UserName} added a new post: {post.Title}");
        return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var user = await userManager.GetUserAsync(User);
        var isAdmin = await userManager.IsInRoleAsync(user!, "Admin");

        var post = await context.Posts
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        if (!isAdmin && post.UserId != user!.Id) return Forbid();

        return Ok(post);
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
    public async Task<IActionResult> GetPosts([FromQuery] string? status, [FromQuery] Guid? categoryId, [FromQuery] Guid? tagId, [FromQuery] string? search)
    {
        var user = await userManager.GetUserAsync(User);
        var isAdmin = await userManager.IsInRoleAsync(user!, "Admin");

        var query = context.Posts
            .Include(p => p.PostTags)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(p => p.UserId == user!.Id);
        }

        if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.Status == status);
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (tagId.HasValue) query = query.Where(p => p.PostTags.Any(pt => pt.TagId == tagId));
        if (!string.IsNullOrEmpty(search)) query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));

        var posts = await query.ToListAsync();
        return Ok(posts);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] PostRequestModel model)
    {
        var post = await context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var isAdmin = await userManager.IsInRoleAsync(user!, "Admin");

        if (post.UserId != user!.Id && !isAdmin) return Forbid();

        post.Title = model.Title;
        post.Content = model.Content;
        post.Status = model.Status;
        post.PublishedAt = model.PublishedAt;
        post.CategoryId = model.CategoryId;

        context.PostTags.RemoveRange(post.PostTags);
        post.PostTags = [.. model.TagIds.Select(tagId => new PostTag { PostId = id, TagId = tagId })];

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var post = await context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        var user = await userManager.GetUserAsync(User);
        var isAdmin = await userManager.IsInRoleAsync(user!, "Admin");

        if (post.UserId != user!.Id && !isAdmin) return Forbid();

        context.Posts.Remove(post);
        await context.SaveChangesAsync();
        return NoContent();
    }


    [HttpPost("{postId}/like")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorModeratorUserPolicy")]
    public async Task<IActionResult> LikePost(Guid postId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var post = await context.Posts.FindAsync(postId);
        if (post == null) return NotFound();

        var existingLike = await context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == user.Id);
        if (existingLike != null) return BadRequest("Already liked this post.");

        var like = new Like { PostId = postId, UserId = user.Id };
        context.Likes.Add(like);
        await context.SaveChangesAsync();

        await hubContext.Clients.All.SendAsync("ReceiveNotification", $"liked {post.Title} name post.");

        return Ok();
    }

    [HttpDelete("{postId}/like")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorModeratorUserPolicy")]
    public async Task<IActionResult> UnlikePost(Guid postId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var like = await context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == user.Id);
        if (like == null) return BadRequest("You haven't liked this post.");

        context.Likes.Remove(like);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{postId}/comments")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorModeratorUserPolicy")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] CommentRequestModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var post = await context.Posts.FindAsync(postId);
        if (post == null) return NotFound();

        var comment = new Comment
        {
            Content = model.Content,
            CreatedAt = DateTime.UtcNow,
            PostId = postId,
            UserId = user.Id
        };

        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        await hubContext.Clients.All.SendAsync("ReceiveNotification", $"Comment added to {post.Title}. Commend: {model.Content}");

        return CreatedAtAction(nameof(GetPostById), new { id = postId }, comment);
    }

    [HttpPost("{postId}/media")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorPolicy")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMedia(Guid postId, IFormFile file, [FromForm] string fileType)
    {
        var post = await context.Posts.FindAsync(postId);
        if (post == null)
            return NotFound(new { message = "Post not found" });

        var userId = User.FindFirst("sub")?.Value;
        var isAdmin = User.IsInRole("admin");

        if (post.UserId != userId && !isAdmin)
            return Forbid();

        var allowedFileTypes = new[] { "image", "video" };
        if (!allowedFileTypes.Contains(fileType.ToLower()))
            return BadRequest(new { message = "Invalid file type" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds 10MB limit" });

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(env.WebRootPath, "uploads", fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var media = new MediaAttachment
        {
            PostId = postId,
            FileUrl = fileName,
            FileType = fileType
        };
        context.MediaAttachments.Add(media);
        await context.SaveChangesAsync();

        return Created($"/uploads/{fileName}", new { message = "File uploaded successfully", url = $"/uploads/{fileName}" });
    }

    [HttpGet("/api/notifications")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorModeratorUserPolicy")]
    public async Task<IActionResult> GetNotifications()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notifications = await context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPut("/api/notifications/{id}/read")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminAuthorModeratorUserPolicy")]
    public async Task<IActionResult> MarkNotificationAsRead(Guid id)
    {
        var notification = await context.Notifications.FindAsync(id);
        if (notification == null) return NotFound();

        notification.IsRead = true;
        context.Notifications.Update(notification);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
