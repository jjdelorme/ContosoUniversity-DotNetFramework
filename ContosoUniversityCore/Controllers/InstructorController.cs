using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoUniversityCore.Data;
using ContosoUniversityCore.Models;
using ContosoUniversityCore.ViewModels;
using Microsoft.Extensions.Logging; // Added for ILogger
using System.ComponentModel.DataAnnotations; // Required for Validator

namespace ContosoUniversityCore.Controllers
{
    public class InstructorController : Controller
    {
        private readonly SchoolContext _context;
        private readonly ILogger<InstructorController> _logger;

        public InstructorController(SchoolContext context, ILogger<InstructorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Instructor
        public async Task<IActionResult> Index(int? id, int? courseID)
        {
            var viewModel = new InstructorIndexData();
            viewModel.Instructors = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                    .ThenInclude(c => c.Department) // Eagerly load Department for courses
                .OrderBy(i => i.LastName)
                .AsNoTracking() // Good for read-only list
                .ToListAsync();

            if (id != null)
            {
                ViewData["InstructorID"] = id.Value;
                Instructor instructor = viewModel.Instructors.Single(i => i.ID == id.Value);
                viewModel.Courses = instructor.Courses;
            }

            if (courseID != null)
            {
                ViewData["CourseID"] = courseID.Value;
                // In EF Core, it's often better to re-query or ensure data is loaded efficiently.
                // The original used explicit loading. Here, we can use Include/ThenInclude if Courses are re-fetched,
                // or rely on the fact that viewModel.Courses (from selected instructor) should have enrollments if structured well.
                // For simplicity and to match original intent of loading enrollments for a *specific* course:
                
                // Ensure Courses collection for the selected instructor is loaded with Department
                // This might be redundant if the initial query for Instructors already did this.
                // Let's refine the initial query for instructors to include Course.Department
                // The initial query for Instructors should be:
                // .Include(i => i.Courses).ThenInclude(c => c.Department)
                // For now, let's assume viewModel.Courses has the necessary courses.

                // var selectedCourse = viewModel.Courses?.SingleOrDefault(x => x.CourseID == courseID.Value);
                // if (selectedCourse != null) // This check is implicitly handled by the query below
                
                // Load enrollments for the selected course, including student details
                viewModel.Enrollments = await _context.Enrollments
                    .Where(e => e.CourseID == courseID.Value)
                    .Include(e => e.Student)
                    .AsNoTracking() // Good for read-only list
                    .ToListAsync();
            }
            return View(viewModel);
        }

        // Other actions (Details, Create, Edit, Delete) will be added subsequently.

        // GET: Instructor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                    .ThenInclude(c => c.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            if (instructor == null)
            {
                return NotFound();
            }

            return View(instructor);
        }

        // GET: Instructor/Create
        public IActionResult Create()
        {
            var instructor = new Instructor
            {
                Courses = new List<Course>(), // Initialize Courses collection
                OfficeAssignment = new OfficeAssignment() // Initialize OfficeAssignment
            };
            PopulateAssignedCourseData(instructor);
            return View();
        }

