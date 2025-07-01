using fitPass.Models;
using fitPass.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace fitPass.Controllers
{
    public class AdminController : Controller
    {
        private readonly GymManagementContext _context;

        public AdminController(GymManagementContext context)
        {
            _context = context;
        }

        //公告類別下拉選單
        private List<SelectListItem> GetNewsCategoryList()
        {
            return new List<SelectListItem>
    {
        new SelectListItem { Text = "系統公告", Value = "系統公告" },
        new SelectListItem { Text = "活動公告", Value = "活動公告" },
        new SelectListItem { Text = "緊急公告", Value = "緊急公告" },
        new SelectListItem { Text = "課程公告", Value = "課程公告" },
        new SelectListItem { Text = "會員相關公告", Value = "會員相關公告" }
    };
        }


        //後台首頁
        public async Task<IActionResult> Dashbord()
        {
            var today = DateTime.Today;
            ViewData["TodayNews"] = await _context.News
                .CountAsync(n => n.Showtime.HasValue && n.Showtime.Value.Date == today);
            ViewData["InsideNowCount"] = await _context.CheckInRecords.CountAsync(c => c.CheckType==1);
            return View();
        }

        //公告管理首頁
        [HttpGet]
        public async Task<IActionResult> NewsList(string? keyword, string? category)
        {
            var query = _context.News.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(n => n.Title.Contains(keyword));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(n => n.Category==category);
            }

            ViewData["CategoryList"] = GetNewsCategoryList();
            ViewData["Keyword"] = keyword;
            ViewData["SelectedCategory"] = category;
            var newsList = await query.OrderByDescending(n => n.PublishTime).ToListAsync();
            return View(newsList);
        }

        //公告單筆詳細
        [HttpGet]
        public async Task<IActionResult> NewsDetail(int id)
        {
            var news = await _context.News.FindAsync(id);
            if(news == null)
            {
                return RedirectToAction("NewsList");
            }
            return View(news);
        }
        //新增公告
        [HttpGet]
        public IActionResult CreateNews()
        {
            ViewData["CategoryList"] = GetNewsCategoryList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNews(News model, IFormFile? BannerFile, IFormFile? InsideimgFile)
        {
            if (ModelState.IsValid)
            {
                model.PublishTime = DateTime.Now;
                // 處理 Banner 圖片
                if (BannerFile != null && BannerFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await BannerFile.CopyToAsync(ms);
                    model.Banner = ms.ToArray();
                }

                // 處理 Insideimg 圖片
                if (InsideimgFile != null && InsideimgFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await InsideimgFile.CopyToAsync(ms);
                    model.Insideimg = ms.ToArray();
                }
                _context.News.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("NewsList");
            }
            ViewData["CategoryList"] = GetNewsCategoryList();
            return View(model);
        }

        //編輯公告
        [HttpGet]
        public async Task<IActionResult> EditNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            ViewData["CategoryList"] = GetNewsCategoryList();
            return View(news);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNews(News model, IFormFile? BannerFile, IFormFile? InsideimgFile)
        {
            if (ModelState.IsValid)
            {
                var existingNews = await _context.News.FindAsync(model.NewsId);
                if (existingNews == null) return NotFound();

                // 更新欄位（保留 PublishTime）
                existingNews.Title = model.Title;
                existingNews.Category = model.Category;
                existingNews.Level = model.Level;
                existingNews.Showtime = model.Showtime;
                existingNews.IsVisible = model.IsVisible;
                existingNews.Content = model.Content;

                if (BannerFile != null && BannerFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await BannerFile.CopyToAsync(ms);
                    existingNews.Banner = ms.ToArray();
                }

                if (InsideimgFile != null && InsideimgFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await InsideimgFile.CopyToAsync(ms);
                    existingNews.Insideimg = ms.ToArray();
                }
                await _context.SaveChangesAsync();
                return RedirectToAction("NewsList");
            }

            ViewData["CategoryList"] = GetNewsCategoryList();
            return View(model);
        }

        //出入場紀錄總覽
        [HttpGet]
        public async Task<IActionResult> CheckInStatusList(string? keyword, string? range)
        {
            var membersQuery = _context.Accounts.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                membersQuery = membersQuery.Where(m => m.Name.Contains(keyword));
            }

            var members = await membersQuery.ToListAsync();
            var statusList = new List<CheckInRecordStatusViewModel>();
            var today = DateTime.Today;

            foreach (var member in members)
            {
                var recordQuery = _context.CheckInRecords
                    .Where(r => r.MemberId == member.MemberId);

                // 篩選時間範圍
                if (range == "today")
                {
                    recordQuery = recordQuery.Where(r =>
                        (r.CheckInTime.HasValue && r.CheckInTime.Value.Date == today) ||
                        (r.CheckOutTime.HasValue && r.CheckOutTime.Value.Date == today));
                }
                else if (range == "week")
                {
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // 星期一為起點
                    recordQuery = recordQuery.Where(r =>
                        (r.CheckInTime >= startOfWeek || r.CheckOutTime >= startOfWeek));
                }
                else if (range == "month")
                {
                    var startOfMonth = new DateTime(today.Year, today.Month, 1);
                    recordQuery = recordQuery.Where(r =>
                        (r.CheckInTime >= startOfMonth || r.CheckOutTime >= startOfMonth));
                }

                var latest = await recordQuery
                    .OrderByDescending(r => r.CheckOutTime ?? r.CheckInTime)
                    .FirstOrDefaultAsync();

                if (latest != null)
                {
                    statusList.Add(new CheckInRecordStatusViewModel
                    {
                        MemberId = member.MemberId,
                        MemberName = member.Name,
                        CheckInTime = latest.CheckInTime,
                        CheckOutTime = latest.CheckOutTime,
                        Status = latest.CheckOutTime == null ? 1 : 2
                    });
                }
            }

            ViewData["Keyword"] = keyword;
            ViewData["Range"] = range;

            return View(statusList);
        }

        //Inbody總覽
        [HttpGet]
        public async Task<IActionResult> InbodyOverview()
        {
            var members = await _context.Accounts
                .Where(a => a.Admin != 3) 
                .Include(a => a.Inbodies)
                .ToListAsync();

            var viewModel = members.Select(m => new InbodyMemberOverviewVM
            {
                MemberId = m.MemberId,
                Name = m.Name,
                Email = m.Email,
                HasInbody = m.Inbodies.Any()
            }).ToList();

            return View(viewModel);
        }

        //Inbody新增與編輯
        // GET: Admin/InbodyCreate/5
        public IActionResult InbodyCreate(int memberId)
        {
            var member = _context.Accounts.FirstOrDefault(a => a.MemberId == memberId);
            if (member == null)
            {
                return NotFound();
            }

            var model = new Inbody
            {
                MemberId = memberId,
                RecordDate = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewBag.MemberName = member.Name;
            return View("InbodyForm",model);
        }

        // POST: Admin/InbodyCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InbodyCreate(Inbody inbody)
        {
            if (ModelState.IsValid)
            {
                _context.Inbodies.Add(inbody);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(InbodyOverview));
            }

            var member = await _context.Accounts.FindAsync(inbody.MemberId);
            ViewBag.MemberName = member?.Name ?? "(未知)";
            return View("InbodyForm",inbody);
        }

        // GET
        public async Task<IActionResult> InbodyEdit(int memberId)
        {
            var inbody = await _context.Inbodies
                .Where(i => i.MemberId == memberId)
                .OrderByDescending(i => i.RecordDate)
                .FirstOrDefaultAsync();

            if (inbody == null)
            {
                return RedirectToAction(nameof(InbodyCreate), new { memberId });
            }

            ViewBag.MemberName = (await _context.Accounts.FindAsync(memberId))?.Name ?? "(未知)";
            return View("InbodyForm", inbody);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InbodyEdit(Inbody inbody)
        {
            if (ModelState.IsValid)
            {
                _context.Inbodies.Update(inbody);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(InbodyOverview));
            }

            ViewBag.MemberName = (await _context.Accounts.FindAsync(inbody.MemberId))?.Name ?? "(未知)";
            return View("InbodyForm", inbody);
        }

        //帳戶總覽
        public async Task<IActionResult> AccountOverview(string? keyword, int? gender, int? admin)
        {

            var query =  _context.Accounts
                                   .Where(r => r.Admin != 3);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(a => a.Name.Contains(keyword)||a.Email.Contains(keyword));
            }

            if (gender.HasValue)
            {
                query = query.Where(a=>a.Gender==gender);
            }

            if (admin.HasValue)
            {
                query = query.Where(a => a.Admin == admin);
            }

            var result = await query.ToListAsync();

            ViewData["Keyword"] = keyword;
            ViewData["Gender"] = gender;
            ViewData["Admin"] = admin;
            return View(result);
        }
        //單筆帳戶詳細資料
        [HttpGet]
        public async Task<IActionResult>AccountDetail(int id)
        {
            var account = await _context.Accounts.Include(a => a.SubscriptionLogs).FirstOrDefaultAsync( a => a.MemberId==id);
            if(account == null)
            {
                return NotFound();
            }

            var latestSubDate = account.SubscriptionLogs.OrderByDescending(s => s.SubscribedTime).FirstOrDefault();
            ViewData["LatestSubDate"] = latestSubDate?.EndDate;
            return View(account);
        }
        //單筆帳戶資料修改

    }
}
