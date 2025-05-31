using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftEng2025.Data;
using SoftEng2025.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SoftEng2025.Pages.Entrepreneur
{
    [Authorize(Roles = "Entrepreneur")]
    public class DeleteNotificationModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DeleteNotificationModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }  // this is the NotificationId

        public async Task<IActionResult> OnPostAsync()
        {
            // 1) Confirm the current user is the owner of this EntrepreneurNotification
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var noti = await _db.EntrepreneurNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == Id
                                          && n.UserId == userId);
            if (noti == null)
            {
                // either it doesn’t exist, or it belongs to someone else
                return Forbid();
            }

            // 2) Delete and save
            _db.EntrepreneurNotifications.Remove(noti);
            await _db.SaveChangesAsync();

            // 3) Redirect back to wherever the user was; simplest: reload the current page
            // We could read Referer header, but you can also do:
            if (Request.Headers.ContainsKey("Referer"))
            {
                var referer = Request.Headers["Referer"].ToString();
                return Redirect(referer);
            }

            return RedirectToPage("/Index"); // fallback
        }
    }
}