        // POST: Instructor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FirstMidName,LastName,HireDate,OfficeAssignment.Location")] Instructor instructor, 
            string[] selectedCourses)
        {
            if (selectedCourses != null)
            {
                instructor.Courses = new List<Course>();
                foreach (var courseIdString in selectedCourses)
                {
                    if (int.TryParse(courseIdString, out var courseId))
                    {
                        var courseToAdd = await _context.Courses.FindAsync(courseId);
                        if (courseToAdd != null)
                        {
                            instructor.Courses.Add(courseToAdd);
                        }
                    }
                }
            }

            // If OfficeAssignment.Location is empty, set OfficeAssignment to null
            // to prevent an empty OfficeAssignment record from being created.
            if (instructor.OfficeAssignment != null && string.IsNullOrWhiteSpace(instructor.OfficeAssignment.Location))
            {
                instructor.OfficeAssignment = null;
            }

            if (ModelState.IsValid)
            {
                _context.Instructors.Add(instructor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // If model state is invalid, re-populate assigned courses for the view
            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }
        
        private void PopulateAssignedCourseData(Instructor instructor)
        {
            var allCourses = _context.Courses;
            var instructorCourseIDs = new HashSet<int>(instructor.Courses.Select(c => c.CourseID));
            var viewModel = new List<AssignedCourseData>();
            foreach (var course in allCourses)
            {
                viewModel.Add(new AssignedCourseData
                {
                    CourseID = course.CourseID,
                    Title = course.Title,
                    Assigned = instructorCourseIDs.Contains(course.CourseID)
                });
            }
            ViewData["Courses"] = viewModel;
        }

        // GET: Instructor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var instructor = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                .FirstOrDefaultAsync(m => m.ID == id);
            
            if (instructor == null)
            {
                return NotFound();
            }
            PopulateAssignedCourseData(instructor);
            return View(instructor);
        }

        // POST: Instructor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, string[] selectedCourses)
        {
            _logger.LogInformation("Instructor Edit POST action called for ID: {InstructorId}", id);
            _logger.LogInformation("Selected courses: {SelectedCourses}", selectedCourses != null ? string.Join(",", selectedCourses) : "null");

            if (id == null)
            {
                _logger.LogWarning("Instructor Edit POST: ID is null, returning BadRequest.");
                return BadRequest();
            }

            var instructorToUpdate = await _context.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.Courses)
                .FirstOrDefaultAsync(i => i.ID == id);

            if (instructorToUpdate == null)
            {
                _logger.LogWarning("Instructor Edit POST: Instructor with ID {InstructorId} not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Instructor to update (before TryUpdateModelAsync): ID={Id}, Name={LastName}, Office={OfficeLocation}", 
                instructorToUpdate.ID, instructorToUpdate.LastName, instructorToUpdate.OfficeAssignment?.Location);

            // Manually update instructor properties to avoid validation issues with navigation properties
            bool modelUpdateResult = true;
            
            // Update FirstMidName
            if (Request.Form.ContainsKey("FirstMidName"))
            {
                instructorToUpdate.FirstMidName = Request.Form["FirstMidName"].ToString();
            }
            
            // Update LastName
            if (Request.Form.ContainsKey("LastName"))
            {
                instructorToUpdate.LastName = Request.Form["LastName"].ToString();
            }
            
            // Update HireDate
            if (Request.Form.ContainsKey("HireDate") && DateTime.TryParse(Request.Form["HireDate"].ToString(), out DateTime hireDate))
            {
                instructorToUpdate.HireDate = hireDate;
            }
            
            // Validate the basic instructor properties manually
            if (string.IsNullOrWhiteSpace(instructorToUpdate.FirstMidName))
            {
                ModelState.AddModelError("FirstMidName", "The First Name field is required.");
                modelUpdateResult = false;
            }
            else if (instructorToUpdate.FirstMidName.Length > 50)
            {
                ModelState.AddModelError("FirstMidName", "First name cannot be longer than 50 characters.");
                modelUpdateResult = false;
            }
            
            if (string.IsNullOrWhiteSpace(instructorToUpdate.LastName))
            {
                ModelState.AddModelError("LastName", "The Last Name field is required.");
                modelUpdateResult = false;
            }
            else if (instructorToUpdate.LastName.Length > 50)
            {
                ModelState.AddModelError("LastName", "The field Last Name must be a string with a maximum length of 50.");
                modelUpdateResult = false;
            }
            
            _logger.LogInformation("Manual model update result: {Result}", modelUpdateResult);
            if (!modelUpdateResult)
            {
                _logger.LogWarning("Manual model update failed. ModelState errors: {ModelStateErrors}", 
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            if (modelUpdateResult) // If Instructor's direct properties are valid
            {
                _logger.LogInformation("Instructor after TryUpdateModelAsync: Name={LastName}", instructorToUpdate.LastName);
                
                // Handle OfficeAssignment with more explicit DbSet interaction
                string officeLocationFromForm = Request.Form["OfficeAssignment.Location"].ToString();
                _logger.LogInformation("OfficeLocation from form: '{OfficeLocation}'", officeLocationFromForm);

                var existingOfficeAssignment = instructorToUpdate.OfficeAssignment; // Already loaded via Include

                if (string.IsNullOrWhiteSpace(officeLocationFromForm))
                {
                    if (existingOfficeAssignment != null)
                    {
                        _logger.LogInformation("Office location cleared. Removing existing OfficeAssignment for Instructor ID: {InstructorId}", instructorToUpdate.ID);
                        _context.OfficeAssignments.Remove(existingOfficeAssignment);
                        instructorToUpdate.OfficeAssignment = null; // Ensure the navigation property is also null
                    }
                }
                else // officeLocationFromForm has a value
                {
                    if (existingOfficeAssignment == null)
                    {
                        _logger.LogInformation("Office location '{OfficeLocation}' provided. Creating new OfficeAssignment for Instructor ID: {InstructorId}", officeLocationFromForm, instructorToUpdate.ID);
                        var newOfficeAssignment = new OfficeAssignment { InstructorID = instructorToUpdate.ID, Location = officeLocationFromForm };
                        _context.OfficeAssignments.Add(newOfficeAssignment);
                        instructorToUpdate.OfficeAssignment = newOfficeAssignment; 
                    }
                    else
                    {
                        if (existingOfficeAssignment.Location != officeLocationFromForm)
                        {
                            _logger.LogInformation("Office location changing from '{OldLocation}' to '{NewLocation}' for Instructor ID: {InstructorId}", existingOfficeAssignment.Location, officeLocationFromForm, instructorToUpdate.ID);
                            existingOfficeAssignment.Location = officeLocationFromForm;
                        }
                        else
                        {
                            _logger.LogInformation("Office location '{OfficeLocation}' unchanged for Instructor ID: {InstructorId}", officeLocationFromForm, instructorToUpdate.ID);
                        }
                    }
                }
                _logger.LogInformation("OfficeAssignment state after logic: Location='{OfficeLocation}', EntityState='{EntityState}'", 
                    instructorToUpdate.OfficeAssignment?.Location, 
                    instructorToUpdate.OfficeAssignment != null ? _context.Entry(instructorToUpdate.OfficeAssignment).State.ToString() : "null");

                // Explicitly validate OfficeAssignment if it's been set/modified
                if (instructorToUpdate.OfficeAssignment != null)
                {
                    var validationContext = new ValidationContext(instructorToUpdate.OfficeAssignment, serviceProvider: null, items: null);
                    var validationResults = new List<ValidationResult>();
                    if (!Validator.TryValidateObject(instructorToUpdate.OfficeAssignment, validationContext, validationResults, validateAllProperties: true))
                    {
                        _logger.LogWarning("OfficeAssignment validation failed.");
                        foreach (var validationResult in validationResults)
                        {
                            foreach (var memberName in validationResult.MemberNames) // e.g., "Location"
                            {
                                ModelState.AddModelError($"OfficeAssignment.{memberName}", validationResult.ErrorMessage);
                                _logger.LogWarning("OfficeAssignment ModelState error for {MemberName}: {ErrorMessage}", $"OfficeAssignment.{memberName}", validationResult.ErrorMessage);
                            }
                            if (!validationResult.MemberNames.Any()) // Error not specific to a property
                            {
                                 ModelState.AddModelError("OfficeAssignment", validationResult.ErrorMessage);
                                 _logger.LogWarning("OfficeAssignment ModelState error (general): {ErrorMessage}", validationResult.ErrorMessage);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogInformation("OfficeAssignment validation succeeded.");
                    }
                }
            }
            // else: if modelUpdateResult (for Instructor) was false, ModelState already contains errors.

            // Check overall ModelState before attempting to save
            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Calling UpdateInstructorCoursesAsync and SaveChangesAsync.");
                await UpdateInstructorCoursesAsync(selectedCourses, instructorToUpdate);
                _logger.LogInformation("UpdateInstructorCoursesAsync completed.");

                try
                {
                    _logger.LogInformation("Calling SaveChangesAsync.");
                    int changes = await _context.SaveChangesAsync();
                    _logger.LogInformation("SaveChangesAsync completed. {ChangesCount} changes saved.", changes);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "DbUpdateConcurrencyException during Instructor Edit POST.");
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Instructor)exceptionEntry.Entity;
                    var databaseEntry = await exceptionEntry.GetDatabaseValuesAsync();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to save changes. The instructor was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Instructor)databaseEntry.ToObject();
                        instructorToUpdate.FirstMidName = databaseValues.FirstMidName;
                        instructorToUpdate.LastName = databaseValues.LastName;
                        instructorToUpdate.HireDate = databaseValues.HireDate;
                        instructorToUpdate.OfficeAssignment = databaseValues.OfficeAssignment;
                        instructorToUpdate.Courses = await _context.Entry(databaseValues).Collection(i => i.Courses).Query().ToListAsync();
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you got the original value. The "
                            + "edit operation was canceled and the current values in the database "
                            + "have been displayed. If you still want to edit this record, click "
                            + "the Save button again. Otherwise click the Back to List hyperlink.");
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "DbUpdateException during Instructor Edit POST.");
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Try again, and if the problem persists, " +
                        "see your system administrator.");
                }
            }
            else
            {
                _logger.LogWarning("Instructor Edit POST: ModelState is invalid before SaveChangesAsync. Errors: {ModelStateErrors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }
            
            // If TryUpdateModelAsync failed, or OfficeAssignment validation failed, or SaveChanges failed
            _logger.LogWarning("Instructor Edit POST: Repopulating data and returning View due to previous errors.");
            PopulateAssignedCourseData(instructorToUpdate);
            return View(instructorToUpdate);
        }

        private async Task UpdateInstructorCoursesAsync(string[] selectedCourses, Instructor instructorToUpdate)
        {
            _logger.LogInformation("UpdateInstructorCoursesAsync: Start. Selected courses count: {SelectedCount}", selectedCourses?.Length ?? 0);
            if (selectedCourses == null)
            {
                _logger.LogInformation("UpdateInstructorCoursesAsync: No courses selected, clearing instructor's courses.");
                instructorToUpdate.Courses.Clear(); // More explicit than new List<Course>() if collection is already tracked
                return;
            }

            var selectedCoursesHS = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<int>(instructorToUpdate.Courses.Select(c => c.CourseID));
            
            foreach (var course in await _context.Courses.ToListAsync()) // Iterate over all courses in DB
            {
                if (selectedCoursesHS.Contains(course.CourseID.ToString()))
                {
                    if (!instructorCourses.Contains(course.CourseID))
                    {
                        _logger.LogInformation("UpdateInstructorCoursesAsync: Adding course {CourseId} to instructor.", course.CourseID);
                        instructorToUpdate.Courses.Add(course);
                    }
                }
                else
                {
                    if (instructorCourses.Contains(course.CourseID))
                    {
                        var courseToRemove = instructorToUpdate.Courses.FirstOrDefault(c => c.CourseID == course.CourseID);
                        if (courseToRemove != null)
                        {
                            _logger.LogInformation("UpdateInstructorCoursesAsync: Removing course {CourseId} from instructor.", course.CourseID);
                            instructorToUpdate.Courses.Remove(courseToRemove);
                        }
                    }
                }
            }
            _logger.LogInformation("UpdateInstructorCoursesAsync: End.");
        }

        // GET: Instructor/Delete/5
        public async Task<IActionResult> Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var instructor = await _context.Instructors
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            
            if (instructor == null)
            {
                return NotFound();
            }

            if (saveChangesError.GetValueOrDefault())
            {
                ViewData["ErrorMessage"] =
                    "Delete failed. Try again, and if the problem persists " +
                    "see your system administrator.";
            }

            return View(instructor);
        }

        // POST: Instructor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // It's important to load related entities that might need updating or
            // that might cause FK issues if not handled.
            Instructor instructor = await _context.Instructors
              .Include(i => i.OfficeAssignment) // To remove it if it exists
              .Include(i => i.Courses) // To handle course assignments (many-to-many)
              .FirstOrDefaultAsync(i => i.ID == id);

            if (instructor == null)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Remove one-to-one OfficeAssignment
                if (instructor.OfficeAssignment != null)
                {
                    _context.OfficeAssignments.Remove(instructor.OfficeAssignment);
                }

                // Remove associations from Departments where this instructor is an administrator
                var departmentsToUpdate = await _context.Departments
                    .Where(d => d.InstructorID == id)
                    .ToListAsync();
                foreach (var dept in departmentsToUpdate)
                {
                    dept.InstructorID = null;
                }
                
                // EF Core handles many-to-many join table records automatically when the principal entity is deleted,
                // or when items are removed from the navigation collection if change tracking is on.
                // Since we are deleting the instructor, the CourseInstructor join records will be removed.

                _context.Instructors.Remove(instructor);
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error and redirect appropriately
                return RedirectToAction(nameof(Delete), new { id = id, saveChangesError = true });
            }
        }
    }
}
