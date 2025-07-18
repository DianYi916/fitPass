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

        //full calender json
        [HttpGet]
        public async Task<IActionResult> GetCoursesForCalendar()
        {
            var courses = await _context.CourseSchedules
                .Include(c => c.Coach)
                    .ThenInclude(co => co.Account)
                .ToListAsync();

            var events = new List<object>();

            foreach (var course in courses)
            {
                if (course.ClassStartDate == null || course.ClassEndDate == null ||
                    course.ClassTimeDayOfWeek == null || course.ClassTimeDaily == null)
                    continue;

                var startDate = course.ClassStartDate.Value;
                var endDate = course.ClassEndDate.Value;

                // 轉換成 C# DayOfWeek
                var dayOfWeek = (DayOfWeek)(course.ClassTimeDayOfWeek.Value % 7);

                // 起始時間（每 1 時段 = 1 小時）
                var startTime = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes((course.ClassTimeDaily.Value - 1) * 60));

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != dayOfWeek) continue;

                    // ✅ 手動補正 6 小時偏差（暫時修正 FullCalendar 解讀錯誤）
                    var startDateTime = date.ToDateTime(startTime).AddHours(6);
                    var endDateTime = startDateTime.AddHours(1);

                    events.Add(new
                    {
                        id = course.CourseId,
                        title = $"{course.Title}",
                        start = startDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = endDateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                        description = course.Description,
                        coachName = course.Coach?.Account?.Name ?? "未指定"
                    });
                }
            }

            return Json(events);
        }


        /*-------------------------------------------------------------------------------------*/

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
            var model = new News();
            ViewData["CategoryList"] = GetNewsCategoryList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNews(News model, IFormFile? BannerFile, IFormFile? InsideimgFile)
        {
            if (ModelState.IsValid)
            {
                if (BannerFile != null && BannerFile.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await BannerFile.CopyToAsync(ms);
                    model.Banner = ms.ToArray();
                }

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

        /*----------------------------------------------------------------------------------------------*/

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

        /*-------------------------------------------------------------------------------------------*/

        //Inbody總覽
        [HttpGet]
        public async Task<IActionResult> InbodyOverview(string? keyword, int page = 1)
        {
            int pageSize = 8;
            var query = _context.Accounts
                .Where(a => a.Admin != 3)
                .Include(a => a.Inbodies)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(a => a.Name.Contains(keyword) || a.Email.Contains(keyword));
            }

            int totalCount = await query.CountAsync();
            var members = await query
                .OrderBy(a => a.MemberId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = members.Select(m => new InbodyMemberOverviewVM
            {
                MemberId = m.MemberId,
                Name = m.Name,
                Email = m.Email,
                InbodyCount = m.Inbodies.Count,
                LatestRecordDate = m.Inbodies
                    .OrderByDescending(i => i.RecordDate)
                    .FirstOrDefault()?.RecordDate
            }).ToList();

            ViewData["Keyword"] = keyword;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalCount / pageSize);

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

        //inbody detail
        [HttpGet]
        public async Task<IActionResult> InbodyDetail(int memberId)
        {
            var member = await _context.Accounts
                .Include(m => m.Inbodies)
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null) return NotFound();

            var sortedData = member.Inbodies.OrderByDescending(i => i.RecordDate).ToList();

            ViewBag.MemberName = member.Name;
            ViewBag.MemberId = member.MemberId;

            return View(sortedData);
        }

        /*---------------------------------------------------------------------------------------------*/

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
        //單筆帳戶資料修改active admin 以及點數log與後台灌點數給帳戶
        [HttpGet]
        public async Task<IActionResult> AccountEdit(int id)
        {
            var account = await _context.Accounts.Include(a => a.PointLogs).FirstOrDefaultAsync(a => a.MemberId == id);
            if (account == null)
            {
                return NotFound();
            }
            return View(account);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccountEdit(int id, [Bind("MemberId,IsActive")] Account account, int AddPoint = 0)
        {
            if (id != account.MemberId)
            {
                return NotFound();
            }

            var existAccount = await _context.Accounts.FindAsync(id);
            if (existAccount == null)
            {
                return NotFound();
            }

            // ✅ 修改帳戶狀態
            existAccount.IsActive = account.IsActive;

            // ✅ 加點並記錄 PointLog
            if (AddPoint > 0)
            {
                int before = existAccount.Point;
                existAccount.Point += AddPoint;

                var log = new PointLog
                {
                    MemberId = existAccount.MemberId,
                    AlterationTime = DateTime.Now,
                    OriginalPoint = before,
                    FinallPoint = existAccount.Point,
                    Detail = $"後台加點 +{AddPoint} 點"
                };

                _context.PointLogs.Add(log);
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = AddPoint > 0
                    ? $"帳戶狀態已更新，並加 {AddPoint} 點成功"
                    : "已更改帳戶啟用狀態";
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "更新失敗，請稍後再試。");
            }

            return RedirectToAction(nameof(AccountEdit), new { id });
        }

        /*-------------------------------------------------------------------------------------------*/

        //管理教練資料(coaches表)
        [HttpGet]
        public async Task<IActionResult> CoachMange()
        {
            var coachAccount = await _context.Accounts.Where(a => a.Admin == 2)
                .Include(a => a.Coach)
                .ToListAsync();

            return View(coachAccount);
        }

        //create & edit coach
        // GET: Admin/AddCoach/{accountId}
        [HttpGet]
        public async Task<IActionResult> AddCoach(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null || account.Admin != 2)
            {
                return NotFound();
            }

            // 如果已存在 Coach，不允許新增
            var existing = await _context.Coaches.FirstOrDefaultAsync(c => c.AccountId == accountId);
            if (existing != null)
            {
                TempData["Error"] = "該帳戶已經是教練，請使用編輯功能。";
                return RedirectToAction(nameof(CoachMange));
            }

            var coach = new Coach { AccountId = accountId };
            return View("AddCoach", coach);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCoach(Coach coach, IFormFile? Photo)
        {
            Console.WriteLine($"[Debug] Coach.AccountId from POST: {coach.AccountId}");
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"[ModelState Error] Field: {state.Key}");
                        foreach (var error in state.Value.Errors)
                        {
                            Console.WriteLine($"    Error: {error.ErrorMessage}");
                        }
                    }
                }
                TempData["Error"] = "資料驗證失敗";
                return View("AddCoach", coach);
            }

            if (Photo != null && Photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await Photo.CopyToAsync(ms);
                coach.Photo = ms.ToArray();
            }

            _context.Coaches.Add(coach);
            await _context.SaveChangesAsync();

            var result = await _context.SaveChangesAsync();
            Console.WriteLine($"[Debug] SaveChanges affected rows: {result}");

            TempData["Success"] = "已成功新增教練";
            return RedirectToAction(nameof(CoachMange));
        }

        // GET: Admin/EditCoach/{coachId}
        [HttpGet]
        public async Task<IActionResult> EditCoach(int coachId)
        {
            var coach = await _context.Coaches
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CoachId == coachId);

            if (coach == null)
            {
                return NotFound();
            }

            return View("EditCoach", coach);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCoach(Coach coach, IFormFile? Photo)
        {
            var existing = await _context.Coaches.FindAsync(coach.CoachId);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Specialty = coach.Specialty;
            existing.Description = coach.Description;
            existing.CoachType = coach.CoachType;

            if (Photo != null && Photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await Photo.CopyToAsync(ms);
                existing.Photo = ms.ToArray();
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "已更新教練資料";
            return RedirectToAction(nameof(CoachMange));
        }

        //課程管理
        //團體課程
        //團課清單
        public async Task<IActionResult> ClassList(string? keyword, int? coachId, int? weekday, string? status)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var query = _context.CourseSchedules
                .Include(c => c.Coach)
                    .ThenInclude(coach => coach.Account)
                .Where(c => c.Coach.CoachType == 1)
                .AsQueryable();

            // 🔍 課程名稱模糊搜尋
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(c => c.Title.Contains(keyword));
            }

            // 🧑‍🏫 教練過濾
            if (coachId.HasValue)
            {
                query = query.Where(c => c.CoachId == coachId.Value);
            }

            // 🗓️ 星期幾過濾
            if (weekday.HasValue)
            {
                query = query.Where(c => c.ClassTimeDayOfWeek == weekday);
            }

            // ⏳ 課程狀態過濾
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = status switch
                {
                    "upcoming" => query.Where(c => c.ClassStartDate > today),
                    "ongoing" => query.Where(c => c.ClassStartDate <= today && c.ClassEndDate >= today),
                    "expired" => query.Where(c => c.ClassEndDate < today),
                    _ => query
                };
            }

            // 📊 撈出所有課程
            var courseList = await query.ToListAsync();

            // 📌 加入報名人數統計
            var result = courseList.Select(course => new CourseWithCountViewModel
            {
                Course = course,
                ReservationCount = _context.Reservations.Count(r => r.CourseId == course.CourseId && r.Status == 1)
            }).ToList();

            // 傳回查詢條件供 View 使用
            ViewData["Keyword"] = keyword;
            ViewData["CoachId"] = coachId;
            ViewData["Weekday"] = weekday;
            ViewData["Status"] = status;

            // 建立教練下拉選單
            ViewBag.CoachList = await _context.Coaches
                .Include(c => c.Account)
                .Where(c => c.CoachType == 1)
                .Select(c => new SelectListItem
                {
                    Text = c.Account.Name,
                    Value = c.CoachId.ToString()
                }).ToListAsync();

            return View(result);
        }


        //單筆課程詳細
        public async Task<IActionResult> ClassDetail(int id)
        {
            var course = await _context.CourseSchedules
                .Include(c => c.Coach)
                    .ThenInclude(coach => coach.Account)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
                return NotFound();

            var reservationCount = await _context.Reservations
                .CountAsync(r => r.CourseId == course.CourseId && r.Status == 1);

            var registeredMembers = await _context.Reservations
                .Where(r => r.CourseId == course.CourseId && r.Status == 1)
                .Include(r => r.Member)
                .Select(r => r.Member)
                .ToListAsync();

            var viewModel = new CourseWithCountViewModel
            {
                Course = course,
                ReservationCount = reservationCount,
                RegisteredMembers = registeredMembers
            };

            return View(viewModel);
        }


        //新增團體課程
        [HttpGet]
        public IActionResult CreateClass()
        {
            ViewData["CoachList"] = new SelectList(
                _context.Coaches
                    .Include(c => c.Account)
                    .Where(c => c.CoachType == 1),
                "CoachId", "Account.Name");

            ViewData["LocationList"] = new List<SelectListItem>
    {
        new SelectListItem { Text = "教室 A", Value = "教室A" },
        new SelectListItem { Text = "教室 B", Value = "教室B" }
    };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(CourseSchedule course, IFormFile? courseImage)
        {
            if (!ModelState.IsValid)
            {
                foreach (var kvp in ModelState)
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        Console.WriteLine($"欄位: {kvp.Key} 錯誤: {error.ErrorMessage}");
                    }
                }
            }
            try
            {
                if (courseImage != null && courseImage.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await courseImage.CopyToAsync(ms);
                        course.CourseImage = ms.ToArray();
                    }
                }

                if (course.Price > 9999)
                {
                    ModelState.AddModelError("Price", "價格不能超過 9999 元");
                }

                if (ModelState.IsValid)
                {
                    course.IsCanceled = false;
                    _context.CourseSchedules.Add(course);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ClassList");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ 寫入失敗：" + ex.Message);
                ModelState.AddModelError("", "資料儲存失敗：" + ex.Message);
            }


            ViewData["CoachList"] = new SelectList(
                _context.Coaches.Include(c => c.Account).Where(c => c.CoachType == 1),
                "CoachId", "Account.Name", course.CoachId);

            ViewData["LocationList"] = new List<SelectListItem>
            {
                 new SelectListItem { Text = "教室 A", Value = "教室A" },
                new SelectListItem { Text = "教室 B", Value = "教室B" }
            };

            return View(course);
        }

        //編輯團體課程
        [HttpGet]
        public async Task<IActionResult> EditClass(int id)
        {
            var course = await _context.CourseSchedules.FindAsync(id);
            if (course == null) return NotFound();

            ViewData["CoachList"] = new SelectList(
                _context.Coaches.Include(c => c.Account).Where(c => c.CoachType == 1),
                "CoachId", "Account.Name", course.CoachId);

            ViewData["LocationList"] = new List<SelectListItem>
            {
                new SelectListItem { Text = "教室 A", Value = "教室A" },
                new SelectListItem { Text = "教室 B", Value = "教室B" }
            };

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClass(CourseSchedule course, IFormFile? newImage)
        {
            if (newImage != null && newImage.Length > 0)
            {
                using var ms = new MemoryStream();
                await newImage.CopyToAsync(ms);
                course.CourseImage = ms.ToArray();
            }
            else
            {
                
                var old = await _context.CourseSchedules.AsNoTracking()
                    .Where(c => c.CourseId == course.CourseId)
                    .Select(c => c.CourseImage)
                    .FirstOrDefaultAsync();

                course.CourseImage = old;
            }

            if (course.Price > 9999)
            {
                ModelState.AddModelError("Price", "價格不得超過 9999");
            }

            if (ModelState.IsValid)
            {
                _context.CourseSchedules.Update(course);
                await _context.SaveChangesAsync();
                return RedirectToAction("ClassList");
            }

            
            ViewData["CoachList"] = new SelectList(
                _context.Coaches.Include(c => c.Account).Where(c => c.CoachType == 1),
                "CoachId", "Account.Name", course.CoachId);

            ViewData["LocationList"] = new List<SelectListItem>
            {
                new SelectListItem { Text = "教室 A", Value = "教室A" },
                new SelectListItem { Text = "教室 B", Value = "教室B" }
            };

            return View(course);
        }

        //私教課程總覽
        public async Task<IActionResult> PrivateSessionOverview()
        {
            var monthStart = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var members = await _context.Accounts
                .Where(a => a.Admin == 1)
                .ToDictionaryAsync(a => a.MemberId, a => a.Name);

            var coaches = await _context.Coaches
                .Include(c => c.Account)
                .Include(c => c.CoachTimes)
                    .ThenInclude(ct => ct.PrivateSessions)
                .Where(c => c.CoachType == 2)
                .ToListAsync();

            var viewModels = coaches.Select(c => new CoachPrivateScheduleViewModel
            {
                CoachId = c.CoachId,
                CoachName = c.Account?.Name ?? $"Coach#{c.CoachId}",
                Specialty = c.Specialty,
                Photo = c.Photo,
                CoachTimes = c.CoachTimes
                    .OrderBy(t => t.Date).ThenBy(t => t.TimeSlot)
                    .Select(t => {
                        var reserved = t.Status == 1;
                        var session = t.PrivateSessions.FirstOrDefault();
                        string? memberName = reserved && session != null && members.TryGetValue(session.MemberId, out var name)
                            ? name
                            : null;

                        return new CoachTimeInfo
                        {
                            Date = t.Date,
                            TimeSlot = t.TimeSlot,
                            IsReserved = reserved,
                            MemberName = memberName
                        };
                    }).ToList()
            }).ToList();

            return View(viewModels);
        }



    }
}
