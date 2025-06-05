using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversityCore.Models
{
    public class Department
    {
        public int DepartmentID { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }

        [DataType(DataType.Currency)]
        // [Column(TypeName = "money")] // Removed for SQLite compatibility
        public decimal Budget { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        public int? InstructorID { get; set; }

        // [Timestamp] // Removed for SQLite, will be configured via Fluent API
        public byte[] RowVersion { get; set; }

        public virtual Instructor Administrator { get; set; } // Nullability warning for this will remain for now
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
