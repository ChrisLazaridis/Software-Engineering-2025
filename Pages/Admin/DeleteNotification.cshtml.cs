using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SoftEng2025.Data;
using SoftEng2025.Models;
using Microsoft.EntityFrameworkCore;

namespace SoftEng2025.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DeleteNotificationModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DeleteNotificationModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }  // matches AdminNotification.NotificationId

        public async Task<IActionResult> OnPostAsync()
        {
            var noti = await _db.AdminNotifications
                .FirstOrDefaultAsync(n => n.NotificationId == Id);
            if (noti == null)
            {
                return NotFound();
            }

            _db.AdminNotifications.Remove(noti);
            await _db.SaveChangesAsync();

            if (Request.Headers.ContainsKey("Referer"))
            {
                var referer = Request.Headers["Referer"].ToString();
                return Redirect(referer);
            }

            return RedirectToPage("/Admin/Index");
        }
    }
}