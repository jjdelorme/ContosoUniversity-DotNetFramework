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
    public class DepartmentController : Controller
    {
        private readonly SchoolContext _context;

        public DepartmentController(SchoolContext context)
        {
            _context = context;
        }

        // GET: Department
        public async Task<IActionResult> Index()
        {
            var departments = _context.Departments
                .Include(d => d.Administrator)
                .AsNoTracking();
            return View(await departments.ToListAsync());
        }

        // GET: Department/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var department = await _context.Departments
                .Include(d => d.Administrator)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DepartmentID == id);
            
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        private async Task PopulateInstructorsDropDownListAsync(object selectedInstructor = null)
        {
            var instructorsQuery = from i in _context.Instructors
                                   orderby i.LastName, i.FirstMidName
                                   select new { i.ID, FullName = i.LastName + ", " + i.FirstMidName }; // Project to anonymous type for FullName
            
            ViewData["InstructorID"] = new SelectList(await instructorsQuery.AsNoTracking().ToListAsync(), "ID", "FullName", selectedInstructor);
        }

        // Create, Edit, Delete actions will be added subsequently.

        // GET: Department/Create
        public async Task<IActionResult> Create()
        {
            await PopulateInstructorsDropDownListAsync();
            return View();
        }

        // POST: Department/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmentID,Name,Budget,StartDate,InstructorID,RowVersion")] Department department)
        {
            // DepartmentID is not database generated in the original, but typically it would be.
            // For this conversion, assuming DepartmentID is an identity column if not explicitly set by user.
            // If DepartmentID is meant to be user-provided and unique, add check:
            // if (await _context.Departments.AnyAsync(d => d.DepartmentID == department.DepartmentID))
            // {
            //     ModelState.AddModelError("DepartmentID", "Department ID already exists.");
            // }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(department);
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
            await PopulateInstructorsDropDownListAsync(department.InstructorID);
            return View(department);
        }

        // GET: Department/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var department = await _context.Departments
                .Include(d => d.Administrator) // Eager load administrator for display
                .AsNoTracking() // For editing, we'll load a tracked entity in POST
                .FirstOrDefaultAsync(m => m.DepartmentID == id);
            
            if (department == null)
            {
                return NotFound();
            }
            await PopulateInstructorsDropDownListAsync(department.InstructorID);
            return View(department);
        }

        // POST: Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, byte[] rowVersion) // rowVersion from hidden field
        {
            if (id == null)
            {
                return BadRequest();
            }

            var departmentToUpdate = await _context.Departments.Include(d => d.Administrator).FirstOrDefaultAsync(m => m.DepartmentID == id);

            if (departmentToUpdate == null)
            {
                // Department was deleted by another user.
                Department deletedDepartment = new Department();
                // Attempt to bind to show the values the user tried to save.
                await TryUpdateModelAsync(deletedDepartment); 
                ModelState.AddModelError(string.Empty,
                    "Unable to save changes. The department was deleted by another user.");
                await PopulateInstructorsDropDownListAsync(deletedDepartment.InstructorID);
                return View(deletedDepartment);
            }

            // Set original RowVersion for concurrency check
            _context.Entry(departmentToUpdate).Property("RowVersion").OriginalValue = rowVersion;

            if (await TryUpdateModelAsync<Department>(
                departmentToUpdate,
                "", // Prefix for form values
                s => s.Name, s => s.Budget, s => s.StartDate, s => s.InstructorID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Department)exceptionEntry.Entity; // Values from user's form
                    var databaseEntry = exceptionEntry.GetDatabaseValues(); // Values from DB

                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Unable to save changes. The department was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Department)databaseEntry.ToObject();

                        if (databaseValues.Name != clientValues.Name)
                        {
                            ModelState.AddModelError("Name", $"Current value: {databaseValues.Name}");
                        }
                        if (databaseValues.Budget != clientValues.Budget)
                        {
                            ModelState.AddModelError("Budget", $"Current value: {databaseValues.Budget:c}");
                        }
                        if (databaseValues.StartDate != clientValues.StartDate)
                        {
                            ModelState.AddModelError("StartDate", $"Current value: {databaseValues.StartDate:d}");
                        }
                        if (databaseValues.InstructorID != clientValues.InstructorID)
                        {
                            Instructor databaseInstructor = await _context.Instructors.FindAsync(databaseValues.InstructorID);
                            ModelState.AddModelError("InstructorID", $"Current value: {databaseInstructor?.FullName}");
                        }

                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you submitted the form. The "
                            + "edit operation was canceled and the current values in the database "
                            + "have been displayed. If you still want to edit this record, click "
                            + "the Save button again. Otherwise click the Back to List hyperlink.");
                        
                        // Update RowVersion to the latest from database for the next postback
                        departmentToUpdate.RowVersion = databaseValues.RowVersion;
                        // Remove ModelState error for RowVersion if TryUpdateModel added one, as we're handling it.
                        ModelState.Remove("RowVersion"); 
                    }
                }
                catch (DbUpdateException /* ex */)
                {
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }
            await PopulateInstructorsDropDownListAsync(departmentToUpdate.InstructorID);
            return View(departmentToUpdate);
        }

        // GET: Department/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? concurrencyError)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var department = await _context.Departments
                .Include(d => d.Administrator)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DepartmentID == id);

            if (department == null)
            {
                if (concurrencyError.GetValueOrDefault())
                {
                    // Department was deleted after concurrency error, redirect to index
                    return RedirectToAction(nameof(Index));
                }
                return NotFound();
            }

            if (concurrencyError.GetValueOrDefault())
            {
                ViewData["ConcurrencyErrorMessage"] = "The record you attempted to delete "
                    + "was modified by another user after you got the original values. "
                    + "The delete operation was canceled and the current values in the "
                    + "database have been displayed. If you still want to delete this "
                    + "record, click the Delete button again. Otherwise "
                    + "click the Back to List hyperlink.";
            }

            return View(department);
        }

        // POST: Department/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Department department) // Department object from form (includes DepartmentID and RowVersion)
        {
            try
            {
                var departmentToDelete = await _context.Departments.FindAsync(department.DepartmentID);
                if (departmentToDelete == null)
                {
                    // Already deleted or ID is incorrect
                    return RedirectToAction(nameof(Index));
                }

                // Set original RowVersion for concurrency check
                _context.Entry(departmentToDelete).Property("RowVersion").OriginalValue = department.RowVersion;
                
                _context.Departments.Remove(departmentToDelete);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                return RedirectToAction(nameof(Delete), new { concurrencyError = true, id = department.DepartmentID });
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.)
                ModelState.AddModelError(string.Empty, "Unable to delete. Try again, and if the problem persists contact your system administrator.");
                // Need to repopulate view data if returning to view with error
                var departmentWithError = await _context.Departments
                    .Include(d => d.Administrator)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DepartmentID == department.DepartmentID);
                if (departmentWithError == null) return RedirectToAction(nameof(Index)); // Should not happen if delete failed due to other reasons
                return View(departmentWithError);
            }
        }
    }
}
