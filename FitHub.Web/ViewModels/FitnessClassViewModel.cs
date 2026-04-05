using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Options;
using System.ComponentModel.DataAnnotations;

namespace FitHub.Web.ViewModels;

public class FitnessClassViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "The class title is mandatory")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 100 characters")]
    [Display(Name = "Class Title")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Capacity is required")]
    [Range(1, 100, ErrorMessage = "Capacity must be between 1 and 100 warriors")]
    public int Capacity { get; set; }

    [Required(ErrorMessage = "A schedule date is required")]
    [Display(Name = "Training Date & Time")]
    [DataType(DataType.DateTime)]
    public DateTime ScheduleDate { get; set; } = DateTime.Now.AddDays(1);

    [Required(ErrorMessage = "Price is mandatory (use 0 for free classes)")]
    [Range(0, 999.99, ErrorMessage = "Price must be between 0 and 999.99")]
    [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Please select a Category")]
    [Display(Name = "Class Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please assign an Instructor")]
    [Display(Name = "Lead Instructor")]
    public int InstructorId { get; set; }

    // Dropdown lists
    public IEnumerable<SelectListItem>? Categories { get; set; }

    public IEnumerable<SelectListItem>? Instructors { get; set; }
}