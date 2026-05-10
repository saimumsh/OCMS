using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OptimumCoaching.core;
using OptimumCoaching.service;
using OptimumCoaching.web.Models;
using OptimumCoaching.web.Services;

namespace OptimumCoaching.web.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly IMessagingService _messaging;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUser;

        public MessagesController(
            IMessagingService messaging,
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUser)
        {
            _messaging = messaging;
            _userManager = userManager;
            _currentUser = currentUser;
        }

        // GET /Messages — inbox
        public async Task<IActionResult> Index()
        {
            var (uid, roles) = await ResolveAsync();
            if (uid == Guid.Empty) return Challenge();

            var inbox = await _messaging.GetInboxAsync(uid, roles);
            ViewBag.UnreadTotal = inbox.Count(i => i.IsUnread);
            return View(inbox);
        }

        // GET /Messages/Compose — new conversation form
        public IActionResult Compose() => View(new ComposeMessageViewModel());

        // POST /Messages/Compose
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(ComposeMessageViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var uid = _currentUser.UserId;
            if (uid == Guid.Empty) return Challenge();

            var (success, message, conv) = await _messaging.CreateAsync(
                uid, model.TargetRole, model.Subject, model.Body);

            if (!success || conv == null)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Open), new { id = conv.Id });
        }

        // GET /Messages/Open/{id} — view thread
        public async Task<IActionResult> Open(Guid id)
        {
            var (uid, roles) = await ResolveAsync();
            if (uid == Guid.Empty) return Challenge();

            var conv = await _messaging.GetByIdForUserAsync(id, uid, roles);
            if (conv == null) return NotFound();

            // Stamp this user as having read up to "now" so subsequent visits
            // recognise existing messages as seen.
            await _messaging.MarkReadAsync(id, uid);

            ViewBag.CurrentUserId = uid;
            ViewBag.IsOriginator = conv.StartedByUserId == uid;
            return View(conv);
        }

        // POST /Messages/Reply
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(ReplyMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Reply cannot be empty.";
                return RedirectToAction(nameof(Open), new { id = model.ConversationId });
            }

            var (uid, roles) = await ResolveAsync();
            if (uid == Guid.Empty) return Challenge();

            var (success, message, _) = await _messaging.ReplyAsync(
                model.ConversationId, uid, roles, model.Body);

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
            return RedirectToAction(nameof(Open), new { id = model.ConversationId });
        }

        private async Task<(Guid UserId, IList<string> Roles)> ResolveAsync()
        {
            var uid = _currentUser.UserId;
            if (uid == Guid.Empty) return (Guid.Empty, new List<string>());

            var user = await _userManager.FindByIdAsync(uid.ToString());
            if (user == null) return (Guid.Empty, new List<string>());

            var roles = await _userManager.GetRolesAsync(user);
            return (uid, roles);
        }
    }
}
