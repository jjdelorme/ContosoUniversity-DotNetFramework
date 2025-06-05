using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversityCore.Data;
using ContosoUniversityCore.Models;

namespace ContosoUniversityCore.Controllers
{
    public class CourseController : Controller
    {
        private readonly SchoolContext _context;

        public CourseController(SchoolContext context)
        {
            _context = context;
        }

        // GET: Course
        public async Task<IActionResult> Index(int? selectedDepartment)
        {
            var departments = await _context.Departments.OrderBy(q => q.Name).ToListAsync();
            ViewData["SelectedDepartment"] = new SelectList(departments, "DepartmentID", "Name", selectedDepartment);
            
            int departmentID = selectedDepartment.GetValueOrDefault();

            IQueryable<Course> courses = _context.Courses
                .Where(c => !selectedDepartment.HasValue || c.DepartmentID == departmentID)
                .OrderBy(d => d.CourseID)
                .Include(d => d.Department);
            
            return View(await courses.AsNoTracking().ToListAsync());
        }

        private async Task PopulateDepartmentsDropDownListAsync(object selectedDepartment = null)
        {
            var departmentsQuery = from d in _context.Departments
                                   orderby d.Name
                                   select d;
            ViewData["DepartmentID"] = new SelectList(await departmentsQuery.AsNoTracking().ToListAsync(), "DepartmentID", "Name", selectedDepartment);
        }

        // Details, Create, Edit, Delete, UpdateCourseCredits actions will be added subsequently.

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var course = await _context.Courses
                .Include(c => c.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.CourseID == id);
            
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Course/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDepartmentsDropDownListAsync();
            return View();
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseID,Title,Credits,DepartmentID")] Course course)
        {
            // CourseID is not database generated, so it's included in Bind.
            // Check if CourseID already exists to prevent primary key violation if user enters an existing one.
            if (await _context.Courses.AnyAsync(c => c.CourseID == course.CourseID))
            {
                ModelState.AddModelError("CourseID", "Course Number already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(course);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException /* ex */)
                {
                    //Log the error (uncomment ex variable name and write a log.)
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }
            await PopulateDepartmentsDropDownListAsync(course.DepartmentID);
            return View(course);
        }

        // GET: Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            await PopulateDepartmentsDropDownListAsync(course.DepartmentID);
            return View(course);
        }

        // POST: Course/Edit/5
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var courseToUpdate = await _context.Courses.FirstOrDefaultAsync(c => c.CourseID == id);

            if (courseToUpdate == null)
            {
                return NotFound(); // Should not happen if GET Edit worked, but good practice
            }

            if (await TryUpdateModelAsync<Course>(
                courseToUpdate,
                "", // Prefix for form values
                c => c.Title, c => c.Credits, c => c.DepartmentID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Course)exceptionEntry.Entity;
                    var databaseEntry = await exceptionEntry.GetDatabaseValuesAsync();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to save changes. The course was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Course)databaseEntry.ToObject();
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you got the original value. The "
                            + "edit operation was canceled and the current values in the database "
                            + "have been displayed. If you still want to edit this record, click "
                            + "the Save button again. Otherwise click the Back to List hyperlink.");
                        // Update the model with the database values to show the user
                        courseToUpdate.Title = databaseValues.Title;
                        courseToUpdate.Credits = databaseValues.Credits;
                        courseToUpdate.DepartmentID = databaseValues.DepartmentID;
                        // No RowVersion on Course model, so this is more informational
                    }
                }
                catch (DbUpdateException /* ex */)
                {
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }
            await PopulateDepartmentsDropDownListAsync(courseToUpdate.DepartmentID);
            return View(courseToUpdate);
        }

        // GET: Course/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var course = await _context.Courses
                .Include(c => c.Department) // Include department for display
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.CourseID == id);
            
            if (course == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] =
                    "Delete failed. Try again, and if the problem persists " +
                    "see your system administrator.";
            }

            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error and redirect with error message
                return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
            }
        }

        // GET: Course/UpdateCourseCredits
        public IActionResult UpdateCourseCredits()
        {
            return View();
        }

        // POST: Course/UpdateCourseCredits
        [HttpPost]
        public async Task<IActionResult> UpdateCourseCredits(int? multiplier)
        {
            if (multiplier != null)
            {
                // Using FromSqlInterpolated or ExecuteSqlInterpolatedAsync for parameterized raw SQL
                ViewData["RowsAffected"] = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Course SET Credits = Credits * {multiplier}");
            }
            return View();
        }
    }
}
