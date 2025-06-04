namespace ContosoUniversity.Core.ViewModels
{
    public class AssignedCourseData
    {
        public int CourseID { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Assigned { get; set; }
    }
}