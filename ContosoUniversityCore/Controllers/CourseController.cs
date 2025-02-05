using ContosoUniversityCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversityCore.Controllers
{
    public class CourseController : Controller
    {
        private readonly DbContext _context;

        public CourseController(DbContext context)
        {
            _context = context;
        }

        // GET: Course
        [HttpGet]
        public IActionResult Index(int? SelectedDepartment)
        {
            var departments = _context.Set<Department>().OrderBy(q => q.Name).ToList();
            ViewBag.SelectedDepartment = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(departments, "DepartmentID", "Name", SelectedDepartment);
            int departmentID = SelectedDepartment.GetValueOrDefault();

            IQueryable<Course> courses = _context.Set<Course>()
                .Where(c => !SelectedDepartment.HasValue || c.DepartmentID == departmentID)
                .OrderBy(d => d.CourseID)
                .Include(d => d.Department);
            var sql = courses.ToString();
            return View(courses.ToList());
        }

        // GET: Course/Details/5
        [HttpGet]
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestResult();
            }
            Course course = _context.Set<Course>().Find(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }


        public IActionResult Create()
        {
            PopulateDepartmentsDropDownList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind(Include = "CourseID,Title,Credits,DepartmentID")]Course course)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Set<Course>().Add(course);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (RetryLimitExceededException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.)
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
            }
            PopulateDepartmentsDropDownList(course.DepartmentID);
            return View(course);
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestResult();
            }
            Course course = _context.Set<Course>().Find(id);
            if (course == null)
            {
                return NotFound();
            }
            PopulateDepartmentsDropDownList(course.DepartmentID);
            return View(course);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public IActionResult EditPost(int? id)
        {
            if (id == null)
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestResult();
            }
            var courseToUpdate = _context.Set<Course>().Find(id);
            if (TryUpdateModel(courseToUpdate, "",
               new string[] { "Title", "Credits", "DepartmentID" }))
            {
                try
                {
                    _context.SaveChanges();

                    return RedirectToAction(nameof(Index));
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    //Log the error (uncomment dex variable name and add a line here to write a log.
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            PopulateDepartmentsDropDownList(courseToUpdate.DepartmentID);
            return View(courseToUpdate);
        }

        private void PopulateDepartmentsDropDownList(object selectedDepartment = null)
        {
            var departmentsQuery = from d in _context.Set<Department>()
                                   orderby d.Name
                                   select d;
            ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(departmentsQuery, "DepartmentID", "Name", selectedDepartment);
        }


        // GET: Course/Delete/5
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestResult();
            }
            Course course = _context.Set<Course>().Find(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Course course = _context.Set<Course>().Find(id);
            _context.Set<Course>().Remove(course);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult UpdateCourseCredits()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UpdateCourseCredits(int? multiplier)
        {
            if (multiplier != null)
            {
                ViewBag.RowsAffected = _context.Database.ExecuteSqlRaw("UPDATE Course SET Credits = Credits * {0}", multiplier);
            }
            return View();
        }
    }
}
